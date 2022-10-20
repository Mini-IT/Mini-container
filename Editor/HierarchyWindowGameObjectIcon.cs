using UnityEditor;
using UnityEngine;

namespace MiniContainer.Editor
{
	[InitializeOnLoad]
	public static class HierarchyWindowGameObjectIcon
	{
		private const string IgnoreIcons = "d_GameObject Icon, d_Prefab Icon";

		static HierarchyWindowGameObjectIcon()
		{
			EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
		}

		private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
		{
			var content = EditorGUIUtility.ObjectContent(EditorUtility.InstanceIDToObject(instanceID), null);

			if (content.image != null && !IgnoreIcons.Contains(content.image.name))
				GUI.DrawTexture(new Rect(selectionRect.xMax - 16, selectionRect.yMin, 20, 20), content.image);

		}
	}
}