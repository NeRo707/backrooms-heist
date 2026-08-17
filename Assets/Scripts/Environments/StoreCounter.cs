using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Place on the store counter in Store.unity.
/// Allows player to sell all carried furniture for cash upon pressing E.
/// </summary>
public class StoreCounter : MonoBehaviour
{
  [Header("Interaction Settings")]
  public float interactDistance = 3.0f;
  public string promptMessage = "Press [E] to Sell Carried Furniture";

  [Header("Audio (Optional)")]
  [SerializeField] private AudioSource audioSource;
  [SerializeField] private AudioClip sellSound;

  private bool _playerInRange = false;

  private void Start()
  {
    if (audioSource == null)
    {
      audioSource = GetComponent<AudioSource>();
    }
  }

  private void OnTriggerEnter(Collider other)
  {
    if (other.CompareTag("Player"))
    {
      _playerInRange = true;
      ShowPrompt();
    }
  }

  private void OnTriggerExit(Collider other)
  {
    if (other.CompareTag("Player"))
    {
      _playerInRange = false;
    }
  }

  private void Update()
  {
    if (!_playerInRange) return;

    bool interactPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
    {
      interactPressed = true;
    }

    if (interactPressed)
    {
      SellFurniture();
    }
  }

  public void SellFurniture()
  {
    if (Inventory.Instance == null) return;

    int itemCount = Inventory.Instance.collectedFurniture.Count;
    int earnings = Inventory.Instance.TotalRunValue;

    if (itemCount <= 0)
    {
      HeistHUDManager.Instance?.ShowToastMessage("<color=#FFAA00>No furniture to sell!</color>");
      return;
    }

    // Cash out
    Inventory.Instance.playerMoney += earnings;
    Inventory.Instance.ClearCurrentRun();

    // Play sale sound
    if (audioSource != null && sellSound != null)
    {
      audioSource.PlayOneShot(sellSound);
    }

    // Feedback
    HeistHUDManager.Instance?.ShowToastMessage($"<color=#55FF55>Sold {itemCount} item(s) for ${earnings:N0}!</color>");
  }

  private void ShowPrompt()
  {
    if (Inventory.Instance != null)
    {
      int earnings = Inventory.Instance.TotalRunValue;
      int count = Inventory.Instance.collectedFurniture.Count;

      if (count > 0)
      {
        HeistHUDManager.Instance?.ShowToastMessage($"[E] Sell {count} furniture item(s) for <color=#55FF55>${earnings:N0}</color>");
      }
      else
      {
        HeistHUDManager.Instance?.ShowToastMessage("[E] Counter (No furniture carried)");
      }
    }
  }

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.green;
    Gizmos.DrawWireSphere(transform.position, interactDistance);
  }
}
