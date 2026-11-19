#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TeksMaster.Core;
using TeksMaster.Shop;
using TeksMaster.World;

namespace TeksMaster.Editor
{
    /// <summary>
    /// Strips procedural Edit-Mode objects during every Player build so level0 never contains
    /// unserializable runtime Texture2D/Material blobs. TeksGameBootstrapper builds the full
    /// 3D world cleanly at runtime on startup.
    /// </summary>
    public class TeksBuildSceneCleaner : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (!BuildPipeline.isBuildingPlayer) return;

            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var go = roots[i];
                if (go == null) continue;
                if (go.name == "BarangayCourtWorld" ||
                    go.name == "PlayerAvatar" ||
                    go.name == "TeksUICanvas" ||
                    go.name == "TeksMatchRoot")
                {
                    Object.DestroyImmediate(go);
                }
            }
        }
    }

    [InitializeOnLoad]
    public static class TeksMasterEditorSetup
    {
        static TeksMasterEditorSetup()
        {
            EditorApplication.delayCall += EnsureCleanBootScene;
        }

        private static void EnsureCleanBootScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;

            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded) return;

            // Remove any leftover scene-saved procedural objects that could turn purple or corrupt builds
            bool removedAny = RemoveProceduralPreviewObjects(activeScene);

            var existing = Object.FindAnyObjectByType<TeksGameBootstrapper>();
            if (existing == null)
            {
                var go = new GameObject("TeksGameManager");
                go.AddComponent<TeksGameBootstrapper>();
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
            else if (removedAny)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }

        private static bool RemoveProceduralPreviewObjects(Scene scene)
        {
            bool removed = false;
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var go = roots[i];
                if (go == null) continue;
                if (go.name == "BarangayCourtWorld" ||
                    go.name == "PlayerAvatar" ||
                    go.name == "TeksUICanvas" ||
                    go.name == "TeksMatchRoot")
                {
                    Object.DestroyImmediate(go);
                    removed = true;
                }
            }
            return removed;
        }

        [MenuItem("Teks Master/Build && Run Fullscreen App (Clean)")]
        public static void BuildAndRunCleanFullscreenApp()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[Teks Master] Stop Play Mode before building.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                RemoveProceduralPreviewObjects(scene);
                if (Object.FindAnyObjectByType<TeksGameBootstrapper>() == null)
                {
                    new GameObject("TeksGameManager").AddComponent<TeksGameBootstrapper>();
                }
                EditorSceneManager.SaveScene(scene);
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildDir = Path.Combine(projectRoot, "App");
            string exePath = Path.Combine(buildDir, "Teks.exe");

            if (!Directory.Exists(buildDir))
                Directory.CreateDirectory(buildDir);

            // Force a clean Player data rebuild so Unity never reuses a stale/corrupted level0
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CleanBuildCache | BuildOptions.AutoRunPlayer
            };

            Debug.Log("[Teks Master] Starting Clean Fullscreen Build to: " + exePath);
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[Teks Master] Clean Build Succeeded! Launching " + exePath);
            }
            else
            {
                Debug.LogError("[Teks Master] Build failed: " + report.summary.result);
            }
        }

        [MenuItem("Teks Master/Clean Scene (Fix Purple Screen)")]
        public static void CleanActiveScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[Teks Master] Exit Play Mode first.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            RemoveProceduralPreviewObjects(scene);
            if (Object.FindAnyObjectByType<TeksGameBootstrapper>() == null)
            {
                new GameObject("TeksGameManager").AddComponent<TeksGameBootstrapper>();
            }
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Teks Master] Scene cleaned and saved! Press Play or run 'Teks Master > Build & Run Fullscreen App (Clean)'.");
        }

        [MenuItem("Teks Master/Give +1,000 Barya Coins")]
        public static void GiveBonusCoins()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddCoins(1000);
                Debug.Log("[Teks Master] Added +1,000 Barya Coins!");
            }
            else
            {
                Debug.LogWarning("[Teks Master] Enter Play Mode first to grant runtime Barya Coins.");
            }
        }

        [MenuItem("Teks Master/Force Sari-Sari Store 5-Minute Rotation")]
        public static void ForceShopRefresh()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.RefreshShopSlots(resetTimer: true);
                BarangayCourtBuilder.Instance?.RefreshStoreDisplayCards();
                Debug.Log("[Teks Master] Sari-Sari Store rotated 4 new Teks cards and reset 05:00 timer!");
            }
            else
            {
                Debug.LogWarning("[Teks Master] Enter Play Mode first to rotate the Sari-Sari Store.");
            }
        }
    }
}
#endif
