using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TEnemyManager : MonoBehaviour {
  [SerializeField] private List<TEnemy> Enemies;

  public void Awake() {
    FindAliveEnemies();
    FindEnemyDescendingByDamage();
    FindEnemiesWithLevelGreaterThan5();
    FindEnemiesWithHealthLessThan50();
    FindAliveEnemiesNames();
  }

  private void FindAliveEnemies() {
    var x = Enemies.Where(e => e.IsAlive).ToList();

    foreach (var enemy in x) {
      print(enemy);
    }
  }

  private void FindEnemyDescendingByDamage() {
    var x = Enemies.OrderByDescending(e => e.Damage).ToList();

    foreach (var enemy in x) {
      print(enemy);
    }
  }

  private void FindEnemiesWithLevelGreaterThan5() {
    var x = Enemies.Where(e => e.Level > 5).ToList();

    foreach (var enemy in x) {
      print(enemy);
    }
  }

  private void FindEnemiesWithHealthLessThan50() {
    var x = Enemies.Where(e => e.Health < 50).ToList();

    foreach (var enemy in x) {
      print(enemy);
    }
  }

  private void FindAliveEnemiesNames() {
    var x = Enemies.Where(e => e.IsAlive).Select(e => e.Name).ToList();

    foreach (var enemy in x) {
      print(enemy);
    }
  }
}
