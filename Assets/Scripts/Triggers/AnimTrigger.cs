using System;
using UnityEngine;

public class AnimTrigger : MonoBehaviour {
  [SerializeField] private Animator animator;
  [SerializeField] private string triggerName;
  [SerializeField] private bool playOnce = true;
  [Tooltip("If true, the proximity detector will beep when near this trigger.")]
  [SerializeField] private bool makeBeep = true;
  public bool MakeBeep { get => makeBeep; set => makeBeep = value; }
  public bool HasPlayed { get; private set; } = false;

  private void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Player")) {
      if (playOnce && !HasPlayed) {
        PlayAnim();
        HasPlayed = true;
      } else if (!playOnce && !HasPlayed) {
        PlayAnim();
        HasPlayed = true; // Mark as played so it stops beeping while inside
      }
    }
  }

  private void OnTriggerExit(Collider other) {
    if (other.CompareTag("Player")) {
      if (!playOnce) {
        HasPlayed = false; // Reset so it can beep again next time we approach
      }
    }
  }

  private void PlayAnim() {
    animator.SetTrigger(triggerName);
  }
}
