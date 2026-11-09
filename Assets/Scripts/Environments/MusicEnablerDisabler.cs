using System;
using UnityEngine;

public class MusicEnablerDisabler : MonoBehaviour {
  [SerializeField]
  private AudioSource Music;
  [SerializeField]
  private Collider EnablerCollider;
  [SerializeField]
  private Collider DisablerCollider;

  private void OnTriggerEnter(Collider other) {
    if (!other.CompareTag("Player")) return;

    if (other == EnablerCollider) {
      if (!Music.isPlaying) {
        Music.Play();
      }
    } else if (other == DisablerCollider) {
      if (Music.isPlaying) {
        Music.Stop();
      }
    }
  }
}
