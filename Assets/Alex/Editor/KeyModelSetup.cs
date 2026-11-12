using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeuroEscape.EditorTools
{
    /// Menu: Neuro Escape > Use Key Model For Keys
    /// Turns the Blender key (Alex_Key.fbx) into a clean key mesh (centred, lying flat, 'Key Length' long)
    /// with its texture, and puts it on Key1, Key2 and Key3 in Map1_Alex instead of the grey blocks.
    /// Safe to run again (it updates the same mesh and material files).
    public static class KeyModelSetup
    {
        const string Folder = "Assets/Alex/Models/Key/";
        const string FbxPath = Folder + "Alex_Key.fbx";
        const string TexPath = Folder + "Alex_Key_BaseColor.png";
        const string MeshPath = Folder + "Alex_Key_Mesh.asset";
        const string MatPath = Folder + "Alex_Mat_Key.mat";
        const string CastlePath = "Assets/Alex/Scenes/Map1_Alex.unity";
        static readonly string[] Keys = { "Key1", "Key2", "Key3" };

        /// Length of the key in meters (the old blocks were 0.3 long).
        const float KeyLength = 0.3f;

        [MenuItem("Neuro Escape/Use Key Model For Keys")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Key model", "Stop Play mode first.", "OK"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Mesh source = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<Mesh>().FirstOrDefault();
            if (source == null) { EditorUtility.DisplayDialog("Key model", "No mesh found in " + FbxPath, "OK"); return; }

            Mesh mesh = BuildMesh(source);
            Material mat = BuildMaterial();

            Scene castle = EditorSceneManager.OpenScene(CastlePath, OpenSceneMode.Single);
            var log = new StringBuilder();
            int done = 0;
            foreach (string keyName in Keys)
            {
                GameObject key = Find(keyName);
                if (key == null) { log.AppendLine("MISSING  " + keyName); continue; }

                MeshFilter mf = key.GetComponent<MeshFilter>();
                if (mf == null) mf = Undo.AddComponent<MeshFilter>(key);
                MeshRenderer mr = key.GetComponent<MeshRenderer>();
                if (mr == null) mr = Undo.AddComponent<MeshRenderer>(key);
                Undo.RecordObjects(new Object[] { mf, mr, key.transform }, "Key model");

                mf.sharedMesh = mesh;
                mr.sharedMaterials = Enumerable.Repeat(mat, Mathf.Max(1, mesh.subMeshCount)).ToArray();
                key.transform.localScale = Vector3.one;   // the mesh already has the real size

                BoxCollider box = key.GetComponent<BoxCollider>();
                if (box == null) box = Undo.AddComponent<BoxCollider>(key);
                else Undo.RecordObject(box, "Key model");
                box.center = mesh.bounds.center;
                box.size = Vector3.Max(mesh.bounds.size, new Vector3(0.06f, 0.06f, 0.06f));   // easy to aim at

                EditorUtility.SetDirty(mf); EditorUtility.SetDirty(mr); EditorUtility.SetDirty(box); EditorUtility.SetDirty(key.transform);
                log.AppendLine("OK  " + keyName);
                done++;
            }

            EditorSceneManager.MarkSceneDirty(castle);
            EditorSceneManager.SaveScene(castle);
            AssetDatabase.SaveAssets();
            Debug.Log("[Key model] " + done + " of " + Keys.Length + " keys changed:\n" + log);
            EditorUtility.DisplayDialog("Key model", done + " of " + Keys.Length + " keys now use the key model.\nDetails are in the Console.", "OK");
        }

        // Centres the key, makes its thinnest side point up (lying flat), longest side along X, and scales it.
        static Mesh BuildMesh(Mesh src)
        {
            Bounds b = src.bounds;
            Vector3 size = b.size;
            int[] bySize = new[] { 0, 1, 2 }.OrderBy(i => size[i]).ToArray();
            int thin = bySize[0], mid = bySize[1], lng = bySize[2];
            int[] map = { lng, thin, mid };                 // new X, Y, Z come from these old axes
            float zSign = IsOddPermutation(map) ? -1f : 1f;  // keep it from being mirrored
            float scale = KeyLength / Mathf.Max(0.0001f, size[lng]);

            Vector3 Remap(Vector3 p) => new Vector3(p[lng], p[thin], p[mid] * zSign);

            Vector3[] verts = src.vertices.Select(p => Remap(p - b.center) * scale).ToArray();
            Vector3[] normals = src.normals;
            bool hasNormals = normals != null && normals.Length == verts.Length;

            Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            bool isNew = m == null;
            if (isNew) m = new Mesh();
            m.Clear();
            m.name = "Alex_Key_Mesh";
            m.vertices = verts;
            if (hasNormals) m.normals = normals.Select(n => Remap(n).normalized).ToArray();
            m.uv = src.uv;
            m.subMeshCount = src.subMeshCount;
            for (int i = 0; i < src.subMeshCount; i++) m.SetTriangles(src.GetTriangles(i), i);
            if (!hasNormals) m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();

            if (isNew) AssetDatabase.CreateAsset(m, MeshPath);
            else EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            return m;
        }

        static bool IsOddPermutation(int[] p)
        {
            int inversions = 0;
            for (int i = 0; i < p.Length; i++)
                for (int j = i + 1; j < p.Length; j++)
                    if (p[i] > p[j]) inversions++;
            return inversions % 2 == 1;
        }

        static Material BuildMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat != null) return mat;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(lit != null ? lit : Shader.Find("Standard")) { name = "Alex_Mat_Key" };
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
            if (tex != null) { mat.SetTexture("_BaseMap", tex); mat.mainTexture = tex; }
            mat.SetFloat("_Metallic", 0.6f);
            mat.SetFloat("_Smoothness", 0.45f);
            AssetDatabase.CreateAsset(mat, MatPath);
            return mat;
        }

        static GameObject Find(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            return null;
        }
    }
}
