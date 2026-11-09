using System;
using UnityEngine;

public class LightbulbExploder : MonoBehaviour {
  [SerializeField]
  private Light lightToDisable;
  [SerializeField]
  private AudioSource lightbulbBreakSound;

  private bool _exploded = false;

  private void OnTriggerEnter(Collider other) {
    if (!other.CompareTag("Player") || _exploded) return;
    lightbulbBreakSound.Play();
    lightToDisable.enabled = false;
    _exploded = true;
  }
}
