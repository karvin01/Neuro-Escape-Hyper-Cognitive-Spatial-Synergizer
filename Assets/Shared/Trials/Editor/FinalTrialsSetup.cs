using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeuroEscape.EditorTools
{
    /// Menu: Neuro Escape > Set Up Final Trials
    /// Wires the forest pillars, portal, libraries, books, code doors and the wand in Karel's Main_map,
    /// the castle intro + bonfire exit in Map1_Alex, and the Build Profiles scene list.
    /// Safe to run again: it updates what is already there instead of adding it twice,
    /// and Travel Points you moved are left where you put them.
    public static class FinalTrialsSetup
    {
        const string MainMapPath = "Assets/Karel/Scenes/Main_map.unity";
        const string CastlePath = "Assets/Alex/Scenes/Map1_Alex.unity";
        const string Content = "Assets/Shared/Trials/Content/";
        const string DoorPrefab = "Assets/Room/prefabs/door.prefab";
        const string BookPrefab = "Assets/Room/prefabs/book.prefab";

        // sounds (by GUID, so moving files does not break this)
        const string SndClunk = "53e2c00fe5ba44c7a416831064114b3f";    // Alex_SFX_Gate_Locked
        const string SndPlace = "de7dd359c4ba4a908d59c84104da6cec";    // Alex_SFX_Torch_Place
        const string SndSolved = "12f6eed5b1b147049161503a0a2e882e";   // Alex_SFX_Puzzle_Solved
        const string SndPoof = "0f514e9e0b3a4457a9b234f4ce75405e";     // Wizard_Poof
        const string SndPaper = "a0808b5e03c64bd8b458dd5985775caa";    // SFX_Pickup_Paper_1

        static StringBuilder log;
        static int problems;

        [MenuItem("Neuro Escape/Set Up Final Trials")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Final Trials", "Stop Play mode first.", "OK"); return; }
            if (!EditorUtility.DisplayDialog("Final Trials",
                    "This opens Main_map and Map1_Alex, adds the trial scripts and saves both scenes.\n\nUnsaved changes in the open scene will be asked about first.",
                    "Set up", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            log = new StringBuilder();
            problems = 0;

            Scene main = EditorSceneManager.OpenScene(MainMapPath, OpenSceneMode.Single);
            Line("<b>Main_map</b>");
            SetUpMainMap();
            EditorSceneManager.MarkSceneDirty(main);
            EditorSceneManager.SaveScene(main);

            Scene castle = EditorSceneManager.OpenScene(CastlePath, OpenSceneMode.Single);
            Line("<b>Map1_Alex</b>");
            SetUpCastle();
            EditorSceneManager.MarkSceneDirty(castle);
            EditorSceneManager.SaveScene(castle);

            Line("<b>Build Profiles</b>");
            SetUpBuildList();

            EditorSceneManager.OpenScene(MainMapPath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();

            Debug.Log("[Final Trials] Setup finished with " + problems + " problem(s):\n" + log);
            EditorUtility.DisplayDialog("Final Trials",
                problems == 0 ? "Done. Everything was found and set up.\nDetails are in the Console."
                              : "Done, but " + problems + " thing(s) were not found.\nSee the Console (lines marked MISSING).", "OK");
        }

        // ------------------------------------------------------------------ Main_map
        static void SetUpMainMap()
        {
            // ---- travel points (Karel's numbers are the PlayerCapsule's values inside PlayerRig) ----
            GameObject rig = Find("PlayerRig");
            Transform rigT = rig != null ? rig.transform : null;
            if (rigT == null) Missing("PlayerRig (travel points are placed at world positions without the rig offset)");
            Transform pointsRoot = Root("Trials_TravelPoints").transform;

            GameObject tree = Find("OakBigTree01_pr");
            TravelPoint start = Point(pointsRoot, rigT, "Start", new Vector3(90f, 0.6f, 88f), 0f, 0f);
            if (createdLast)
            {
                if (tree != null) FaceTowards(start.transform, tree.transform.position);
                SnapAboveTerrain(start.transform);   // outdoors: never start under the ground
            }
            start.startOfScene = true;
            start.arrivalDialogue = Load<DialogueData>("Trials_Dialogue_Intro.asset");

            TravelPoint lib1 = Point(pointsRoot, rigT, "Library1", new Vector3(-82f, 0.6f, 41f), 180f, 0f);
            lib1.arrivalDialogue = Load<DialogueData>("Trials_Dialogue_Library.asset");
            Point(pointsRoot, rigT, "Library2", new Vector3(-92f, 0.6f, 41f), 180f, 0f);
            Point(pointsRoot, rigT, "Library3", new Vector3(-102f, 0.6f, 41f), 180f, 0f);
            TravelPoint wandRoom = Point(pointsRoot, rigT, "WandRoom", new Vector3(-48f, 0.61f, -60f), -90f, -15f);
            wandRoom.arrivalDialogue = Load<DialogueData>("Trials_Dialogue_Final.asset");
            wandRoom.clearHotbarOnArrival = true;
            foreach (TravelPoint p in pointsRoot.GetComponentsInChildren<TravelPoint>()) EditorUtility.SetDirty(p);

            // ---- portal ----
            PortalTeleport portal = null;
            GameObject portalGO = Find("Portal");
            if (portalGO == null) Missing("Portal");
            else
            {
                portal = GetOrAdd<PortalTeleport>(portalGO);
                portal.travelPoint = "Library1";
                portal.otherScene = "";
                portal.activateSound = Sound(SndSolved);
                portal.enterSound = Sound(SndPoof);
                MakePortalGlow(portalGO, portal);
                Done(portal, "Portal: Portal Teleport -> Library1, purple glow added");
            }

            // ---- pillars ----
            var pillars = new List<RotatingPillar>();
            RotatingPillar.Element[] elements = { RotatingPillar.Element.Fire, RotatingPillar.Element.Water, RotatingPillar.Element.Earth };
            for (int i = 1; i <= 3; i++)
            {
                GameObject pillarGO = Find("Stone_Pillar_Sqr_b_Elements_Color" + i);
                GameObject pedestal = Find("pedestal" + i);
                if (pillarGO == null) { Missing("Stone_Pillar_Sqr_b_Elements_Color" + i); continue; }
                if (pedestal == null) Missing("pedestal" + i);
                RotatingPillar rp = GetOrAdd<RotatingPillar>(pillarGO);
                rp.element = elements[i - 1];
                rp.target = pedestal != null ? pedestal.transform : null;
                Transform lights = pedestal != null ? FindChild(pedestal.transform, "PortalRisingLights") : null;
                if (pedestal != null && lights == null) Missing("PortalRisingLights under pedestal" + i);
                rp.turnOnWhenRight = lights != null ? lights.gameObject : null;
                rp.randomStart = true;
                rp.turnSound = Sound(SndClunk);
                rp.rightSound = Sound(SndPlace);
                pillars.Add(rp);
                Done(rp, pillarGO.name + ": Rotating Pillar (" + rp.element + " -> pedestal" + i + ")");
            }

            PillarPuzzle puzzle = GetOrAdd<PillarPuzzle>(Root("Trials_PillarPuzzle"));
            puzzle.pillars = pillars.ToArray();
            puzzle.portal = portal;
            puzzle.solvedSound = Sound(SndSolved);
            Done(puzzle, "Trials_PillarPuzzle: Pillar Puzzle (" + pillars.Count + " pillars)");

            // ---- libraries ----
            SetUpLibrary(1, "4, 9, 7, 12, 10, 15, [ ? ]", "13", "Library2", null,
                new Dictionary<string, string> { { "GREEN", "+5   -2" } }, null);
            SetUpLibrary(2, "4, 6, 9, 13, 18, 24, [ ? ]", "31", "Library3", null,
                new Dictionary<string, string> { { "GREEN", "+2   +3   +4   +5" } }, null);
            SetUpLibrary(3, "2, 3, 5, 8, 13, 21, [ ? ]", "34", "", "Map1_Alex",
                new Dictionary<string, string>(), "2 + 3 = 5\n\n3 + 5 = 8\n\n5 + 8 = 13");

            // ---- wand ----
            GameObject wand = Find("wand");
            if (wand == null) Missing("wand");
            else
            {
                if (wand.GetComponent<Pickup>() != null && wand.GetComponent<ClaimToWin>() == null)
                    Undo.DestroyObjectImmediate(wand.GetComponent<Pickup>());
                ClaimToWin claim = GetOrAdd<ClaimToWin>(wand);
                claim.item = Load<ItemData>("Trials_Item_Wand.asset");
                GetOrAdd<Spinner>(wand);
                if (wand.GetComponentInChildren<Collider>() == null) AddBox(wand, 1.6f, 0.3f);
                Done(claim, "wand: Claim To Win (Legendary Wand) + Spinner");
            }
        }

        static void SetUpLibrary(int n, string sequence, string answer, string nextPoint, string nextScene,
                                 Dictionary<string, string> bookText, string tableBookText)
        {
            GameObject lib = Find("library" + n);
            if (lib == null) { Missing("library" + n); return; }

            // door
            GameObject door = FindPrefabChild(lib.transform, DoorPrefab, "door");
            if (door == null) Missing("door in library" + n);
            else
            {
                CodeDoor cd = GetOrAdd<CodeDoor>(door);
                cd.sequence = sequence;
                cd.answer = answer;
                cd.travelPoint = nextPoint;
                cd.otherScene = nextScene ?? "";
                cd.rightSound = Sound(SndSolved);
                cd.wrongSound = Sound(SndClunk);
                Done(cd, "library" + n + " door: Code Door (" + answer + " -> " + (string.IsNullOrEmpty(nextScene) ? nextPoint : nextScene) + ")");
            }

            // coloured books
            foreach (string colour in new[] { "RED", "GREEN", "BLUE" })
            {
                foreach (Transform t in lib.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith(colour + "_Book")) continue;
                    bookText.TryGetValue(colour, out string text);
                    MakeBook(t.gameObject, Title(colour) + " Book", text, "library" + n);
                }
            }

            // the book on the table (only library 3 can be read)
            if (tableBookText != null)
            {
                GameObject tableBook = FindPrefabChild(lib.transform, BookPrefab, "book");
                if (tableBook == null) Missing("book on the table in library" + n);
                else MakeBook(tableBook, "Old Book", tableBookText, "library" + n);
            }
        }

        static void MakeBook(GameObject go, string title, string text, string where)
        {
            Inspectable ins = GetOrAdd<Inspectable>(go);
            ins.title = title;
            ins.look = Inspectable.Look.OpenBook;
            ins.book = null;
            ins.item = null;
            ins.pages = string.IsNullOrEmpty(text) ? new[] { "", "" } : new[] { text, "" };
            ins.openSound = Sound(SndPaper);
            Done(ins, where + " " + go.name + ": Inspectable (" + (string.IsNullOrEmpty(text) ? "blank" : text.Replace("\n", " ")) + ")");
        }

        static void MakePortalGlow(GameObject portalGO, PortalTeleport portal)
        {
            Transform glowT = FindChild(portalGO.transform, "Trials_PortalGlow");
            if (glowT == null)
            {
                // measure the portal (without any old glow)
                Renderer[] rends = portalGO.GetComponentsInChildren<Renderer>();
                Bounds b = rends.Length > 0 ? rends[0].bounds : new Bounds(portalGO.transform.position + Vector3.up * 1.5f, new Vector3(2f, 3f, 0.3f));
                foreach (Renderer r in rends) b.Encapsulate(r.bounds);
                bool thinInX = b.size.x < b.size.z;
                Vector3 normal = thinInX ? Vector3.right : Vector3.forward;
                float width = (thinInX ? b.size.z : b.size.x) * 0.62f;
                float height = b.size.y * 0.72f;

                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Undo.RegisterCreatedObjectUndo(quad, "Portal glow");
                quad.name = "Trials_PortalGlow";
                quad.transform.SetPositionAndRotation(b.center - Vector3.up * (b.size.y * 0.04f), Quaternion.LookRotation(normal, Vector3.up));
                quad.transform.localScale = new Vector3(width, height, 1f);
                quad.transform.SetParent(portalGO.transform, true);
                Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
                var box = quad.AddComponent<BoxCollider>();
                box.size = new Vector3(1f, 1f, 0.2f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = GlowMaterial();
                quad.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var lightGO = new GameObject("Trials_PortalLight");
                lightGO.transform.SetParent(quad.transform, false);
                lightGO.transform.localPosition = new Vector3(0f, 0f, -0.3f);
                Light l = lightGO.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.7f, 0.35f, 1f);
                l.range = 7f;
                l.intensity = 0f;
                glowT = quad.transform;
                Line("   portal glow: " + width.ToString("0.0") + " x " + height.ToString("0.0") + " m (scale the Trials_PortalGlow object if it does not fit the frame)");
            }
            portal.glow = glowT.gameObject;
            portal.glowLight = glowT.GetComponentInChildren<Light>(true);
        }

        static Material GlowMaterial()
        {
            string path = Content + "Trials_PortalGlow.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            m = new Material(sh) { name = "Trials_PortalGlow" };
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Content + "Trials_PortalGlow.png"));
            m.SetColor("_BaseColor", new Color(0.75f, 0.3f, 1f, 1f) * 1.8f);
            // transparent + additive, seen from both sides
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_Cull", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ------------------------------------------------------------------ Map1_Alex
        static void SetUpCastle()
        {
            BonfireWizard wizard = Object.FindFirstObjectByType<BonfireWizard>(FindObjectsInactive.Include);
            if (wizard == null) Missing("BonfireWizard in Map1_Alex");
            else
            {
                Undo.RecordObject(wizard, "Bonfire exit");
                wizard.nextScene = "Main_map";
                wizard.nextTravelPoint = "WandRoom";
                Done(wizard, "Bonfire: after the right answers -> Main_map / WandRoom");
            }

            GameObject introGO = Root("Trials_CastleIntro");
            DialogueTrigger intro = GetOrAdd<DialogueTrigger>(introGO);
            intro.startWhen = DialogueTrigger.StartMode.AtStart;
            intro.onlyOnce = true;
            intro.dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/Alex/Dialogue/Wiz_CastleStart.asset");
            if (intro.dialogue == null) Missing("Assets/Alex/Dialogue/Wiz_CastleStart.asset");
            Done(intro, "Trials_CastleIntro: Dialogue Trigger (At Start, Wiz_CastleStart)");
        }

        // ------------------------------------------------------------------ Build Profiles
        static void SetUpBuildList()
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == MainMapPath || s.path == CastlePath);
            list.Insert(0, new EditorBuildSettingsScene(CastlePath, true));
            list.Insert(0, new EditorBuildSettingsScene(MainMapPath, true));
            EditorBuildSettings.scenes = list.ToArray();
            Line("   Scene List: 0 = Main_map, 1 = Map1_Alex" + (list.Count > 2 ? " (+" + (list.Count - 2) + " other)" : ""));
        }

        // ------------------------------------------------------------------ helpers
        static bool createdLast;

        static TravelPoint Point(Transform root, Transform rig, string id, Vector3 capsuleLocal, float yaw, float pitch)
        {
            Transform t = FindChild(root, "TP_" + id);
            createdLast = t == null;
            if (t == null)
            {
                var go = new GameObject("TP_" + id);
                Undo.RegisterCreatedObjectUndo(go, "Travel point");
                go.transform.SetParent(root, false);
                Vector3 world = rig != null ? rig.TransformPoint(capsuleLocal) : capsuleLocal;
                float worldYaw = (rig != null ? rig.eulerAngles.y : 0f) + yaw;
                go.transform.SetPositionAndRotation(world, Quaternion.Euler(0f, worldYaw, 0f));
                t = go.transform;
            }
            TravelPoint p = GetOrAdd<TravelPoint>(t.gameObject);
            p.id = id;
            p.lookPitch = pitch;
            Line("   TP_" + id + (createdLast ? " created at " : " kept at ") + t.position.ToString("0.0"));
            return p;
        }

        static void SnapAboveTerrain(Transform t)
        {
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain == null) return;
            float ground = terrain.transform.position.y + terrain.SampleHeight(t.position);
            if (ground > t.position.y - 0.05f)
            {
                t.position = new Vector3(t.position.x, ground + 0.1f, t.position.z);
                Line("   TP_Start raised to the ground height (" + (ground + 0.1f).ToString("0.00") + ")");
            }
        }

        static void FaceTowards(Transform t, Vector3 target)
        {
            Vector3 d = target - t.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) t.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(go);
            else Undo.RecordObject(c, "Final trials");
            return c;
        }

        static void AddBox(GameObject go, float grow, float minWorld)
        {
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            var box = Undo.AddComponent<BoxCollider>(go);
            if (rends.Length == 0) return;
            Bounds b = rends[0].bounds;
            foreach (Renderer r in rends) b.Encapsulate(r.bounds);
            Vector3 size = Vector3.Max(b.size * grow, Vector3.one * minWorld);
            Vector3 s = go.transform.lossyScale;
            box.center = go.transform.InverseTransformPoint(b.center);
            box.size = new Vector3(size.x / Mathf.Max(0.0001f, Mathf.Abs(s.x)), size.y / Mathf.Max(0.0001f, Mathf.Abs(s.y)), size.z / Mathf.Max(0.0001f, Mathf.Abs(s.z)));
        }

        static GameObject Root(string name)
        {
            GameObject go = Find(name);
            if (go != null) return go;
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
            return go;
        }

        static GameObject Find(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            return null;
        }

        static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
                if (t != parent && t.name == name) return t;
            return null;
        }

        // a prefab instance (of the given prefab file) under 'parent', or a child with that name
        static GameObject FindPrefabChild(Transform parent, string prefabPath, string fallbackName)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                if (PrefabUtility.GetNearestPrefabInstanceRoot(go) == go &&
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == prefabPath) return go;
            }
            Transform byName = FindChild(parent, fallbackName);
            return byName != null ? byName.gameObject : null;
        }

        static T Load<T>(string file) where T : Object
        {
            T a = AssetDatabase.LoadAssetAtPath<T>(Content + file);
            if (a == null) Missing(Content + file);
            return a;
        }

        static AudioClip Sound(string guid) => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));

        static string Title(string colour) => colour.Substring(0, 1) + colour.Substring(1).ToLower();

        static void Done(Object o, string what)
        {
            EditorUtility.SetDirty(o);
            if (o is Component c && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            Line("   OK  " + what);
        }

        static void Missing(string what) { problems++; Line("   MISSING  " + what); }
        static void Line(string s) => log.AppendLine(s);
    }
}
