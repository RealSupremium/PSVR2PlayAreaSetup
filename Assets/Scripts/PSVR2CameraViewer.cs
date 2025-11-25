using UnityEngine;
using System;
using Valve.VR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PSVR2CameraViewer : MonoBehaviour
{
    public MeshRenderer cameraRendererLeft;
    public MeshRenderer cameraRendererRight;
    public Material bc4Material;
    public Transform poseStabilizer; // Assign the parent of the passthrough meshes here
    public Transform center;
    public Vector3 correction;
    public Vector3 correctionPost;

    private Texture2D texLeft, texRight;
    private byte[] bufferLeft, bufferRight;
    private Material matLeft, matRight;
    private MeshFilter meshFilterLeft, meshFilterRight;

    void Start()
    {
        // Ensure SteamVR is initialized
        if (SteamVR.instance != null)
        {
            Debug.Log("Setting tracking universe to Raw and Uncalibrated...");

            // Option 1: Set the plugin's global setting
            // This is the recommended way if you're using the SteamVR_Settings asset
            SteamVR.settings.trackingSpace = ETrackingUniverseOrigin.TrackingUniverseRawAndUncalibrated;

            // Option 2: Call the Compositor directly (lower-level)
            // This forces the change on the compositor itself.
            if (OpenVR.Compositor != null)
            {
                OpenVR.Compositor.SetTrackingSpace(ETrackingUniverseOrigin.TrackingUniverseRawAndUncalibrated);
                Debug.Log("Compositor tracking space set successfully.");
            }
            else
            {
                Debug.LogError("Could not find OpenVR Compositor to set tracking space.");
            }
        }
        else
        {
            Debug.LogError("SteamVR is not initialized. Cannot set tracking universe.");
        }

        try
        {
            PSVR2SharedMemory.Init();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize PSVR2 Shared Memory: {e.Message}");
            this.enabled = false;
            return;
        }

        int width = 1024;
        int height = 1016;
        texLeft = new Texture2D(width, height, TextureFormat.BC4, false);
        texRight = new Texture2D(width, height, TextureFormat.BC4, false);
        bufferLeft = new byte[PSVR2SharedMemory.BC4_DATA_SIZE];
        bufferRight = new byte[PSVR2SharedMemory.BC4_DATA_SIZE];

        meshFilterLeft = cameraRendererLeft.GetComponent<MeshFilter>();
        meshFilterRight = cameraRendererRight.GetComponent<MeshFilter>();

        matLeft = Instantiate(bc4Material);
        matRight = Instantiate(bc4Material);

        matLeft.SetInt("_StereoEyeIndex", 0);  // Left Eye
        matRight.SetInt("_StereoEyeIndex", 1); // Right Eye

        cameraRendererLeft.material = matLeft;
        cameraRendererRight.material = matRight;

        matLeft.SetTexture("_MainTex", texLeft);
        matRight.SetTexture("_MainTex", texRight);

        UpdateMeshes();

        if (poseStabilizer == null)
        {
            Debug.LogWarning("Pose Stabilizer not assigned. Creating one automatically.");
            GameObject stabilizer = new GameObject("Passthrough Stabilizer");
            poseStabilizer = stabilizer.transform;
            // Try to parent to XR Origin if possible, otherwise root is okay if it tracks room-scale.
            if (Camera.main != null && Camera.main.transform.parent != null)
            {
                poseStabilizer.SetParent(Camera.main.transform.parent);
            }
            poseStabilizer.localPosition = Vector3.zero;
            poseStabilizer.localRotation = Quaternion.identity;

            cameraRendererLeft.transform.SetParent(poseStabilizer, false);
            cameraRendererRight.transform.SetParent(poseStabilizer, false);
        }
    }

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeforeRender;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeforeRender;
    }

    void OnBeforeRender(ScriptableRenderContext context, Camera camera)
    {
        // Get latest image AND the interpolated pose for when it was taken
        if (PSVR2SharedMemory.GetLatestImageBuffer(bufferLeft, bufferRight, out PoseData historicalPose))
        {
            texLeft.LoadRawTextureData(bufferLeft);
            texRight.LoadRawTextureData(bufferRight);
            texLeft.Apply();
            texRight.Apply();

            if (historicalPose.isValid)
            {
                // Apply the historical pose to the stabilizer.
                // Since the stabilizer is parented to the XR Origin, these local coordinates
                // should match the SteamVR tracking space coordinates from shared memory.
                // NOTE: You might need to flip/swizzle coordinates if SteamVR and Unity differ.
                // SteamVR is usually right-handed, Y-up. Unity is left-handed, Y-up.
                // A common conversion is: UnityPos = new Vector3(steamPos.x, steamPos.y, -steamPos.z);
                // UnityRot = new Quaternion(-steamRot.x, -steamRot.y, steamRot.z, steamRot.w);
                // Try direct first, then apply conversion if it moves backwards/inverted.

                poseStabilizer.localPosition = historicalPose.position;
                poseStabilizer.localRotation = Quaternion.Euler(correction.x, 0, 0)
                    * Quaternion.Euler(0, correction.y, 0)
                    * Quaternion.Euler(0, 0, correction.z)
                    * historicalPose.rotation
                    * Quaternion.Euler(correctionPost.x, 0, 0)
                    * Quaternion.Euler(0, correctionPost.y, 0)
                    * Quaternion.Euler(0, 0, correctionPost.z);
            }

            matLeft.SetVector("_FloorPosition", center.position);
            matRight.SetVector("_FloorPosition", center.position);
            matLeft.SetVector("_FloorUp", center.up);
            matRight.SetVector("_FloorUp", center.up);
        }
    }

    void UpdateMeshes()
    {
        CameraParameters params0, params1;
        CameraIntrinsics intr0, intr1;

        bool ok0 = PSVR2SharedMemory.GetDistortionConfig(0, out params0, out intr0);
        bool ok1 = PSVR2SharedMemory.GetDistortionConfig(1, out params1, out intr1);

        if (ok0)
            meshFilterLeft.mesh = PSVR2Distortion.CreateUndistortionMesh(1024, 1016, intr0, params0);
        else
            meshFilterLeft.mesh = PSVR2Distortion.CreateDefaultMesh();

        if (ok1)
            meshFilterRight.mesh = PSVR2Distortion.CreateUndistortionMesh(1024, 1016, intr1, params1);
        else
            meshFilterRight.mesh = PSVR2Distortion.CreateDefaultMesh();
    }

    void OnApplicationQuit()
    {
        PSVR2SharedMemory.Cleanup();
    }
}