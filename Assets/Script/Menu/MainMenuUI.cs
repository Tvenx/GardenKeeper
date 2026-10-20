
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private InputActionReference backAction;

    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Settings Panels")]
    [SerializeField] private GameObject resolutionPanel;
    [SerializeField] private GameObject soundPanel;
    [SerializeField] private GameObject languagePanel;

    [Header("Game")]
    [SerializeField] private string gameSceneName = "Game";

    private readonly Stack<GameObject> panelHistory = new();

    private GameObject currentPanel;

    private void Start()
    {
        ShowMainMenu();
    }

    private void OnEnable()
    {
        if (backAction != null)
            backAction.action.performed += OnBackPerformed;
    }

    private void OnDisable()
    {
        if (backAction != null)
            backAction.action.performed -= OnBackPerformed;
    }

    private void OnBackPerformed(InputAction.CallbackContext context)
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        panelHistory.Clear();
        ShowPanel(mainMenuPanel, false);
    }

    public void ShowSettings()
    {
        OpenPanel(settingsPanel);
    }

    public void ShowResolution()
    {
        OpenPanel(resolutionPanel);
    }

    public void ShowSound()
    {
        OpenPanel(soundPanel);
    }

    public void ShowLanguage()
    {
        OpenPanel(languagePanel);
    }

    public void Back()
    {
        if (panelHistory.Count == 0)
        {
            ShowMainMenu();
            return;
        }

        GameObject previousPanel = panelHistory.Pop();
        ShowPanel(previousPanel, false);
    }

    public void StartGame()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadGame();
        }
        else
        {
            Debug.LogError("SceneTransitionManager not found.");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void OpenPanel(GameObject panel)
    {
        if (currentPanel == panel)
            return;

        if (currentPanel != null)
        {
            panelHistory.Push(currentPanel);
        }

        ShowPanel(panel, false);
    }

    private void ShowPanel(GameObject panel, bool saveCurrent)
    {
        if (saveCurrent && currentPanel != null)
        {
            panelHistory.Push(currentPanel);
        }

        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        resolutionPanel.SetActive(false);
        soundPanel.SetActive(false);
        languagePanel.SetActive(false);

        panel.SetActive(true);
        currentPanel = panel;
    }


}
