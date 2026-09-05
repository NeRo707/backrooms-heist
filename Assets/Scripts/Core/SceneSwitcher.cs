using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
  public enum SceneOptions
  {
    Store,
    Backrooms,
  }

  [SerializeField] private SceneOptions sceneToLoad;
  [SerializeField] private LoadingScene loadingSceneController;

  private void Start()
  {
    if (loadingSceneController == null)
    {
      loadingSceneController = FindFirstObjectByType<LoadingScene>();
    }
  }

  private void OnTriggerEnter(Collider other)
  {
    if (other.CompareTag("Player"))
    {
      if (loadingSceneController != null)
      {
        loadingSceneController.LoadScene((int)sceneToLoad);
      }
      else
      {
        Debug.LogWarning("LoadingScene controller not found. Loading scene synchronously.");
        SceneManager.LoadScene((int)sceneToLoad);
      }
    }
  }
}
