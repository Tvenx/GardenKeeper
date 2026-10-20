
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;

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
        if (pauseAction != null)
            pauseAction.action.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        if (pauseAction != null)
            pauseAction.action.performed -= OnPausePerformed;

        if (isPaused)
            ResumeGame();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (!isPaused)
        {
            PauseGame();
            return;
        }

        // Escape returns to the previous tab.
        if (currentPanel != pausePanel)
        {
            Back();
        }
        else
        {
            ResumeGame();
        }
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
        isPaused = false;
        Time.timeScale = 1f;

        panelHistory.Clear();
        SetAllPanelsInactive();
        currentPanel = pausePanel;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
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
