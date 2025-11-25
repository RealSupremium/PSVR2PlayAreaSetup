Shader "Unlit/BC4Decompress_URP"
{
    Properties
    {
        _MainTex ("BC4 Texture", 2D) = "white" {}
        _StereoEyeIndex ("Stereo Eye Index", Int) = 0
        _FloorHeight ("Floor Height", Float) = 0.0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        LOD 100
        Cull Off

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                int _StereoEyeIndex;
                float _FloorHeight;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posOS = IN.positionOS.xyz;
                float3 worldPos = TransformObjectToWorld(posOS);

                // Check if point is below the floor height
                if (worldPos.y < _FloorHeight)
                {
                    float3 worldOrigin = TransformObjectToWorld(float3(0.0, 0.0, 0.0)); 
                    
                    // Only project if camera is above the floor
                    if (worldOrigin.y > _FloorHeight)
                    {
                        float3 rayDir = worldPos - worldOrigin;
                        // Avoid division by zero if ray is perfectly horizontal
                        if (abs(rayDir.y) > 1e-6)
                        {
                            // Ray-plane intersection: t = (planeY - originY) / rayDirY
                            float t = (_FloorHeight - worldOrigin.y) / rayDir.y;
                            // Only project forward in time (t > 0)
                            if (t > 0.0)
                            {
                                worldPos = worldOrigin + rayDir * t;
                            }
                        }
                    }
                }

                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                int currentEyeIndex = (int)unity_StereoEyeIndex;

                // --- STEREO EYE INDEX FALLBACK HACK ---
                // If standard macros fail (remain 0), infer eye from projection matrix asymmetry.
                // UNITY_MATRIX_P._13 is the horizontal offset of the projection center.
                if (UNITY_MATRIX_P._13 > 0.0)
                {
                     currentEyeIndex = 1; // Right Eye
                }
                else if (UNITY_MATRIX_P._13 < 0.0)
                {
                     currentEyeIndex = 0; // Left Eye
                }
                // --------------------------------------

                if (currentEyeIndex != _StereoEyeIndex)
                {
                    clip(-1);
                }

                clip(IN.uv.x);
                clip(1.0 - IN.uv.x);
                clip(IN.uv.y);
                clip(1.0 - IN.uv.y);
                
                float val = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).r;
                val = pow(val, 0.75) * 4.0 - 0.5;

                return half4(val, val, val, 1.0);
            }
            ENDHLSL
        }
    }
}