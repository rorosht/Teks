using UnityEngine;
using TeksMaster.Audio;
using TeksMaster.Characters;
using TeksMaster.Gameplay;
using TeksMaster.Shop;
using TeksMaster.UI;
using TeksMaster.World;

namespace TeksMaster.Core
{
    public class TeksGameBootstrapper : MonoBehaviour
    {
        private static bool _runtimeBootstrapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            _runtimeBootstrapped = false;
            EnsureBootstrapped();
        }

        public static TeksGameBootstrapper EnsureBootstrapped()
        {
            var existing = FindAnyObjectByType<TeksGameBootstrapper>();
            if (existing == null)
            {
                var go = new GameObject("TeksGameManager");
                existing = go.AddComponent<TeksGameBootstrapper>();
            }

            if (Application.isPlaying && !_runtimeBootstrapped)
            {
                _runtimeBootstrapped = true;
                existing.InitializeRuntimeSystems();
            }

            return existing;
        }

        private void Start()
        {
            if (Application.isPlaying && !_runtimeBootstrapped)
            {
                _runtimeBootstrapped = true;
                InitializeRuntimeSystems();
            }
        }

        public void InitializeRuntimeSystems()
        {
            var inv = GetOrAdd<PlayerInventory>(gameObject);
            inv.LoadFromDisk();
            inv.EnsureStarterCollection();

            if (inv.AudioSettings != null)
            {
                bool fs = inv.AudioSettings.IsFullscreen;
                Screen.fullScreenMode = fs ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                Screen.fullScreen = fs;
            }

            var audioMgr = GetOrAdd<TeksAudioManager>(gameObject);
            audioMgr.EnsureInitialized();
            audioMgr.BindInventory(inv);
            audioMgr.StartBgm();

            var shop = GetOrAdd<ShopManager>(gameObject);
            shop.EnsureInitialized();

            var court = GetOrAdd<BarangayCourtBuilder>(gameObject);
            court.BuildWorldIfNeeded();
            court.RefreshAreaGates();
            court.RefreshHiddenCardStates();

            GetOrAdd<TeksMatchManager>(gameObject);
            GetOrAdd<TeksGameModeManager>(gameObject);

            // Spawn or reuse PlayerAvatar cleanly on the main Kalye road (North lane, facing North toward court & store)
            var playerGo = GameObject.Find("PlayerAvatar");
            if (playerGo == null)
            {
                playerGo = new GameObject("PlayerAvatar");
            }
            var player = GetOrAdd<PlayerController>(playerGo);
            player.InitializePlayer(new Vector3(0f, 0f, -14.2f), Quaternion.Euler(0f, 0f, 0f));
            player.ExternalInteractHandlerActive = true;

            // Frame main camera nicely on startup behind the road spawn
            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(0f, 4.8f, -23.5f);
                Camera.main.transform.LookAt(new Vector3(0f, 1.35f, -13.5f));
            }

            var ui = GetOrAdd<TeksUIManager>(gameObject);
            ui.BuildUIIfNeeded();
            ui.RebuildNpcTags();

            var sampleRend = court.CourtRoot != null ? court.CourtRoot.GetComponentInChildren<Renderer>() : null;
            string shaderName = (sampleRend != null && sampleRend.sharedMaterial != null && sampleRend.sharedMaterial.shader != null)
                ? sampleRend.sharedMaterial.shader.name
                : "NONE";
            Debug.Log($"[Teks Master] Runtime initialized! NPCs={court.SpawnedOpponents.Count}, HiddenCards={court.SpawnedHiddenCards.Count}, Shader={shaderName}, Cam={(Camera.main != null ? Camera.main.name : "NONE")}");
        }

        public void SetupEditModeCourtPreview()
        {
            var inv = GetOrAdd<PlayerInventory>(gameObject);
            inv.EnsureStarterCollection();

            var shop = GetOrAdd<ShopManager>(gameObject);
            shop.EnsureInitialized();

            var court = GetOrAdd<BarangayCourtBuilder>(gameObject);
            court.BuildWorldIfNeeded();

            var worldGo = GameObject.Find("BarangayCourtWorld");
            if (worldGo != null && !Application.isPlaying)
            {
                worldGo.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }

            var playerGo = GameObject.Find("PlayerAvatar");
            if (playerGo == null)
            {
                playerGo = new GameObject("PlayerAvatar");
                playerGo.transform.position = new Vector3(0f, 0f, -14.2f);
                playerGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                CharacterVisualBuilder.BuildCharacterVisual(
                    playerGo,
                    CharacterOutfitRole.MaleKid,
                    new Color(0.95f, 0.95f, 0.96f),
                    new Color(0.14f, 0.16f, 0.24f),
                    new Color(0.75f, 0.51f, 0.37f),
                    new Color(0.18f, 0.56f, 0.86f)
                );
            }
            if (playerGo != null && !Application.isPlaying)
            {
                playerGo.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }

            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(0f, 4.8f, -23.5f);
                Camera.main.transform.LookAt(new Vector3(0f, 1.35f, -13.5f));
            }
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }
    }
}
