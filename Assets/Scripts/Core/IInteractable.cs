using UnityEngine;

/// <summary>
/// Contract for anything the player can interact with via the [E] key.
/// Implement on doors, keys, switches, levers, etc.
/// </summary>
public interface IInteractable
{
    /// <summary>Prompt string shown in the HUD when the player is looking at this object.</summary>
    string InteractPrompt { get; }

    /// <summary>Called when the player presses the interact key while hovering over this object.</summary>
    /// <param name="interactor">The player GameObject performing the interaction.</param>
    void Interact(GameObject interactor);

    /// <summary>
    /// Returns false if interaction should be blocked (e.g. locked door without a key).
    /// The prompt is still shown; the object can explain the block inside Interact().
    /// </summary>
    bool CanInteract(GameObject interactor);
}
