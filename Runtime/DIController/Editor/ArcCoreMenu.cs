using ArcCore;
using UnityEditor;
using UnityEngine;

public class ArcCoreMenu
{
    // Add a menu item to create custom GameObjects.
    // Priority 1 ensures it is grouped with the other menu items of the same kind
    // and propagated to the hierarchy dropdown and hierarchy context menus.
    [MenuItem("GameObject/ArcCore/CompositionRoot", false, 10)]
    public static void CreateCompositionRoot(MenuCommand menuCommand)
    {
        // Create a custom game object
        var go = new GameObject("CompositionRoot");
        go.AddComponent<CompositionRoot>();
        // Ensure it gets reparented if this was a context click (otherwise does nothing)
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        // Register the creation in the undo system
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }

    [MenuItem("GameObject/ArcCore/SubContainer", false, 10)]
    public static void CreateSceneRoot(MenuCommand menuCommand)
    {
        // Create a custom game object
        var go = new GameObject("SubContainer");
        go.AddComponent<SubContainer>();
        // Ensure it gets reparented if this was a context click (otherwise does nothing)
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        // Register the creation in the undo system
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }
}
