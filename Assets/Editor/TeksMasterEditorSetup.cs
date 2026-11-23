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
            TeksMasterEditorSetup.RemoveProceduralPreviewObjects(scene);
        }
    }

    [InitializeOnLoad]
    public static class TeksMasterEditorSetup
    {
        private static bool _restoringAfterSave;

        static TeksMasterEditorSetup()
        {
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += EnsureScenePreview;
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            if (Application.isPlaying || BuildPipeline.isBuildingPlayer) return;
            // Remove procedural preview right before writing the .unity file so SampleScene stays clean (~12 KB)
            if (RemoveProceduralPreviewObjects(scene))
            {
                _restoringAfterSave = true;
            }
        }

        private static void OnSceneSaved(Scene scene)
        {
            if (!_restoringAfterSave) return;
            _restoringAfterSave = false;
            EditorApplication.delayCall += EnsureScenePreview;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded)
                {
                    RemoveProceduralPreviewObjects(scene);
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += EnsureScenePreview;
            }
        }

        private static void EnsureScenePreview()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;

            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded) return;

            var bootstrapper = Object.FindAnyObjectByType<TeksGameBootstrapper>();
            if (bootstrapper == null)
            {
                var go = new GameObject("TeksGameManager");
                bootstrapper = go.AddComponent<TeksGameBootstrapper>();
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            // Build or refresh the 3D Scene View preview cleanly (never saved to disk!)
            bool needsPreview = GameObject.Find("BarangayCourtWorld") == null;
            if (!needsPreview)
            {
                // If materials went stale/purple after a build reload, rebuild the preview cleanly
                var existingWorld = GameObject.Find("BarangayCourtWorld");
                var rend = existingWorld != null ? existingWorld.GetComponentInChildren<Renderer>() : null;
                if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.shader == null ||
                    rend.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
                {
                    RemoveProceduralPreviewObjects(activeScene);
                    needsPreview = true;
                }
            }

            if (needsPreview)
            {
                bootstrapper.SetupEditModeCourtPreview();
                MarkPreviewDontSave(GameObject.Find("BarangayCourtWorld"));
                MarkPreviewDontSave(GameObject.Find("PlayerAvatar"));
                SceneView.RepaintAll();
            }
        }

        private static void MarkPreviewDontSave(GameObject root)
        {
            if (root == null) return;
            const HideFlags previewFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            root.hideFlags = previewFlags;

            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                tr.gameObject.hideFlags = previewFlags;
            }
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = rend.sharedMaterials;
                if (mats == null) continue;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mats[i])))
                    {
                        mats[i].hideFlags = previewFlags;
                        if (mats[i].HasProperty("_BaseMap"))
                        {
                            var tex = mats[i].GetTexture("_BaseMap");
                            if (tex != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex)))
                                tex.hideFlags = previewFlags;
                        }
                        if (mats[i].HasProperty("_MainTex"))
                        {
                            var tex = mats[i].GetTexture("_MainTex");
                            if (tex != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex)))
                                tex.hideFlags = previewFlags;
                        }
                    }
                }
            }
        }

        public static bool RemoveProceduralPreviewObjects(Scene scene)
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
                    go.name == "TeksMasterCanvas" ||
                    go.name == "TeksMatchRoot")
                {
                    Object.DestroyImmediate(go);
                    removed = true;
                }
            }
            return removed;
        }

        [MenuItem("Teks Master/Refresh Scene View Preview")]
        public static void RefreshSceneViewPreview()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[Teks Master] Exit Play Mode before refreshing Edit-Mode Scene View preview.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            RemoveProceduralPreviewObjects(scene);
            var bootstrapper = Object.FindAnyObjectByType<TeksGameBootstrapper>();
            if (bootstrapper == null)
            {
                bootstrapper = new GameObject("TeksGameManager").AddComponent<TeksGameBootstrapper>();
            }
            bootstrapper.SetupEditModeCourtPreview();
            MarkPreviewDontSave(GameObject.Find("BarangayCourtWorld"));
            MarkPreviewDontSave(GameObject.Find("PlayerAvatar"));
            SceneView.RepaintAll();
            Debug.Log("[Teks Master] Scene View 3D preview refreshed (safe: not saved into SampleScene.unity).");
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

            // Restore the Scene View preview after build finishes
            EditorApplication.delayCall += EnsureScenePreview;
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

        [MenuItem("Teks Master/Give +100 Gems")]
        public static void GiveBonusGems()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddGems(100);
                Debug.Log("[Teks Master] Added +100 Gems!");
            }
            else
            {
                Debug.LogWarning("[Teks Master] Enter Play Mode first to grant runtime Gems.");
            }
        }

        [MenuItem("Teks Master/Reset Game Progress To Zero")]
        public static void ResetGameProgressToZero()
        {
            TeksSaveSystem.DeleteSaveFilesForce();

            if (Application.isPlaying && PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.ResetProgressToZero();
                BarangayCourtBuilder.Instance?.RefreshAreaGates();
                BarangayCourtBuilder.Instance?.RefreshHiddenCardStates();
                if (TeksMaster.Characters.PlayerController.Instance != null)
                {
                    TeksMaster.Characters.PlayerController.Instance.TeleportForMatch(
                        new Vector3(24.8f, 0f, 5.2f),
                        Quaternion.Euler(0f, -145f, 0f));
                }
                Debug.Log("[Teks Master] Game progress reset to ZERO! Basketball Court, Old Neighborhood & Fiesta Arena are now LOCKED.");
            }
            else
            {
                Debug.Log("[Teks Master] Deleted saved progress (teks_save.json). Press Play to test from zero with locked areas!");
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
