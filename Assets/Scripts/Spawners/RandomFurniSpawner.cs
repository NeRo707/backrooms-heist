using System;
using System.Collections.Generic;
using UnityEngine;

public class RandomFurniSpawner : MonoBehaviour {
  [SerializeField] private List<Transform> furniPositions;
  [SerializeField] private List<GameObject> furniPrefabs;
  [SerializeField] private List<FurnitureItem> furnitureItems;


  private void Start() {
    if (furniPrefabs.Count != furnitureItems.Count) {
      Debug.LogError("RandomFurniSpawner needs one FurnitureItem for every furniture prefab.", this);
      return;
    }

    // spawn furniture at random position
    for (int i = 0; i < furniPositions.Count; i++) {
      var randomIndex = UnityEngine.Random.Range(0, furniPrefabs.Count);
      var furni = furniPrefabs[randomIndex];

      Vector3 spawnPos = furniPositions[i].position;
      spawnPos.y += 0.3f;

      Rigidbody rb = furni.GetComponent<Rigidbody>();
      if (rb == null) rb = furni.AddComponent<Rigidbody>();
      rb.mass = 1f;

      var spawnedFurniture = Instantiate(furni, spawnPos, furni.transform.rotation);
      var pickupItem = spawnedFurniture.GetComponent<PickupItem>();

      if (pickupItem == null) {
        pickupItem = spawnedFurniture.AddComponent<PickupItem>();
      }

      pickupItem.ItemData = furnitureItems[randomIndex];
    }
  }
}
