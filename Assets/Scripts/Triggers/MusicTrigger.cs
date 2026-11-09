using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// One-shot trigger that starts or stops a music AudioSource when the player enters.
/// Use two of these (one Play, one Stop) for a music zone.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MusicTrigger : MonoBehaviour {
  private enum MusicAction { Play, Stop }

  [SerializeField] private AudioSource music;
  [SerializeField] private MusicAction action = MusicAction.Play;
  [Tooltip("Stop only: seconds to fade out. 0 = cut instantly.")]
  [SerializeField] private float stopFadeSeconds = 0f;

  private bool _fired;

  private void Awake() {
    if (music == null)
      throw new Exception($"{name}: MusicTrigger requires a music AudioSource.");
    if (!GetComponent<Collider>().isTrigger)
      throw new Exception($"{name}: MusicTrigger collider must have Is Trigger enabled.");
  }

  private void OnTriggerEnter(Collider other) {
    if (_fired || !other.CompareTag("Player")) return;
    _fired = true;

    if (action == MusicAction.Play) {
      music.Play();
    } else if (stopFadeSeconds <= 0f) {
      music.Stop();
    } else {
      StartCoroutine(FadeOutAndStop());
    }
  }

  private IEnumerator FadeOutAndStop() {
    float startVolume = music.volume;
    float elapsed = 0f;
    while (elapsed < stopFadeSeconds) {
      elapsed += Time.deltaTime;
      music.volume = Mathf.Lerp(startVolume, 0f, elapsed / stopFadeSeconds);
      yield return null;
    }
    music.Stop();
    music.volume = startVolume;
  }
}
