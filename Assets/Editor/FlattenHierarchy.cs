using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Place this file inside a folder named "Editor" anywhere in Assets/.
// Select the ROOT object in the Hierarchy, then use:
// GameObject > Flatten Children One Level
//
// root
//   parent > child
//   parent > child
// becomes
// root
//   child
//   child
//
// World position/rotation/scale of each child is preserved.
// The emptied "parent" objects are destroyed.
public static class FlattenHierarchy
{
  [MenuItem("GameObject/Flatten Children One Level", false, 0)]
  private static void Flatten()
  {
    GameObject root = Selection.activeGameObject;
    if (root == null)
    {
      Debug.LogError("FlattenHierarchy: no GameObject selected.");
      return;
    }

    List<Transform> parents = new List<Transform>();
    foreach (Transform parent in root.transform)
      parents.Add(parent);

    foreach (Transform parent in parents)
    {
      List<Transform> children = new List<Transform>();
      foreach (Transform child in parent)
        children.Add(child);

      foreach (Transform child in children)
        Undo.SetTransformParent(child, root.transform, "Flatten Hierarchy");

      Undo.DestroyObjectImmediate(parent.gameObject);
    }
  }

  [MenuItem("GameObject/Flatten Children One Level", true)]
  private static bool ValidateFlatten()
  {
    return Selection.activeGameObject != null;
  }
}
