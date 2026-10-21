using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRestarter : MonoBehaviour
{
    private void Update()
    {
        // Проверяем нажатие клавиши F12
        if (Input.GetKeyDown(KeyCode.F12))
        {
            RestartScene();
        }
    }

    /// <summary>
    /// Перезапускает текущую активную сцену.
    /// </summary>
    private void RestartScene()
    {
        // Получаем индекс текущей сцены
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Загружаем сцену заново
        SceneManager.LoadScene(currentSceneIndex);

        // Альтернативный вариант — по имени сцены:
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}