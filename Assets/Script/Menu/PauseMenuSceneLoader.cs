using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuSceneLoader : MonoBehaviour
{
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private string pauseSceneName = "PauseMenu";

    private bool isLoading;

    private void OnEnable()
    {
        if (pauseAction == null)
        {
            Debug.LogError("PauseMenuSceneLoader: Pause action is not assigned.");
            return;
        }

        if (!pauseAction.action.enabled)
            pauseAction.action.Enable();

        pauseAction.action.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        if (pauseAction == null)
            return;

        pauseAction.action.performed -= OnPausePerformed;
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        Scene pauseScene = SceneManager.GetSceneByName(pauseSceneName);
        if (pauseScene.IsValid() && pauseScene.isLoaded)
        {
            PauseMenuUI pauseMenu = FindPauseMenu(pauseScene);
            if (pauseMenu != null)
                pauseMenu.HandlePauseInput();
            return;
        }

        if (isLoading)
            return;

        isLoading = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        AsyncOperation operation = SceneManager.LoadSceneAsync(pauseSceneName, LoadSceneMode.Additive);
        if (operation == null)
        {
            isLoading = false;
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Debug.LogError($"PauseMenuSceneLoader: Failed to load scene '{pauseSceneName}'.");
            return;
        }

        operation.completed += OnPauseSceneLoaded;
    }

    private void OnPauseSceneLoaded(AsyncOperation operation)
    {
        isLoading = false;
        Scene pauseScene = SceneManager.GetSceneByName(pauseSceneName);
        if (!pauseScene.IsValid() || !pauseScene.isLoaded)
        {
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private static PauseMenuUI FindPauseMenu(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PauseMenuUI pauseMenu = root.GetComponentInChildren<PauseMenuUI>(true);
            if (pauseMenu != null)
                return pauseMenu;
        }

        return null;
    }
}
