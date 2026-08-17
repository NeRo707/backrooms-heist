using System;
using UnityEngine;

  public class ScaryPass : MonoBehaviour {
    public GameObject scary;

    private void OnTriggerEnter(Collider other) {
      if(other.CompareTag("Player")) {
        scary.SetActive(true);
      }
    }
  }
