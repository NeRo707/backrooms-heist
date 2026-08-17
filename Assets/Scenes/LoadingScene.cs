using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScene : MonoBehaviour {
  public GameObject LoadingScreen;
  public Image LoadingBarFill;

  public void LoadScene(int id) {
    StartCoroutine(LoadSceneAsync(id));
  }

  IEnumerator LoadSceneAsync(int sceneId) {
    // Show the loading screen first
    LoadingScreen.SetActive(true);

    // Wait until the next frame so the UI has a chance to actually render on screen
    yield return null;

    // Now start the heavy loading process
    AsyncOperation operation = SceneManager.LoadSceneAsync(sceneId);

    while (!operation.isDone) {
      float progress = Mathf.Clamp01(operation.progress / 0.9f);
      LoadingBarFill.fillAmount = progress;

      yield return null;
    }
  }
}
