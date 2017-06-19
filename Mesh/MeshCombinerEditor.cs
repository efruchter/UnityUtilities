using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Within.Sequence;
using System.Linq;
using UnityEditor.SceneManagement;

/// <summary>
/// Mesh combiner editor.
/// -Eric
/// </summary>
public class MeshCombinerEditor : EditorWindow
{
    private int combinedMeshes = 1;

    [MenuItem("Window/Mesh Combiner")]
    static void Init()
    {
        MeshCombinerEditor window = (MeshCombinerEditor)EditorWindow.GetWindow(typeof(MeshCombinerEditor));
        window.Show();
        window.titleContent.text = "Mesh Combiner";
    }

    void OnGUI()
    {
        GUILayout.Label ("Combine Meshes");
        GUILayout.Label ("1. Select Meshes in Scene");
        combinedMeshes = EditorGUILayout.IntField (combinedMeshes);
        if (GUILayout.Button ("2. Combine Selection"))
        {
            var meshFilters = Selection.gameObjects
                .Where (go => go.activeInHierarchy)
                .Select (go => go.GetComponent<MeshFilter> ())
                .Where (meshFilter => meshFilter != null)
                .ToArray ();

            int sublength = meshFilters.Length / combinedMeshes;

            for (int i = 0; i < combinedMeshes + 1; i++)
            {
                if (i * sublength >= meshFilters.Length)
                {
                    break;
                }

                var meshListSlice = Range (meshFilters, i * sublength, sublength);
                var combinedMesh = BakeMeshesFromFilters (Range(meshFilters, i * sublength, sublength));
                Undo.RegisterCreatedObjectUndo (combinedMesh, "Combined Mesh");
            }
        }
    }

    private IEnumerable<T> Range<T>(T[] t, int startIndex, int length)
    {
        int finalIndexExclusive = System.Math.Min (startIndex + length, t.Length);
        for (int i = startIndex; i < finalIndexExclusive; i++)
        {
            yield return t [i];
        }
    }

    private static GameObject BakeMeshesFromFilters(IEnumerable<MeshFilter> meshFilters)
    {
        var bakedMesh = new GameObject ("[Combined Mesh]");
        Mesh combinedMesh = bakedMesh.AddComponent<MeshFilter> ().mesh = new Mesh ();
        combinedMesh.CombineMeshes (meshFilters.SelectMany (GetMeshCombinesFromFilter).ToArray ());
        MeshRenderer combinedMeshRenderer = bakedMesh.AddComponent<MeshRenderer> ();
        return bakedMesh;
    }

    private static IEnumerable<CombineInstance> GetMeshCombinesFromFilter(MeshFilter meshFilter)
    {
        if (meshFilter == null)
        {
            yield break;
        }

        Mesh mesh = meshFilter.sharedMesh;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            yield return new CombineInstance()
            {
                mesh = mesh,
                transform = meshFilter.transform.localToWorldMatrix,
                subMeshIndex = i
            };
        }
    }
}
