using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Replaces the placeholder enemy/wall visuals with Synty rocks.
// Runs once automatically after compiling; can be re-run from Tools > Synty Visuals > Apply.
[InitializeOnLoad]
public static class SyntyVisualsSetup
{
    const string RockFolder = "Assets/Asset Store/Synty/PolygonGeneric/Prefabs/Environment/";
    const string ScenePath = "Assets/Scenes/MyGame.unity";
    static readonly string[] EnemyPrefabs =
    {
        "Assets/Prefabs/Enemy1.prefab",
        "Assets/Prefabs/Enemy2.prefab",
        "Assets/Prefabs/Enemy3.prefab",
        "Assets/Prefabs/Enemy4.prefab",
    };
    static readonly float[] EnemySizes = { 1.2f, 1.4f, 1.1f, 1.0f };
    static readonly string[] WallNames = { "LeftWall", "RightWall" };

    static string DoneKey => "SyntyVisualsSetup.Done." + Application.dataPath;

    struct Rock
    {
        public string name;
        public Mesh mesh;
        public Material[] materials;
        public Quaternion rotation;
        public Bounds bounds; // world bounds at scale 1, rotated, at origin
    }

    static SyntyVisualsSetup()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[SyntyVisuals] Skipped because Play mode is active. Run Tools > Synty Visuals > Apply.");
                return;
            }
            Apply();
        };
    }

    [MenuItem("Tools/Synty Visuals/Apply")]
    public static void Apply()
    {
        List<Rock> rocks = LoadRocks();
        if (rocks.Count < 4)
        {
            Debug.LogError("[SyntyVisuals] Could not find Synty rock prefabs in " + RockFolder);
            return;
        }

        // The most compact (boulder-like) rocks become enemies.
        List<Rock> enemyRocks = rocks.OrderByDescending(Compactness).Take(EnemyPrefabs.Length).ToList();
        for (int i = 0; i < EnemyPrefabs.Length; i++)
            SetupEnemy(EnemyPrefabs[i], enemyRocks[i], EnemySizes[i]);

        SetupWalls(rocks);

        EditorPrefs.SetBool(DoneKey, true);
        Debug.Log("[SyntyVisuals] Done: enemies use Synty rocks and walls are covered with rocks.");
    }

    static List<Rock> LoadRocks()
    {
        var rocks = new List<Rock>();
        for (int i = 1; i <= 10; i++)
        {
            string path = RockFolder + $"SM_Gen_Env_Rock_{i:00}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var filter = prefab.GetComponentInChildren<MeshFilter>();
            var renderer = prefab.GetComponentInChildren<MeshRenderer>();
            if (filter == null || renderer == null || filter.sharedMesh == null) continue;

            Quaternion rotation = filter.transform.rotation;
            rocks.Add(new Rock
            {
                name = prefab.name,
                mesh = filter.sharedMesh,
                materials = renderer.sharedMaterials,
                rotation = rotation,
                bounds = RotatedBounds(filter.sharedMesh.bounds, rotation),
            });
        }
        return rocks;
    }

    static Bounds RotatedBounds(Bounds b, Quaternion rotation)
    {
        var result = new Bounds(rotation * b.center, Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            var corner = b.center + Vector3.Scale(b.extents, new Vector3(
                (i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            result.Encapsulate(rotation * corner);
        }
        return result;
    }

    static float MaxDim(Bounds b) => Mathf.Max(b.size.x, b.size.y, b.size.z);

    static float Compactness(Rock r)
    {
        Vector3 s = r.bounds.size;
        return Mathf.Min(s.x, s.y, s.z) / Mathf.Max(s.x, s.y, s.z);
    }

    static void SetupEnemy(string prefabPath, Rock rock, float size)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            float scale = size / MaxDim(rock.bounds);

            var filter = root.GetComponent<MeshFilter>();
            var renderer = root.GetComponent<MeshRenderer>();
            filter.sharedMesh = rock.mesh;
            renderer.sharedMaterials = rock.materials;

            // Swap the primitive collider for one matching the rock shape.
            foreach (var col in root.GetComponents<Collider>())
                Object.DestroyImmediate(col);
            var meshCollider = root.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = rock.mesh;
            meshCollider.convex = true; // required for a non-kinematic Rigidbody

            root.transform.localRotation = rock.rotation;
            root.transform.localScale = Vector3.one * scale;
            // Sit just above the ground (y = 0); the spawner uses this Y.
            Vector3 pos = root.transform.localPosition;
            pos.y = -rock.bounds.min.y * scale + 0.02f;
            root.transform.localPosition = pos;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[SyntyVisuals] {root.name} -> {rock.name}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void SetupWalls(List<Rock> rocks)
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = false;
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            openedHere = true;
        }

        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true));
        var random = new System.Random(1234);

        foreach (string wallName in WallNames)
        {
            Transform wall = all.FirstOrDefault(t => t.name == wallName);
            if (wall == null)
            {
                Debug.LogWarning("[SyntyVisuals] Wall not found: " + wallName);
                continue;
            }

            // Remove a previous run's rocks so re-running doesn't duplicate them.
            foreach (var old in all.Where(t => t != null && t.name == wallName + " Rocks").ToList())
                Object.DestroyImmediate(old.gameObject);

            // Keep the BoxCollider for physics, just hide the grey box.
            var wallRenderer = wall.GetComponent<MeshRenderer>();
            if (wallRenderer != null) wallRenderer.enabled = false;

            Bounds wb = wall.GetComponent<Collider>().bounds;
            float inwardSign = wb.center.x > 0 ? -1f : 1f; // direction towards the play area
            float innerFaceX = wb.center.x + inwardSign * wb.extents.x;

            var group = new GameObject(wallName + " Rocks");
            SceneManager.MoveGameObjectToScene(group, scene);

            int n = 0;
            float z = wb.min.z;
            while (z < wb.max.z)
            {
                Rock rock = rocks[random.Next(rocks.Count)];
                float size = Mathf.Lerp(1.8f, 2.6f, (float)random.NextDouble());
                float scale = size / MaxDim(rock.bounds);
                Quaternion spin = Quaternion.Euler(0, (float)random.NextDouble() * 360f, 0);
                Quaternion rotation = spin * rock.rotation;
                Bounds b = RotatedBounds(rock.mesh.bounds, rotation);
                b = new Bounds(b.center * scale, b.size * scale);

                // Inner edge of the rock lines up with the collider's inner face.
                float x = inwardSign > 0
                    ? innerFaceX - b.max.x
                    : innerFaceX - b.min.x;

                var go = new GameObject($"Rock_{n++:00}");
                go.transform.SetParent(group.transform, false);
                go.transform.SetPositionAndRotation(new Vector3(x, -b.min.y - 0.1f, z - b.min.z), rotation);
                go.transform.localScale = Vector3.one * scale;
                go.AddComponent<MeshFilter>().sharedMesh = rock.mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = rock.materials;
                go.isStatic = true;

                z += b.size.z * 0.7f; // overlap so the wall has no gaps
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (openedHere) EditorSceneManager.CloseScene(scene, true);
    }
}
