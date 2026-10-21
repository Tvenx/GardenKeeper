
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference backAction;

    [Header("Pause Menu")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Settings Panels")]
    [SerializeField] private GameObject resolutionPanel;
    [SerializeField] private GameObject soundPanel;

    private readonly Stack<GameObject> panelHistory = new();

    private GameObject currentPanel;
    private bool isPaused;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        SetAllPanelsInactive();
        currentPanel = pausePanel;
    }

    private void OnEnable()
    {
        if (backAction != null)
        {
            backAction.action.performed += OnBackPerformed;
            backAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (backAction != null)
        {
            backAction.action.performed -= OnBackPerformed;
        }

        if (isPaused)
            RestoreGameState();
    }

    private void OnBackPerformed(InputAction.CallbackContext context)
    {
        if (isPaused)
            HandlePauseInput();
        else
            PauseGame();
    }

    public void HandlePauseInput()
    {
        if (!isPaused)
            return;

        if (currentPanel != pausePanel)
            Back();
        else
            ResumeGame();
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        panelHistory.Clear();
        ShowPanel(pausePanel);
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        RestoreGameState();
    }

    public void ShowPauseMenu()
    {
        panelHistory.Clear();
        ShowPanel(pausePanel);
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

    public void Back()
    {
        if (panelHistory.Count == 0)
        {
            ShowPanel(pausePanel);
            return;
        }

        ShowPanel(panelHistory.Pop());
    }

    public void ExitToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;

        panelHistory.Clear();
        SetAllPanelsInactive();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogError("SceneTransitionManager not found.");
        }
    }

    private void OpenPanel(GameObject panel)
    {
        if (panel == null || panel == currentPanel)
            return;

        if (currentPanel != null)
            panelHistory.Push(currentPanel);

        ShowPanel(panel);
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogError("PauseMenuUI: Panel is not assigned.");
            return;
        }

        SetAllPanelsInactive();
        panel.SetActive(true);
        currentPanel = panel;
    }

    private void RestoreGameState()
    {
        isPaused = false;
        Time.timeScale = 1f;

        panelHistory.Clear();
        SetAllPanelsInactive();

        currentPanel = pausePanel;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void SetAllPanelsInactive()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (resolutionPanel != null)
            resolutionPanel.SetActive(false);

        if (soundPanel != null)
            soundPanel.SetActive(false);
    }
}
