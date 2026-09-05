using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
  public static Inventory Instance { get; private set; }

  [Header("Carry Capacity")]
  [Min(0f)] [SerializeField] private float maxWeight = 20f;
  public float MaxWeight { get => maxWeight; set => maxWeight = value; }

  [Header("Progression")]
  [SerializeField] private int playerMoney = 0;
  public int PlayerMoney { get => playerMoney; set => playerMoney = value; }
  [SerializeField] private int playerLevel = 1;
  public int PlayerLevel { get => playerLevel; set => playerLevel = value; }

  [Header("Current Run")]
  [SerializeField] private List<FurnitureItem> collectedFurniture = new List<FurnitureItem>();
  public List<FurnitureItem> CollectedFurniture { get => collectedFurniture; set => collectedFurniture = value; }

  public float CurrentWeight { get; private set; }
  public float RemainingWeight => Mathf.Max(0f, maxWeight - CurrentWeight);

  /// <summary>Total sale value of all furniture currently being carried.</summary>
  public int TotalRunValue
  {
    get
    {
      int total = 0;
      foreach (var item in collectedFurniture)
        if (item != null) total += item.SaleValue;
      return total;
    }
  }

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
  public static event System.Action<FurnitureItem> OnItemRemoved;
  public static event System.Action<string> OnInventoryMessage;

  public bool RemoveItem(FurnitureItem item)
  {
    if (item != null && collectedFurniture.Remove(item))
    {
      RecalculateWeight();
      OnItemRemoved?.Invoke(item);
      return true;
    }
    return false;
  }

  // ReSharper disable Unity.PerformanceAnalysis
  public bool AddItem(FurnitureItem item)
  {
    if (!item)
    {
      Debug.LogWarning("Cannot add a missing furniture item to the inventory.");
      return false;
    }

    if (CurrentWeight + item.Weight > maxWeight)
    {
      string msg = $"Too heavy! ({item.Weight:0.#} kg exceeds remaining {RemainingWeight:0.#} kg)";
      Debug.Log(msg);
      OnInventoryMessage?.Invoke(msg);
      return false;
    }

    collectedFurniture.Add(item);
    CurrentWeight += item.Weight;
    Debug.Log($"Picked up: {item.ItemName}. Carry weight: {CurrentWeight:0.##}/{maxWeight:0.##}");
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
        CurrentWeight += item.Weight;
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
