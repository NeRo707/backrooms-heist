using System;
using UnityEngine;

  public class ScaryPass : MonoBehaviour {
    [SerializeField] private GameObject scary;

    private void OnTriggerEnter(Collider other) {
      if(other.CompareTag("Player")) {
        scary.SetActive(true);
      }
    }
  }
