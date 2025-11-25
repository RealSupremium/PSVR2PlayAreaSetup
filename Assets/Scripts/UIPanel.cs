using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Manages a UI panel with a title, description, and a dynamic list of buttons.
/// </summary>
public class UIPanel : MonoBehaviour
{
    [System.Serializable]
    public struct ButtonData
    {
        public string Text;
        public UnityEvent OnClick;
    }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;

    private List<GameObject> m_InstantiatedButtons = new List<GameObject>();

    /// <summary>
    /// Initializes the panel with the provided data.
    /// </summary>
    /// <param name="title">The title to display.</param>
    /// <param name="description">The description to display.</param>
    /// <param name="buttons">A list of button configurations.</param>
    public void Initialize(string title, string description, List<ButtonData> buttons)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        // Clear any previously created buttons
        foreach (var button in m_InstantiatedButtons)
        {
            Destroy(button);
        }
        m_InstantiatedButtons.Clear();

        if (buttonPrefab == null || buttonContainer == null)
        {
            Debug.LogWarning("Button Prefab or Container not set on UIPanel.", this);
            return;
        }

        // Deactivate the prefab template before instantiating
        buttonPrefab.SetActive(false);

        // Create a button for each item in the list
        foreach (var buttonData in buttons)
        {
            Debug.Log($"Creating button: {buttonData.Text}");

            GameObject buttonGO = Instantiate(buttonPrefab, buttonContainer);
            buttonGO.name = $"Button - {buttonData.Text}";

            // Set button text
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = buttonData.Text;
            }

            // Set button click event
            Button buttonComponent = buttonGO.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() => buttonData.OnClick.Invoke());
            }
            
            buttonGO.SetActive(true);
            m_InstantiatedButtons.Add(buttonGO);
        }
    }
}
