using UnityEngine;
using Valve.VR;

public class UIPointer : MonoBehaviour
{
    public SteamVR_Action_Boolean uiClick;

    public GameObject hitVisual;

    void Update()
    {
        // Cast and find UI elements (UI layer only) to interact with
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 10.0f, LayerMask.GetMask("UI")))
        {
            hitVisual.SetActive(true);
            hitVisual.transform.position = hit.point;
            // Check if the hit object is a button
            var button = hit.transform.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                // If the UI click action is pressed, invoke the button's onClick event
                if (uiClick.GetStateDown(SteamVR_Input_Sources.Any))
                {
                    button.onClick.Invoke();
                }
            }
        }
        else
        {
            hitVisual.SetActive(false);
        }
    }
}
