#if UNITY_EDITOR && KUCHARZE_ONLINE
using System.Collections.Generic;
using System.Linq;
using ModularMovement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Fits and colors the existing animated model without replacing its rig or movement components.
public static class LobbyPlayerVisualSetup
{
    private const string MaterialRoot = ProjectAssetPaths.CharacterMaterials;
    private const string MeshRoot = ProjectAssetPaths.CharacterMeshes;

    public static void Apply()
    {
        ProjectAssetPaths.EnsureFolder(MaterialRoot);
        ProjectAssetPaths.EnsureFolder(MeshRoot);
        string path = ProjectAssetPaths.LobbyPlayer;
        GameObject player = PrefabUtility.LoadPrefabContents(path);
        try
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Development/Networking/OnlinePlayer.prefab");
            Transform model = player.GetComponent<PlayerRotation>().ResolvedModel;
            Transform sourceModel = source.GetComponent<PlayerRotation>().ResolvedModel;
            Transform wrapper = player.transform.Find("PlayerVisualScale");
            if (wrapper == null)
            {
                wrapper = new GameObject("PlayerVisualScale").transform;
                wrapper.SetParent(player.transform, false);
            }
            wrapper.localPosition = sourceModel.localPosition;
            wrapper.localRotation = Quaternion.identity;
            wrapper.localScale = new Vector3(1.12f, 0.95f, 1.12f);
            model.SetParent(wrapper, false);
            model.localPosition = Vector3.zero;
            model.localRotation = sourceModel.localRotation;
            model.localScale = sourceModel.localScale;
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            Mesh original = sourceModel.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            renderer.sharedMesh = SplitCharacter(original, renderer.transform, model);
            renderer.sharedMaterials = new[]
            {
                MakeMaterial("Coat", new Color(0.23f, 0.57f, 0.77f)),
                MakeMaterial("HatAndGloves", new Color(0.94f, 0.91f, 0.80f)),
                MakeMaterial("Face", new Color(0.88f, 0.69f, 0.45f)),
                MakeMaterial("Boots", new Color(0.22f, 0.24f, 0.25f))
            };
            Transform ring = player.transform.Find("PlayerGroundRing");
            if (ring == null)
            {
                ring = new GameObject("PlayerGroundRing", typeof(MeshFilter), typeof(MeshRenderer)).transform;
                ring.SetParent(player.transform, false);
            }
            ring.localPosition = new Vector3(0, -0.90f, 0);
            ring.GetComponent<MeshFilter>().sharedMesh = RingMesh();
            MeshRenderer ringRenderer = ring.GetComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = MakeMaterial("PlayerRing", new Color(0.23f, 0.57f, 0.77f), true);
            ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            var appearance = player.GetComponent<PlayerNetworkAppearance>() ?? player.AddComponent<PlayerNetworkAppearance>();
            var serialized = new SerializedObject(appearance);
            SerializedProperty renderers = serialized.FindProperty("tintRenderers");
            renderers.arraySize = 2;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            renderers.GetArrayElementAtIndex(1).objectReferenceValue = ringRenderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (renderer.sharedMesh.vertexCount != original.vertexCount || renderer.sharedMesh.triangles.Length != original.triangles.Length
                || renderer.sharedMesh.bindposes.Length != original.bindposes.Length)
                throw new System.InvalidOperationException("The styled mesh must preserve its original geometry and rig.");
            Animator animator = model.GetComponent<Animator>();
            Animator sourceAnimator = sourceModel.GetComponent<Animator>();
            if (animator.runtimeAnimatorController != sourceAnimator.runtimeAnimatorController || animator.applyRootMotion)
                throw new System.InvalidOperationException("The original animation controller must stay connected without root motion.");
            foreach (AnimationClip clip in sourceAnimator.runtimeAnimatorController.animationClips)
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                    if (binding.path.Length > 0 && sourceModel.Find(binding.path) != null && model.Find(binding.path) == null)
                        throw new System.InvalidOperationException("Animation path was lost: " + binding.path);
            PrefabUtility.SaveAsPrefabAsset(player, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
    }

    private static Material MakeMaterial(string name, Color color, bool unlit = false)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Simple Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0);
        if (material.HasProperty("_SpecColor")) material.SetColor("_SpecColor", Color.black);
        if (!unlit && (name == "Coat" || name == "Face"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * (name == "Coat" ? 0.20f : 0.07f));
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh SplitCharacter(Mesh original, Transform renderer, Transform model)
    {
        Vector3[] vertices = original.vertices;
        int[] triangles = original.triangles;
        int[] parents = Enumerable.Range(0, vertices.Length).ToArray();
        var welded = new Dictionary<Vector3Int, int>();
        var local = new Vector3[vertices.Length];
        var bounds = new Bounds();
        for (int index = 0; index < vertices.Length; index++)
        {
            Vector3 point = vertices[index];
            var key = Vector3Int.RoundToInt(point * 10000);
            if (welded.TryGetValue(key, out int previous)) parents[Find(parents, index)] = Find(parents, previous);
            else welded.Add(key, index);
            local[index] = model.InverseTransformPoint(renderer.TransformPoint(point));
            if (index == 0) bounds = new Bounds(local[index], Vector3.zero);
            else bounds.Encapsulate(local[index]);
        }
        for (int index = 0; index < triangles.Length; index += 3)
        {
            int root = Find(parents, triangles[index]);
            parents[Find(parents, triangles[index + 1])] = root;
            parents[Find(parents, triangles[index + 2])] = root;
        }
        var pieces = new Dictionary<int, Bounds>();
        for (int index = 0; index < vertices.Length; index++)
        {
            int root = Find(parents, index);
            if (!pieces.TryGetValue(root, out Bounds piece)) piece = new Bounds(local[index], Vector3.zero);
            piece.Encapsulate(local[index]);
            pieces[root] = piece;
        }
        var groups = new[] { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
        for (int index = 0; index < triangles.Length; index += 3)
        {
            Bounds piece = pieces[Find(parents, triangles[index])];
            float height = (piece.center.y - bounds.min.y) / Mathf.Max(0.001f, bounds.size.y);
            int group = height > 0.70f ? 1 : height > 0.52f ? 2 : height < 0.16f ? 3 :
                Mathf.Abs(piece.center.x - bounds.center.x) > bounds.size.x * 0.22f ? 1 : 0;
            groups[group].Add(triangles[index]);
            groups[group].Add(triangles[index + 1]);
            groups[group].Add(triangles[index + 2]);
        }
        Mesh mesh = Object.Instantiate(original);
        mesh.name = "LobbyCharacter";
        mesh.subMeshCount = 4;
        for (int index = 0; index < groups.Length; index++) mesh.SetTriangles(groups[index], index, true);
        mesh.bounds = original.bounds;
        return SaveMesh(mesh, MeshRoot + "/LobbyCharacter.asset");
    }

    private static int Find(int[] parents, int index)
    {
        while (parents[index] != index) { parents[index] = parents[parents[index]]; index = parents[index]; }
        return index;
    }

    private static Mesh RingMesh()
    {
        const int count = 48;
        var vertices = new Vector3[count * 2];
        var triangles = new int[count * 6];
        for (int index = 0; index < count; index++)
        {
            float angle = index * Mathf.PI * 2 / count;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            vertices[index * 2] = direction * 0.70f;
            vertices[index * 2 + 1] = direction * 0.60f;
            int next = (index + 1) % count * 2;
            int offset = index * 6;
            triangles[offset] = index * 2;
            triangles[offset + 1] = index * 2 + 1;
            triangles[offset + 2] = next;
            triangles[offset + 3] = next;
            triangles[offset + 4] = index * 2 + 1;
            triangles[offset + 5] = next + 1;
        }
        var mesh = new Mesh { name = "PlayerRing", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return SaveMesh(mesh, MeshRoot + "/PlayerRing.asset");
    }

    private static Mesh SaveMesh(Mesh mesh, string path)
    {
        Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, saved);
        Object.DestroyImmediate(mesh);
        EditorUtility.SetDirty(saved);
        return saved;
    }
}
#endif
