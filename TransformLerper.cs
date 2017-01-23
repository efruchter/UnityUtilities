using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TransformLerper : MonoBehaviour
{
    public bool applyInLateUpdate = false;
    public Transform bonesToMove, bonesToTrack;

    [Header("Cache")]
    public Mapping[] mappingCache;

    private void LateUpdate()
    {
        Lerp(1);
    }

    public void Lerp(float lerp)
    {
        if (mappingCache == null)
        {
            return;
        }

        foreach (var mapping in mappingCache)
        {
            mapping.source.position = Vector3.Lerp(mapping.source.position, mapping.dest.position, lerp);
            mapping.source.rotation = Quaternion.Slerp(mapping.source.rotation, mapping.dest.rotation, lerp);
        }
    }

    [System.Serializable]
    public struct Mapping
    {
        public Transform source, dest;
    }

#if UNITY_EDITOR
    public void Editor_MapBones()
    {
        Dictionary<string, Transform> sources = new Dictionary<string, Transform>();
        Dictionary<string, Transform> destinations = new Dictionary<string, Transform>();

        foreach (var bone in bonesToMove.GetComponentsInChildren<Transform>(false))
        {
            sources[bone.gameObject.name] = bone;
        }

        foreach (var bone in bonesToTrack.GetComponentsInChildren<Transform>(false))
        {
            destinations[bone.gameObject.name] = bone;
        }

        mappingCache = sources.Keys
            .Where(boneName => destinations.ContainsKey(boneName))
            .Select(boneName => new Mapping() { source = sources[boneName], dest = destinations[boneName] })
            .ToArray();
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(TransformLerper))]
public class TransformSkeletonChaserEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var chaser = target as TransformLerper;
        if (GUILayout.Button("Map Bones"))
        {
            chaser.Editor_MapBones();
        }
    }
}
#endif
