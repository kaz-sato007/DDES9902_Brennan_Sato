using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LandNav.EditorTools
{
    /// <summary>
    /// Builds the whole training scene from the data in Assets/LandNav/Data:
    /// terrain (heights, textures, trees, grass), feature zones, flags, player rig, map and signage.
    /// Menu: Land Nav > Build Training Scene. Safe to run again; it regenerates everything.
    /// </summary>
    public static class LandNavSceneBuilder
    {
        const string Root = "Assets/__My Project/Assessment 1";
        const string Data = Root + "/Data";
        const string Tex = Root + "/Textures";
        const string Gen = Root + "/Generated";
        const string ScenePath = Root + "/Scenes/TerrainID_Training.unity";
        const string FlatPlayerPrefab = "Assets/EZPZ Interaction Toolkit/Prefabs/Flat Screen Specific/EZPZ Player Flat Screen WASD.prefab";
        const string XRPlayerPrefab = "Assets/EZPZ Interaction Toolkit/Prefabs/XR Utilities/EZPZ Player XR - Dual Remote - No Teleport - No Jump.prefab";

        [Serializable] class StartJson { public float x, y, z; }
        [Serializable] class FeatureJson { public string id, name, flag, teach; public float x, y, z, r; }
        [Serializable] class MetaJson
        {
            public float size, maxHeight;
            public int res, splatRes;
            public StartJson start;
            public FeatureJson[] features;
        }

        [MenuItem("Land Nav/Build Training Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                EditorUtility.DisplayProgressBar("Land Nav", "Reading data", 0.05f);
                Directory.CreateDirectory(Gen);
                Directory.CreateDirectory(Root + "/Scenes");
                var meta = JsonUtility.FromJson<MetaJson>(File.ReadAllText(Data + "/landnav_meta.json"));

                ConfigureTextures();

                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                foreach (var go in scene.GetRootGameObjects())
                    if (go.GetComponent<Camera>()) UnityEngine.Object.DestroyImmediate(go);
                    else if (go.TryGetComponent<Light>(out var l)) SetupSun(l);

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = 0.0018f;
                RenderSettings.fogColor = new Color(0.72f, 0.78f, 0.84f);

                EditorUtility.DisplayProgressBar("Land Nav", "Building terrain", 0.2f);
                var mats = CreateMaterials();
                var terrain = BuildTerrain(meta, mats);

                EditorUtility.DisplayProgressBar("Land Nav", "Placing features and player", 0.8f);
                BuildBoundary(meta.size);
                var registry = new GameObject("TerrainFeatures").AddComponent<FeatureRegistry>();
                foreach (var f in meta.features)
                {
                    registry.features.Add(new TerrainFeature
                    {
                        id = f.id,
                        displayName = f.name,
                        position = new Vector3(f.x, f.y, f.z),
                        radius = f.r,
                        flag = string.IsNullOrEmpty(f.flag) ? FlagColour.None : (FlagColour)Enum.Parse(typeof(FlagColour), f.flag),
                        teaching = f.teach
                    });
                }

                BuildPlayer(meta, terrain, mats);

                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
                var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
                foreach (var s in EditorBuildSettings.scenes) if (s.path != ScenePath) scenes.Add(new EditorBuildSettingsScene(s.path, false));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log("[LandNav] Training scene built: " + ScenePath);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ---------------------------------------------------------------- textures & materials
        static void ConfigureTextures()
        {
            foreach (var name in new[] { "Tex_GrassLush", "Tex_GrassDry", "Tex_ForestFloor", "Tex_RockDirt" })
                SetImport($"{Tex}/{name}.png", ti => { ti.wrapMode = TextureWrapMode.Repeat; ti.anisoLevel = 4; ti.maxTextureSize = 512; });
            SetImport($"{Tex}/Tex_GrassBlade.png", ti =>
            {
                ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.maxTextureSize = 256;
            });
            SetImport($"{Tex}/TrainingMap.png", ti =>
            {
                ti.wrapMode = TextureWrapMode.Clamp; ti.anisoLevel = 8; ti.maxTextureSize = 4096;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.mipmapEnabled = true;
            });
        }

        static void SetImport(string path, Action<TextureImporter> edit)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogWarning("[LandNav] Missing texture " + path); return; }
            ti.textureType = TextureImporterType.Default;
            edit(ti);
            ti.SaveAndReimport();
        }

        class Mats
        {
            public Material terrain, pole, blue, red, yellow, green, map, board, needle, bark, conifer, broadleaf, post;
        }

        static Material Lit(string name, Color c, float smooth = 0.2f, Texture tex = null)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            if (tex) m.SetTexture("_BaseMap", tex);
            return SaveAsset(m, $"{Gen}/{name}.mat");
        }

        static T SaveAsset<T>(T obj, string path) where T : UnityEngine.Object
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(obj, path);
            return obj;
        }

        static Mats CreateMaterials()
        {
            var m = new Mats();
            m.terrain = SaveAsset(new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")) { name = "Terrain" }, $"{Gen}/Terrain.mat");
            m.pole = Lit("FlagPole", new Color(0.85f, 0.85f, 0.82f), 0.4f);
            m.blue = Lit("Flag_Blue", WorldUI.ToColor(FlagColour.Blue));
            m.red = Lit("Flag_Red", WorldUI.ToColor(FlagColour.Red));
            m.yellow = Lit("Flag_Yellow", WorldUI.ToColor(FlagColour.Yellow));
            m.green = Lit("Flag_Green", WorldUI.ToColor(FlagColour.Green));
            m.board = Lit("MapBoard", new Color(0.18f, 0.2f, 0.16f), 0.3f);
            m.needle = Lit("CompassNeedle", new Color(0.85f, 0.1f, 0.1f), 0.5f);
            m.bark = Lit("Bark", new Color(0.33f, 0.24f, 0.16f), 0.1f);
            m.conifer = Lit("Foliage_Conifer", new Color(0.13f, 0.30f, 0.16f), 0.1f);
            m.broadleaf = Lit("Foliage_Broadleaf", new Color(0.25f, 0.42f, 0.15f), 0.1f);
            m.post = Lit("SignPost", new Color(0.35f, 0.27f, 0.18f), 0.1f);

            var mapTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}/TrainingMap.png");
            var map = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "TrainingMap" };
            map.SetTexture("_BaseMap", mapTex);
            map.SetColor("_BaseColor", new Color(0.92f, 0.92f, 0.92f)); // slightly under white so it is not glaring in a headset
            m.map = SaveAsset(map, $"{Gen}/TrainingMap.mat");
            return m;
        }

        static void SetupSun(Light l)
        {
            l.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            l.intensity = 1.6f;
            l.shadows = LightShadows.Soft;
            l.color = new Color(1f, 0.96f, 0.88f);
        }

        // ---------------------------------------------------------------- terrain
        static Terrain BuildTerrain(MetaJson meta, Mats mats)
        {
            int res = meta.res;
            var td = new TerrainData();
            td.heightmapResolution = res;
            td.size = new Vector3(meta.size, meta.maxHeight, meta.size);

            // heights: float32 metres, row-major [z][x]
            byte[] hb = File.ReadAllBytes(Data + "/heights_f32.bytes");
            var heights = new float[res, res];
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                    heights[z, x] = BitConverter.ToSingle(hb, (z * res + x) * 4) / meta.maxHeight;
            td.SetHeights(0, 0, heights);

            // texture layers, weights painted from the generator's splat map
            var layers = new[]
            {
                Layer("Layer_GrassLush", "Tex_GrassLush", 5f),
                Layer("Layer_GrassDry", "Tex_GrassDry", 5f),
                Layer("Layer_ForestFloor", "Tex_ForestFloor", 4f),
                Layer("Layer_RockDirt", "Tex_RockDirt", 4f),
            };
            td.terrainLayers = layers;
            int sr = meta.splatRes;
            td.alphamapResolution = sr;
            byte[] sb = File.ReadAllBytes(Data + "/splat_f32.bytes");
            var alpha = new float[sr, sr, 4];
            for (int z = 0; z < sr; z++)
                for (int x = 0; x < sr; x++)
                    for (int l = 0; l < 4; l++)
                        alpha[z, x, l] = BitConverter.ToSingle(sb, ((z * sr + x) * 4 + l) * 4);
            td.SetAlphamaps(0, 0, alpha);

            // trees
            var conifer = MakeTreePrefab("Tree_Conifer", ConiferMesh(), mats.bark, mats.conifer, 0.25f);
            var broadleaf = MakeTreePrefab("Tree_Broadleaf", BroadleafMesh(), mats.bark, mats.broadleaf, 0.3f);
            td.treePrototypes = new[] { new TreePrototype { prefab = conifer }, new TreePrototype { prefab = broadleaf } };
            var trees = new List<TreeInstance>();
            var rng = new System.Random(5);
            foreach (var line in File.ReadAllLines(Data + "/trees.csv"))
            {
                var p = line.Split(',');
                if (p.Length < 4) continue;
                float x = float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture);
                float z = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture);
                float s = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture);
                int type = int.Parse(p[3]);
                trees.Add(new TreeInstance
                {
                    position = new Vector3(x / meta.size, 0, z / meta.size),
                    widthScale = s,
                    heightScale = s * (0.9f + 0.2f * (float)rng.NextDouble()),
                    rotation = (float)(rng.NextDouble() * Math.PI * 2),
                    prototypeIndex = type,
                    color = Color.white,
                    lightmapColor = Color.white
                });
            }
            td.SetTreeInstances(trees.ToArray(), true);

            // grass detail on open ground (none under the woodland canopy or on rocky slopes)
            var blade = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}/Tex_GrassBlade.png");
            td.detailPrototypes = new[]
            {
                new DetailPrototype
                {
                    prototypeTexture = blade,
                    renderMode = DetailRenderMode.GrassBillboard,
                    usePrototypeMesh = false,
                    useInstancing = false,
                    healthyColor = new Color(0.55f, 0.75f, 0.35f),
                    dryColor = new Color(0.75f, 0.72f, 0.42f),
                    minWidth = 0.35f, maxWidth = 0.7f,
                    minHeight = 0.2f, maxHeight = 0.5f,
                    noiseSpread = 0.4f
                }
            };
            td.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            td.SetDetailResolution(512, 32);
            var detail = new int[512, 512];
            for (int z = 0; z < 512; z++)
                for (int x = 0; x < 512; x++)
                {
                    int sz = Mathf.Min(sr - 1, z * sr / 512), sx = Mathf.Min(sr - 1, x * sr / 512);
                    float open = alpha[sz, sx, 0] + alpha[sz, sx, 1];
                    detail[z, x] = open > 0.6f ? Mathf.RoundToInt(open * 2.5f) : 0;
                }
            td.SetDetailLayer(0, 0, 0, detail);

            SaveAsset(td, $"{Gen}/TrainingTerrain.asset");
            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Terrain";
            var t = go.GetComponent<Terrain>();
            t.materialTemplate = mats.terrain;
            t.heightmapPixelError = 4;
            t.basemapDistance = 250;
            t.treeDistance = 500;
            t.treeBillboardDistance = 500;
            t.detailObjectDistance = 45;
            t.detailObjectDensity = 0.8f;
            t.drawInstanced = true;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
            return t;
        }

        static TerrainLayer Layer(string name, string tex, float tile)
        {
            var l = new TerrainLayer
            {
                diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}/{tex}.png"),
                tileSize = new Vector2(tile, tile),
                smoothness = 0.05f,
                metallic = 0f
            };
            return SaveAsset(l, $"{Gen}/{name}.terrainlayer");
        }

        // ---------------------------------------------------------------- tree meshes (low-poly, flat shaded)
        class MeshBuilder
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<int>[] tris = { new List<int>(), new List<int>() };
            public void Tri(int sub, Vector3 a, Vector3 b, Vector3 c)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c);
                tris[sub].Add(i); tris[sub].Add(i + 1); tris[sub].Add(i + 2);
            }
            public void Cone(int sub, float y0, float y1, float r0, float r1, int sides, float twist = 0)
            {
                for (int k = 0; k < sides; k++)
                {
                    float a0 = (k / (float)sides) * Mathf.PI * 2 + twist, a1 = ((k + 1) / (float)sides) * Mathf.PI * 2 + twist;
                    Vector3 b0 = new Vector3(Mathf.Cos(a0) * r0, y0, Mathf.Sin(a0) * r0);
                    Vector3 b1 = new Vector3(Mathf.Cos(a1) * r0, y0, Mathf.Sin(a1) * r0);
                    Vector3 t0 = new Vector3(Mathf.Cos(a0) * r1, y1, Mathf.Sin(a0) * r1);
                    Vector3 t1 = new Vector3(Mathf.Cos(a1) * r1, y1, Mathf.Sin(a1) * r1);
                    Tri(sub, b0, t0, b1);
                    if (r1 > 0.001f) Tri(sub, b1, t0, t1);
                    Tri(sub, new Vector3(0, y0, 0), b0, b1); // underside cap
                }
            }
            public Mesh Build(string name)
            {
                var m = new Mesh { name = name };
                m.SetVertices(v);
                m.subMeshCount = 2;
                m.SetTriangles(tris[0], 0);
                m.SetTriangles(tris[1], 1);
                m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            }
        }

        static Mesh ConiferMesh()
        {
            var b = new MeshBuilder();
            b.Cone(0, 0f, 3.2f, 0.22f, 0.14f, 6);
            b.Cone(1, 1.6f, 6.0f, 2.4f, 0f, 7);
            b.Cone(1, 3.6f, 8.2f, 1.9f, 0f, 7, 0.4f);
            b.Cone(1, 5.6f, 10.4f, 1.3f, 0f, 7, 0.8f);
            return b.Build("Conifer");
        }

        static Mesh BroadleafMesh()
        {
            var b = new MeshBuilder();
            b.Cone(0, 0f, 4.2f, 0.28f, 0.18f, 6);
            // faceted crown: two stacked frustums and caps
            b.Cone(1, 3.4f, 5.6f, 1.6f, 3.2f, 8);
            b.Cone(1, 5.6f, 7.6f, 3.2f, 2.6f, 8, 0.2f);
            b.Cone(1, 7.6f, 8.8f, 2.6f, 0f, 8, 0.4f);
            return b.Build("Broadleaf");
        }

        static GameObject MakeTreePrefab(string name, Mesh mesh, Material bark, Material foliage, float trunkRadius)
        {
            mesh = SaveAsset(mesh, $"{Gen}/{name}.mesh");
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { bark, foliage };
            var cap = go.AddComponent<CapsuleCollider>();
            cap.radius = trunkRadius + 0.1f; cap.height = 4f; cap.center = new Vector3(0, 2f, 0);
            string path = $"{Gen}/{name}.prefab";
            AssetDatabase.DeleteAsset(path);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        // ---------------------------------------------------------------- boundary
        static void BuildBoundary(float size)
        {
            var root = new GameObject("Boundary");
            void Wall(string n, Vector3 c, Vector3 s)
            {
                var w = new GameObject(n);
                w.transform.SetParent(root.transform);
                w.transform.position = c;
                w.AddComponent<BoxCollider>().size = s;
            }
            float h = 300, m = 3;
            Wall("South", new Vector3(size / 2, 0, m), new Vector3(size, h, 1));
            Wall("North", new Vector3(size / 2, 0, size - m), new Vector3(size, h, 1));
            Wall("West", new Vector3(m, 0, size / 2), new Vector3(1, h, size));
            Wall("East", new Vector3(size - m, 0, size / 2), new Vector3(1, h, size));
        }

        // ---------------------------------------------------------------- player, map, flags, signs
        static void BuildPlayer(MetaJson meta, Terrain terrain, Mats mats)
        {
            Vector3 start = new Vector3(meta.start.x, 0, meta.start.z);
            start.y = terrain.SampleHeight(start) + 0.1f;
            Vector3 look = new Vector3(meta.size / 2, 0, meta.size / 2) - start; look.y = 0;

            var session = new GameObject("Session").AddComponent<SessionManager>();

            // EZPZ players: flat screen (default, WebGL) and XR (switched on when a headset is active)
            GameObject Spawn(string path)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { Debug.LogError("[LandNav] Missing EZPZ prefab: " + path); return null; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.SetPositionAndRotation(start, Quaternion.LookRotation(look));
                return go;
            }
            var flat = Spawn(FlatPlayerPrefab);
            var xr = Spawn(XRPlayerPrefab);
            if (xr) xr.SetActive(false);

            var kitGo = new GameObject("LandNav Kit");
            var kit = kitGo.AddComponent<FlagKit>();
            kit.poleMaterial = mats.pole;
            kit.blueMaterial = mats.blue; kit.redMaterial = mats.red; kit.yellowMaterial = mats.yellow; kit.greenMaterial = mats.green;

            var mapGo = new GameObject("Map");
            mapGo.transform.SetParent(kitGo.transform, false);
            var map = mapGo.AddComponent<MapBoard>();
            map.mapMaterial = mats.map; map.boardMaterial = mats.board; map.needleMaterial = mats.needle; map.faceMaterial = mats.pole;

            var bridge = kitGo.AddComponent<LandNavPlayerBridge>();
            bridge.flatPlayer = flat; bridge.xrPlayer = xr;
            bridge.map = map; bridge.kit = kit;

            session.kit = kit; session.map = map;

            // start-point information board, a few metres ahead of the player, facing them
            Vector3 signPos = start + look.normalized * 4f + Vector3.Cross(Vector3.up, look.normalized) * -1.5f;
            signPos.y = terrain.SampleHeight(signPos);
            var sign = new GameObject("StartSign").AddComponent<InfoSign>();
            sign.transform.SetPositionAndRotation(signPos, Quaternion.LookRotation(signPos - start - Vector3.up * (signPos.y - start.y)));
            sign.postMaterial = mats.post;
            sign.title = "TERRAIN IDENTIFICATION";
            sign.body =
                "You carry four flags: <b>Blue, Red, Yellow, Green</b>.\n" +
                "Your map shows where each one belongs. Use the contours to work out what feature is at each spot, " +
                "walk there, and plant that flag.\n\n" +
                "A sign on the flag tells you if you were right. Pull a flag up and try again whenever you like.\n\n" +
                "<b>VR</b>: left stick walk, right stick turn, A switch flag, trigger plant, B pull up.\n" +
                "<b>Keyboard</b>: WASD walk, mouse look, M map, 1-4 flag, F plant, G pull up.\n\n" +
                "Take breaks. Remove the headset if you feel unwell.";
            sign.size = new Vector2(1.5f, 1.15f);
        }
    }
}
