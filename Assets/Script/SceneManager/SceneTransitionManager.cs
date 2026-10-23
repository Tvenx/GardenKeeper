
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string winCutSceneName = "WinCutScene";

    private bool isLoading;

    public bool IsLoading => isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadMainMenu()
    {
        LoadScene(mainMenuSceneName);
    }

    public void LoadGame()
    {
        LoadScene(gameSceneName);
    }

    public void LoadWinCutScene()
    {
        LoadScene(winCutSceneName);
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Scene name is empty.");
            return;
        }

        if (SceneManager.GetActiveScene().name == sceneName)
            return;

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError($"Failed to load scene: {sceneName}");
            isLoading = false;
            yield break;
        }

        yield return operation;

        isLoading = false;
    }
}
