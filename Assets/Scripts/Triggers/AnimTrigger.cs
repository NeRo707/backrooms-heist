using System;
using UnityEngine;

public class AnimTrigger : MonoBehaviour {

    public Animator animator;
    public string triggerName;

    private void OnTriggerEnter(Collider other) {
      if(other.CompareTag("Player")) {
        animator.SetTrigger(triggerName);
      }
    }
}
