using UnityEngine;
using UnityEditor;

public class FuelSpawnEditor : EditorWindow
{
    private GameObject fuelPrefab;
    private Transform containerParent;

    private Vector3Int gridDimensions = new Vector3Int(3, 2, 3);
    private Vector3 spacing = new Vector3(0.3f, 0.3f, 0.3f);

    private bool addRandomJitter = false;
    private Vector3 maxJitter = new Vector3(0.02f, 0.02f, 0.02f);

    [MenuItem("Tools/Fuel Array Spawner")]
    public static void ShowWindow()
    {
        GetWindow<FuelSpawnEditor>("Fuel Spawner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab & Parent Setup", EditorStyles.boldLabel);
        fuelPrefab = (GameObject)EditorGUILayout.ObjectField("Fuel Prefab", fuelPrefab, typeof(GameObject), false);
        containerParent = (Transform)EditorGUILayout.ObjectField("Container Parent", containerParent, typeof(Transform), true);

        EditorGUILayout.Space();
        GUILayout.Label("Grid Setup", EditorStyles.boldLabel);
        gridDimensions = EditorGUILayout.Vector3IntField("Count (X, Y, Z)", gridDimensions);
        spacing = EditorGUILayout.Vector3Field("Spacing", spacing);

        EditorGUILayout.Space();
        GUILayout.Label("Random Jitter", EditorStyles.boldLabel);
        addRandomJitter = EditorGUILayout.Toggle("Add Jitter", addRandomJitter);
        if (addRandomJitter)
        {
            maxJitter = EditorGUILayout.Vector3Field("Max Jitter Offset", maxJitter);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Spawn Prefab Array", GUILayout.Height(30)))
        {
            SpawnArray();
        }

        if (containerParent != null && GUILayout.Button("Clear Container Children"))
        {
            ClearContainer();
        }
    }

    private void SpawnArray()
    {
        if (fuelPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Fuel Prefab first!", "OK");
            return;
        }

        // If no container assigned, create one automatically
        if (containerParent == null)
        {
            GameObject container = new GameObject("Fuel_Container");
            Undo.RegisterCreatedObjectUndo(container, "Create Fuel Container");
            containerParent = container.transform;
        }

        Vector3 originPos = Selection.activeTransform != null ? Selection.activeTransform.position : Vector3.zero;
        int totalSpawned = 0;

        // Group all instantiations under one undo step
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Spawn Fuel Prefabs");
        int undoGroupIndex = Undo.GetCurrentGroup();

        for (int x = 0; x < gridDimensions.x; x++)
        {
            for (int y = 0; y < gridDimensions.y; y++)
            {
                for (int z = 0; z < gridDimensions.z; z++)
                {
                    Vector3 localPos = new Vector3(
                        (x - (gridDimensions.x - 1) * 0.5f) * spacing.x,
                        y * spacing.y,
                        (z - (gridDimensions.z - 1) * 0.5f) * spacing.z
                    );

                    if (addRandomJitter)
                    {
                        localPos += new Vector3(
                            Random.Range(-maxJitter.x, maxJitter.x),
                            Random.Range(-maxJitter.y, maxJitter.y),
                            Random.Range(-maxJitter.z, maxJitter.z)
                        );
                    }

                    Vector3 spawnPos = originPos + localPos;

                    // Instantiate as a proper Prefab instance linked to the source asset
                    GameObject newFuel = (GameObject)PrefabUtility.InstantiatePrefab(fuelPrefab, containerParent);
                    newFuel.transform.position = spawnPos;
                    newFuel.name = $"{fuelPrefab.name}_{x}_{y}_{z}";

                    // Register for Undo support
                    Undo.RegisterCreatedObjectUndo(newFuel, "Spawn Fuel Piece");

                    totalSpawned++;
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroupIndex);
        Debug.Log($"Spawned {totalSpawned} connected Prefabs under '{containerParent.name}'!");
    }

    private void ClearContainer()
    {
        if (containerParent == null) return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Clear Fuel Container");
        int undoGroupIndex = Undo.GetCurrentGroup();

        for (int i = containerParent.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(containerParent.GetChild(i).gameObject);
        }

        Undo.CollapseUndoOperations(undoGroupIndex);
    }
}