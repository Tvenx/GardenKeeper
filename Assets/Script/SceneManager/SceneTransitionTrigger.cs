using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTrigger : MonoBehaviour
{
    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
        {
            return;
        }

        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("There is no next scene in Build Settings.", this);
            return;
        }

        string nextScenePath =
            SceneUtility.GetScenePathByBuildIndex(nextSceneIndex);
        string nextSceneName = Path.GetFileNameWithoutExtension(nextScenePath);
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError(
                $"Could not resolve scene at build index {nextSceneIndex}.",
                this
            );
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("SceneTransitionManager not found.", this);
            return;
        }

        hasTriggered = true;
        SceneTransitionManager.Instance.LoadScene(nextSceneName);
    }
}
