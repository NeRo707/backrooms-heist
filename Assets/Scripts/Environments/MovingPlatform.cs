using UnityEngine;

public class MovingPlatform : MonoBehaviour {
  public Transform pointA;
  public Transform pointB;
  public float speed = 3f;

  private Vector3 currentTarget;
  private Vector3 lastPosition;
  private CharacterController playerOnPlatform;

  void Start() {
    transform.position = pointA.position;
    currentTarget = pointB.position;
    lastPosition = transform.position;
  }

  void LateUpdate() {
    transform.position = Vector3.MoveTowards(transform.position, currentTarget, speed * Time.deltaTime);

    Vector3 distanceMoved = transform.position - lastPosition;

    if (playerOnPlatform != null) {
      playerOnPlatform.Move(distanceMoved);
    }

    lastPosition = transform.position;

    if (Vector3.Distance(transform.position, currentTarget) < 0.01f) {
      if (currentTarget == pointB.position) {
        currentTarget = pointA.position;
      } else {
        currentTarget = pointB.position;
      }
    }
  }

  private void OnTriggerEnter(Collider other) {
    CharacterController controller = other.GetComponent<CharacterController>();

    if (controller != null) {
      playerOnPlatform = controller;
    }
  }

  private void OnTriggerExit(Collider other) {
    CharacterController controller = other.GetComponent<CharacterController>();

    if (controller != null) {
      playerOnPlatform = null;
    }
  }
}
