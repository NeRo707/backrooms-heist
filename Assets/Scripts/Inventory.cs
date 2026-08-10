using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
  public static Inventory Instance { get; private set; }

  [Header("Carry Capacity")]
  [Min(0f)] public float maxWeight = 20f;

  [Header("Current Run")]
  public List<FurnitureItem> collectedFurniture = new List<FurnitureItem>();

  public float CurrentWeight { get; private set; }
  public float RemainingWeight => Mathf.Max(0f, maxWeight - CurrentWeight);

  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);
    RecalculateWeight();
  }

  public static event System.Action<FurnitureItem> OnItemCollected;
  public static event System.Action<string> OnInventoryMessage;

  public bool AddItem(FurnitureItem item)
  {
    if (item == null)
    {
      Debug.LogWarning("Cannot add a missing furniture item to the inventory.");
      return false;
    }

    if (CurrentWeight + item.weight > maxWeight)
    {
      string msg = $"Too heavy! ({item.weight:0.#} kg exceeds remaining {RemainingWeight:0.#} kg)";
      Debug.Log(msg);
      OnInventoryMessage?.Invoke(msg);
      return false;
    }

    collectedFurniture.Add(item);
    CurrentWeight += item.weight;
    Debug.Log($"Picked up: {item.itemName}. Carry weight: {CurrentWeight:0.##}/{maxWeight:0.##}");
    OnItemCollected?.Invoke(item);
    return true;
  }

  public void ClearCurrentRun()
  {
    collectedFurniture.Clear();
    CurrentWeight = 0f;
  }

  private void OnDestroy()
  {
    if (Instance == this)
    {
      Instance = null;
    }
  }

  private void RecalculateWeight()
  {
    CurrentWeight = 0f;

    foreach (FurnitureItem item in collectedFurniture)
    {
      if (item != null)
      {
        CurrentWeight += item.weight;
      }
    }
  }
}

public static class InventoryBootstrap
{
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  private static void CreateInventory()
  {
    if (Inventory.Instance != null)
    {
      return;
    }

    var inventoryObject = new GameObject("Inventory");
    inventoryObject.AddComponent<Inventory>();
  }
}
