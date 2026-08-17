using UnityEngine;
using UnityEngine.Playables; // Required for Timeline

public class CinematicController : MonoBehaviour {
  [Header("Cinematic Reference")]
  public PlayableDirector director;
  private bool isPlaying = false;
  public GameObject clark;

  void Update() {
    if (isPlaying && Input.GetKeyDown(KeyCode.Space)) {
      SkipCinematic();
    }
  }

  private void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Player") && director != null && !isPlaying) {
      PlayCinematic();
      GetComponent<Collider>().enabled = false;
    }
  }

  public void PlayCinematic() {
    isPlaying = true;
    director.Play();

    director.stopped += OnCinematicFinished;
  }

  public void SkipCinematic() {
    if (!isPlaying) return;

    // Instantly jump to the end of the timeline
    director.time = director.duration;
    director.Evaluate(); // Forces Unity to apply the final frame's state immediately
    director.Stop(); // Stops the director

    clark.SetActive(true);
  }

  private void OnCinematicFinished(PlayableDirector aDirector) {
    if (aDirector == director) {
      isPlaying = false;
      director.stopped -= OnCinematicFinished; // Clean up the event listener
      clark.SetActive(true);

      // Optional: Add any logic here that should happen after the cinematic ends
    }
  }
}
