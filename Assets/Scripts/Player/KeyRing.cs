using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that tracks which key IDs the player is currently carrying.
/// Attach to the Player GameObject (or a persistent manager object).
/// Keys are identified by string IDs that must match the DoorController's requiredKeyId.
/// </summary>
public class KeyRing : MonoBehaviour
{
    public static KeyRing Instance { get; private set; }

    // Internal set of collected key IDs
    private readonly HashSet<string> _keys = new HashSet<string>();

    private void Awake()
    {
        // Singleton enforcement — destroy duplicate
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Add a key to the ring. Duplicate IDs are silently ignored.</summary>
    public void AddKey(string keyId) => _keys.Add(keyId);

    /// <summary>Returns true if the player is carrying the key with the given ID.</summary>
    public bool HasKey(string keyId) => _keys.Contains(keyId);

    /// <summary>Remove a key from the ring (use when a door consumes its key on unlock).</summary>
    public void RemoveKey(string keyId) => _keys.Remove(keyId);

    /// <summary>Returns a read-only snapshot of all currently held key IDs (useful for save/load).</summary>
    public IReadOnlyCollection<string> GetAllKeys() => _keys;
}
