using UnityEngine;
using UnityEditor;

public class SnapToMeshCenter
{
    [MenuItem("Tools/Snap Selected Object to Child Mesh Center")]
    public static void AlignToCenter()
    {
        GameObject activeObj = Selection.activeGameObject;
        if (activeObj == null) return;

        MeshFilter mf = activeObj.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("No MeshFilter found in selected object or its children!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(activeObj, "Snap To Mesh Center");

        // 1. Store and unparent all children so they don't move in world space
        int childCount = activeObj.transform.childCount;
        Transform[] children = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            children[i] = activeObj.transform.GetChild(i);
        }

        foreach (Transform child in children)
        {
            child.SetParent(null, true); // true = keep current world position
        }

        // 2. Move parent object to exact geometric center of the target CAD mesh
        Vector3 worldCenter = mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
        activeObj.transform.position = worldCenter;

        // 3. Re-parent children back to the newly positioned parent
        foreach (Transform child in children)
        {
            child.SetParent(activeObj.transform, true);
        }

        Debug.Log($"Pivot centered to {worldCenter} without shifting visuals!");
    }
}