using UnityEngine;
using System.Collections.Generic;

public class PanelManager : MonoBehaviour
{
    public UIPanel panelPrefab;

    void Start()
    {
        // Find the main canvas in the scene to parent the UI to.
        // It's good practice to have a dedicated canvas for dynamic UI.
        // Ensure you have a Canvas in your scene with the tag "MainCanvas".
        GameObject canvasGO = GameObject.FindGameObjectWithTag("MainCanvas");
        if (canvasGO == null)
        {
            Debug.LogError("PanelManager: Could not find a GameObject with the 'MainCanvas' tag.");
            return;
        }
        // Instantiate the panel
        UIPanel panelInstance = Instantiate(panelPrefab, canvasGO.transform);

        // Prepare the data
        string title = "Welcome!";
        string description = "This is a dynamically generated UI panel. Please choose an option below.";
        var buttons = new List<UIPanel.ButtonData>
        {
            new UIPanel.ButtonData
            {
                Text = "Option 1",
                OnClick = new UnityEngine.Events.UnityEvent()
            },
            new UIPanel.ButtonData
            {
                Text = "Close",
                OnClick = new UnityEngine.Events.UnityEvent()
            }
        };

        // Add listeners for button clicks
        buttons[0].OnClick.AddListener(() => Debug.Log("Option 1 Clicked!"));
        buttons[1].OnClick.AddListener(() => Destroy(panelInstance.gameObject));

        // Initialize the panel
        panelInstance.Initialize(title, description, buttons);
    }
}
