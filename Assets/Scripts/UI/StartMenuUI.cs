using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Controls the initial Start Menu screen with the PLAY button.
/// </summary>
public class StartMenuUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject startMenuPanel;
    public Button playButton;

    public event Action OnPlayClicked;

    private void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(HandlePlayClicked);
        }
    }

    private void HandlePlayClicked()
    {
        OnPlayClicked?.Invoke();
    }

    public void Show()
    {
        if (startMenuPanel != null)
            startMenuPanel.SetActive(true);
    }

    public void Hide()
    {
        if (startMenuPanel != null)
            startMenuPanel.SetActive(false);
    }
}
