// Element Pillar builder (URP) — Tools > Element Pillar > Build Prefabs
// Creates URP materials for Stone_Pillar_Sqr_b and four element medallions
// (Water, Fire, Wind, Rock), then saves two prefab variants of the original pillar:
//   Stone_Pillar_Sqr_b_Elements        – medallions carved in the pillar's stone
//   Stone_Pillar_Sqr_b_Elements_Color  – same, with coloured inlays in the symbols
// The original prefab, material and textures are not modified.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ElementPillarBuilder
{
    // GUIDs of the original Occult Stone Pillars textures (from their .meta files)
    const string PillarAlbedoGUID = "51ed9eafa8183ec4cb205432778168e5";
    const string PillarNormalGUID = "a5d5469cda9818845ace017e718ca28d";
    const string PillarMetalGUID  = "adca76438c1abe84a814976e3e7d2b1a";
    const string PillarPrefabName = "Stone_Pillar_Sqr_b";

    // ---- Placement, in metres, pillar local space (taken from the FBX) ----
    const float FaceOffset   = 0.20f;  // shaft is 40 cm square, flat from 0.78 m to 2.44 m
    const float CenterHeight = 2.08f;  // medallion centre height
    const float Diameter     = 0.20f;
    const float Thickness    = 0.020f; // how far it stands out from the shaft
    const float Bevel        = 0.006f;
    const float Embed        = 0.002f; // sunk slightly into the shaft so no gap shows
    const int   Segments     = 64;
    const int   LastLodWithMedallions = 2; // shown in LOD0-LOD2, hidden in LOD3

    // Same tint as the original Standard material (_Color 0.757)
    static readonly Color StoneTint = new Color(0.75686276f, 0.75686276f, 0.75686276f, 1f);

    struct Slot { public string Name; public int Cell; public Vector3 Outward; }
    // Opposites face each other: Water <-> Fire, Wind <-> Rock
    static readonly Slot[] Slots =
    {
        new Slot { Name = "Water", Cell = 0, Outward = Vector3.back    },
        new Slot { Name = "Fire",  Cell = 1, Outward = Vector3.forward },
        new Slot { Name = "Wind",  Cell = 2, Outward = Vector3.right   },
        new Slot { Name = "Rock",  Cell = 3, Outward = Vector3.left    },
    };

    [MenuItem("Tools/Element Pillar/Build Prefabs")]
    public static void Build()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            EditorUtility.DisplayDialog("Element Pillar", "URP Lit shader not found. Is the Universal Render Pipeline package installed?", "OK");
            return;
        }

        string root = FindRoot();
        if (root == null)
        {
            EditorUtility.DisplayDialog("Element Pillar", "Could not find Textures/Medallion_Elements_Albedo.png. Keep the ElementPillar folder structure intact.", "OK");
            return;
        }
        string matDir  = EnsureFolder(root, "Materials");
        string meshDir = EnsureFolder(root, "Meshes");

        // ---- Textures ----
        string tex = root + "/Textures/";
        Setup(tex + "Medallion_Elements_Albedo.png", true, false);
        Setup(tex + "Medallion_Elements_Albedo_ColorInlay.png", true, false);
        Setup(tex + "Medallion_Elements_Normal.png", false, true);
        Setup(tex + "Medallion_Elements_MetallicSmoothness.png", false, false);
        var medAlb    = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "Medallion_Elements_Albedo.png");
        var medAlbCol = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "Medallion_Elements_Albedo_ColorInlay.png");
        var medNrm    = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "Medallion_Elements_Normal.png");
        var medMS     = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "Medallion_Elements_MetallicSmoothness.png");

        var pAlb = LoadByGuid<Texture2D>(PillarAlbedoGUID);
        var pNrm = LoadByGuid<Texture2D>(PillarNormalGUID);
        var pMS  = LoadByGuid<Texture2D>(PillarMetalGUID);
        if (pAlb == null || pNrm == null || pMS == null)
            Debug.LogWarning("[Element Pillar] Some original Stone_Pillar_Sqr_b textures were not found by GUID; the pillar material may be incomplete.");

        // ---- Materials ----
        Material pillarMat = MakeLit(lit, matDir + "/Stone_Pillar_Sqr_b_URP.mat", pAlb, pNrm, pMS, Vector2.one, Vector2.zero);
        var carved = new Material[Slots.Length];
        var color  = new Material[Slots.Length];
        for (int i = 0; i < Slots.Length; i++)
        {
            Vector2 scale  = new Vector2(0.5f, 0.5f);
            Vector2 offset = new Vector2((Slots[i].Cell % 2) * 0.5f, (Slots[i].Cell / 2) * 0.5f);
            carved[i] = MakeLit(lit, matDir + "/Medallion_" + Slots[i].Name + ".mat", medAlb, medNrm, medMS, scale, offset);
            color[i]  = MakeLit(lit, matDir + "/Medallion_" + Slots[i].Name + "_ColorInlay.mat", medAlbCol, medNrm, medMS, scale, offset);
        }

        // ---- Mesh ----
        Mesh mesh = SaveMesh(BuildMedallionMesh(), meshDir + "/Medallion.asset");

        // ---- Prefab variants ----
        GameObject src = FindPillarPrefab();
        if (src == null)
        {
            EditorUtility.DisplayDialog("Element Pillar", "Could not find " + PillarPrefabName + ".prefab in the project.", "OK");
            return;
        }
        string a = SaveVariant(src, root + "/Stone_Pillar_Sqr_b_Elements.prefab", pillarMat, carved, mesh);
        SaveVariant(src, root + "/Stone_Pillar_Sqr_b_Elements_Color.prefab", pillarMat, color, mesh);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        var made = AssetDatabase.LoadAssetAtPath<GameObject>(a);
        Selection.activeObject = made;
        EditorGUIUtility.PingObject(made);
        Debug.Log("[Element Pillar] Built prefabs in " + root);
    }

    // ------------------------------------------------------------------
    static string SaveVariant(GameObject src, string path, Material pillarMat, Material[] mats, Mesh mesh)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
        try
        {
            inst.name = Path.GetFileNameWithoutExtension(path);
            foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>(true))
                mr.sharedMaterial = pillarMat;

            var group = new GameObject("Element_Medallions").transform;
            group.SetParent(inst.transform, false);
            var renderers = new List<Renderer>();
            for (int i = 0; i < Slots.Length; i++)
            {
                var go = new GameObject("Medallion_" + Slots[i].Name);
                go.transform.SetParent(group, false);
                go.transform.localPosition = Slots[i].Outward * (FaceOffset - Embed) + Vector3.up * CenterHeight;
                // local +Z points into the pillar, so the front (local -Z) faces outward, unmirrored
                go.transform.localRotation = Quaternion.LookRotation(-Slots[i].Outward, Vector3.up);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = mats[i];
                renderers.Add(r);
            }

            var lodGroup = inst.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                LOD[] lods = lodGroup.GetLODs();
                for (int l = 0; l < lods.Length && l <= LastLodWithMedallions; l++)
                {
                    var list = new List<Renderer>(lods[l].renderers);
                    list.AddRange(renderers);
                    lods[l].renderers = list.ToArray();
                }
                lodGroup.SetLODs(lods); // original LOD distances are kept
            }
            PrefabUtility.SaveAsPrefabAsset(inst, path);
        }
        finally
        {
            Object.DestroyImmediate(inst);
        }
        return path;
    }

    static Material MakeLit(Shader lit, string path, Texture2D baseMap, Texture2D normal, Texture2D ms, Vector2 scale, Vector2 offset)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, path); }
        else mat.shader = lit;

        mat.SetFloat("_WorkflowMode", 1f); // metallic
        mat.SetTexture("_BaseMap", baseMap);
        mat.SetTextureScale("_BaseMap", scale);
        mat.SetTextureOffset("_BaseMap", offset);
        mat.SetColor("_BaseColor", StoneTint);

        mat.SetTexture("_BumpMap", normal);
        mat.SetFloat("_BumpScale", 1f);
        if (normal != null) mat.EnableKeyword("_NORMALMAP"); else mat.DisableKeyword("_NORMALMAP");

        mat.SetTexture("_MetallicGlossMap", ms);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 1f);              // scales the smoothness stored in alpha
        mat.SetFloat("_SmoothnessTextureChannel", 0f); // smoothness from metallic map alpha
        if (ms != null) mat.EnableKeyword("_METALLICSPECGLOSSMAP"); else mat.DisableKeyword("_METALLICSPECGLOSSMAP");

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Disc: flat front cap, 45° bevel, straight side wall. Front faces local -Z.
    static Mesh BuildMedallionMesh()
    {
        float R = Diameter * 0.5f, Rf = R - Bevel;
        float zFront = -Thickness, zBevel = -(Thickness - Bevel), zBack = 0f;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();

        // front cap
        int c = v.Count;
        v.Add(new Vector3(0, 0, zFront)); n.Add(Vector3.back); uv.Add(new Vector2(0.5f, 0.5f));
        for (int i = 0; i <= Segments; i++)
        {
            Vector2 d = Dir(i);
            v.Add(new Vector3(d.x * Rf, d.y * Rf, zFront)); n.Add(Vector3.back); uv.Add(UV(d, Rf / R));
        }
        for (int i = 0; i < Segments; i++) Tri(t, v, c, c + 1 + i, c + 2 + i, Vector3.back);

        // bevel and side wall: rings of (inner, outer) vertex pairs
        Ring(v, n, uv, t, Rf, zFront, Rf / R, R, zBevel, 1f, true);
        Ring(v, n, uv, t, R, zBevel, 1f, R, zBack, 0.97f, false);

        var m = new Mesh { name = "Medallion" };
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateBounds();
        m.RecalculateTangents();
        return m;
    }

    static void Ring(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t,
                     float r0, float z0, float uvR0, float r1, float z1, float uvR1, bool bevel)
    {
        int s = v.Count;
        for (int i = 0; i <= Segments; i++)
        {
            Vector2 d = Dir(i);
            Vector3 radial = new Vector3(d.x, d.y, 0f);
            Vector3 nrm = bevel ? (radial + Vector3.back).normalized : radial;
            v.Add(new Vector3(d.x * r0, d.y * r0, z0)); n.Add(nrm); uv.Add(UV(d, uvR0));
            v.Add(new Vector3(d.x * r1, d.y * r1, z1)); n.Add(nrm); uv.Add(UV(d, uvR1));
        }
        for (int i = 0; i < Segments; i++)
        {
            int a = s + i * 2, b = a + 1, c = a + 2, d2 = a + 3;
            Vector3 want = (n[a] + n[c]).normalized;
            Tri(t, v, a, b, c, want);
            Tri(t, v, c, b, d2, want);
        }
    }

    static Vector2 Dir(int i) { float a = i * 2f * Mathf.PI / Segments; return new Vector2(Mathf.Cos(a), Mathf.Sin(a)); }
    static Vector2 UV(Vector2 dir, float r) { return new Vector2(0.5f + 0.5f * r * dir.x, 0.5f + 0.5f * r * dir.y); }

    // Adds a triangle wound so that its face normal points along 'want'
    static void Tri(List<int> t, List<Vector3> v, int a, int b, int c, Vector3 want)
    {
        Vector3 cr = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (Vector3.Dot(cr, want) < 0f) { t.Add(a); t.Add(c); t.Add(b); }
        else { t.Add(a); t.Add(b); t.Add(c); }
    }

    static Mesh SaveMesh(Mesh fresh, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(fresh, path); return fresh; }
        existing.Clear();
        EditorUtility.CopySerialized(fresh, existing);
        Object.DestroyImmediate(fresh);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    static void Setup(string path, bool srgb, bool normal)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        ti.sRGBTexture = srgb;
        ti.alphaSource = TextureImporterAlphaSource.FromInput;
        ti.alphaIsTransparency = false;
        ti.wrapMode = TextureWrapMode.Clamp; // atlas: keep cells from bleeding into each other
        ti.mipmapEnabled = true;
        ti.maxTextureSize = 2048;
        ti.SaveAndReimport();
    }

    static string FindRoot()
    {
        foreach (string g in AssetDatabase.FindAssets("Medallion_Elements_Albedo t:Texture2D"))
        {
            string p = AssetDatabase.GUIDToAssetPath(g).Replace('\\', '/');
            if (p.EndsWith("/Textures/Medallion_Elements_Albedo.png"))
                return p.Substring(0, p.Length - "/Textures/Medallion_Elements_Albedo.png".Length);
        }
        return null;
    }

    static GameObject FindPillarPrefab()
    {
        foreach (string g in AssetDatabase.FindAssets(PillarPrefabName + " t:Prefab"))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileName(p) == PillarPrefabName + ".prefab")
                return AssetDatabase.LoadAssetAtPath<GameObject>(p);
        }
        return null;
    }

    static T LoadByGuid<T>(string guid) where T : Object
    {
        string p = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(p) ? null : AssetDatabase.LoadAssetAtPath<T>(p);
    }

    static string EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        return path;
    }
}
