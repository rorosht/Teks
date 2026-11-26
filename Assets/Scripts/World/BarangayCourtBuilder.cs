using System.Collections.Generic;
using UnityEngine;
using TeksMaster.Core;
using TeksMaster.Characters;
using TeksMaster.Shop;
using TeksMaster.Visuals;

namespace TeksMaster.World
{
    public class BarangayCourtBuilder : MonoBehaviour
    {
        public static BarangayCourtBuilder Instance { get; private set; }

        public const string TournamentHostDisplayName = "KUYA REF (TOURNAMENT HOST)";

        public Transform CourtRoot { get; private set; }
        public Transform TournamentHostTransform { get; private set; }
        public Vector3 StoreInteractionPoint { get; private set; } = new Vector3(-4.5f, 0f, -11.2f);
        public Vector3 TournamentInteractionPoint { get; private set; } = new Vector3(-22.8f, 0f, -22.2f);
        public Vector3 TournamentBattleCenter { get; private set; } = new Vector3(-22.8f, 0f, -28.5f);
        public List<Vector3> StoreInteractionPoints { get; } = new List<Vector3>();
        public List<NpcOpponent> SpawnedOpponents { get; } = new List<NpcOpponent>();
        public List<HiddenCardCollectible> SpawnedHiddenCards { get; } = new List<HiddenCardCollectible>();
        public List<AreaGateBarrier> SpawnedAreaGates { get; } = new List<AreaGateBarrier>();

        private readonly List<TextMesh> _storeTimerMeshes = new List<TextMesh>();
        private readonly List<Renderer> _storeRackCardRenderers = new List<Renderer>();
        private static readonly Dictionary<string, Material> _houseUrpMatCache = new Dictionary<string, Material>();
        private bool _built;
        private ShopManager _subscribedShop;
        private PlayerInventory _subscribedInventory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
            _houseUrpMatCache.Clear();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (!Application.isPlaying) return;

            SubscribeRuntimeEvents();
            RefreshStoreDisplayCards();
            RefreshAreaGates();
            RefreshHiddenCardStates();
        }

        private void OnDestroy()
        {
            UnsubscribeRuntimeEvents();
            if (Instance == this) Instance = null;
        }

        private void SubscribeRuntimeEvents()
        {
            if (!Application.isPlaying) return;

            if (ShopManager.Instance != null && _subscribedShop != ShopManager.Instance)
            {
                if (_subscribedShop != null) _subscribedShop.OnShopUpdated -= RefreshStoreDisplayCards;
                _subscribedShop = ShopManager.Instance;
                _subscribedShop.OnShopUpdated += RefreshStoreDisplayCards;
            }

            if (PlayerInventory.Instance != null && _subscribedInventory != PlayerInventory.Instance)
            {
                if (_subscribedInventory != null)
                {
                    _subscribedInventory.OnAreasChanged -= RefreshAreaGates;
                    _subscribedInventory.OnAreaUnlocked -= HandleAreaUnlocked;
                }
                _subscribedInventory = PlayerInventory.Instance;
                _subscribedInventory.OnAreasChanged += RefreshAreaGates;
                _subscribedInventory.OnAreaUnlocked += HandleAreaUnlocked;
            }
        }

        private void UnsubscribeRuntimeEvents()
        {
            if (_subscribedShop != null)
            {
                _subscribedShop.OnShopUpdated -= RefreshStoreDisplayCards;
                _subscribedShop = null;
            }

            if (_subscribedInventory != null)
            {
                _subscribedInventory.OnAreasChanged -= RefreshAreaGates;
                _subscribedInventory.OnAreaUnlocked -= HandleAreaUnlocked;
                _subscribedInventory = null;
            }
        }

        private void HandleAreaUnlocked(BarangayAreaId unlockedArea)
        {
            RefreshAreaGates();
        }

        private void Update()
        {
            if (ShopManager.Instance == null) return;
            string timerText = "NEW CARDS IN\n" + ShopManager.Instance.GetFormattedTimer();
            for (int i = 0; i < _storeTimerMeshes.Count; i++)
            {
                if (_storeTimerMeshes[i] != null && _storeTimerMeshes[i].text != timerText)
                {
                    _storeTimerMeshes[i].text = timerText;
                }
            }
        }

        public struct BarangayZoneInfo
        {
            public BarangayAreaId AreaId;
            public string Name;
            public string Subtitle;
            public string UnlockHint;
            public Vector3 SpawnPosition;
            public float SpawnYaw;
        }

        public static readonly BarangayZoneInfo[] WorldZones = new[]
        {
            new BarangayZoneInfo
            {
                AreaId = BarangayAreaId.PlayersNeighborhood,
                Name = "PLAYER'S NEIGHBORHOOD",
                Subtitle = "Your House, Plaza Gazebo & Dodong (Starter Area)",
                UnlockHint = "Starting Area (Unlocked)",
                SpawnPosition = new Vector3(24.8f, 0f, 5.2f),
                SpawnYaw = -145f
            },
            new BarangayZoneInfo
            {
                AreaId = BarangayAreaId.SariSariAndKalye,
                Name = "SARI-SARI & KALYE",
                Subtitle = "Aling Nena's Kalye Kiosk, Jeepney, Fishball Cart & Mark",
                UnlockHint = "Starting Area (Unlocked)",
                SpawnPosition = new Vector3(-2.5f, 0f, -11.4f),
                SpawnYaw = 180f
            },
            new BarangayZoneInfo
            {
                AreaId = BarangayAreaId.BasketballCourt,
                Name = "BASKETBALL COURT",
                Subtitle = "Main Court, Sideline Bleachers & Carlo",
                UnlockHint = "Defeat Dodong & Mark (or win 2 Starter Area matches)",
                SpawnPosition = new Vector3(-3.8f, 0f, -4.6f),
                SpawnYaw = 34f
            },
            new BarangayZoneInfo
            {
                AreaId = BarangayAreaId.OldNeighborhood,
                Name = "OLD NEIGHBORHOOD",
                Subtitle = "West Tambayan Alley, Waiting Shed & Joshua",
                UnlockHint = "Defeat Carlo (or win 2 Basketball Court matches)",
                SpawnPosition = new Vector3(-17.8f, 0f, -0.5f),
                SpawnYaw = -90f
            },
            new BarangayZoneInfo
            {
                AreaId = BarangayAreaId.FiestaArena,
                Name = "FIESTA TEKS ARENA",
                Subtitle = "Covered Stage, Banderitas & Teks Master",
                UnlockHint = "Defeat Joshua (or win 2 Old Neighborhood matches)",
                SpawnPosition = new Vector3(18.5f, 0f, -25.5f),
                SpawnYaw = 115f
            },
            new BarangayZoneInfo
            {
                AreaId = BarangayAreaId.TournamentArena,
                Name = "TOURNAMENT ARENA",
                Subtitle = "3-Round Championship Bracket, Trophy Podium & Kuya Ref",
                UnlockHint = "Open Arena (Pay 150 Coins Entry Fee to Compete)",
                SpawnPosition = new Vector3(-22.8f, 0f, -22.0f),
                SpawnYaw = 180f
            }
        };

        public static BarangayAreaId GetAreaAtPosition(Vector3 pos)
        {
            if (pos.x <= -14.5f && pos.z <= -20.8f)
            {
                return BarangayAreaId.TournamentArena;
            }
            if (pos.x >= 14.5f && pos.z <= -20.8f)
            {
                return BarangayAreaId.FiestaArena;
            }
            if (pos.x <= -14.5f && pos.z > -8.4f)
            {
                return BarangayAreaId.OldNeighborhood;
            }
            if (pos.x >= 14.5f && pos.z > -11.5f)
            {
                return BarangayAreaId.PlayersNeighborhood;
            }
            if (pos.x > -14.5f && pos.x < 14.5f && pos.z > -8.4f)
            {
                return BarangayAreaId.BasketballCourt;
            }
            return BarangayAreaId.SariSariAndKalye;
        }

        public static string GetAreaDisplayName(BarangayAreaId area)
        {
            switch (area)
            {
                case BarangayAreaId.PlayersNeighborhood: return "PLAYER'S NEIGHBORHOOD";
                case BarangayAreaId.SariSariAndKalye: return "SARI-SARI & KALYE";
                case BarangayAreaId.BasketballCourt: return "BASKETBALL COURT";
                case BarangayAreaId.OldNeighborhood: return "OLD NEIGHBORHOOD";
                case BarangayAreaId.FiestaArena: return "FIESTA TEKS ARENA";
                case BarangayAreaId.TournamentArena: return "TOURNAMENT ARENA";
                default: return "BARANGAY";
            }
        }

        public static BarangayZoneInfo GetZoneInfo(BarangayAreaId area)
        {
            for (int i = 0; i < WorldZones.Length; i++)
            {
                if (WorldZones[i].AreaId == area)
                {
                    return WorldZones[i];
                }
            }
            return WorldZones[0];
        }

        public static string GetZoneNameAtPosition(Vector3 pos)
        {
            return GetAreaDisplayName(GetAreaAtPosition(pos));
        }

        public void BuildWorldIfNeeded()
        {
            const string versionMarker = "WorldVersion_V9";
            if (_built && CourtRoot != null && CourtRoot.Find(versionMarker) != null)
            {
                // Verify materials aren't stale/missing shaders (prevents magenta screen when Reload Domain is disabled)
                var firstRend = CourtRoot.GetComponentInChildren<Renderer>();
                if (firstRend != null && firstRend.sharedMaterial != null && firstRend.sharedMaterial.shader != null &&
                    firstRend.sharedMaterial.shader.name != "Hidden/InternalErrorShader")
                {
                    RefreshAreaGates();
                    RefreshHiddenCardStates();
                    return;
                }
            }

            var existing = GameObject.Find("BarangayCourtWorld");
            if (existing != null)
            {
                SafeDestroy(existing);
            }

            _built = true;
            TournamentHostTransform = null;
            SpawnedOpponents.Clear();
            SpawnedHiddenCards.Clear();
            SpawnedAreaGates.Clear();
            StoreInteractionPoints.Clear();
            _storeTimerMeshes.Clear();
            _storeRackCardRenderers.Clear();

            var rootGo = new GameObject("BarangayCourtWorld");
            CourtRoot = rootGo.transform;
            new GameObject(versionMarker).transform.SetParent(CourtRoot, false);

            SetupLighting();
            BuildGroundAndCourt();
            BuildPerimeterChainLinkFence();
            BuildBasketballHoop("LeftHoop", new Vector3(-13.2f, 0f, 0f), faceRight: true);
            BuildBasketballHoop("RightHoop", new Vector3(13.2f, 0f, 0f), faceRight: false);
            BuildCenterCircleTeksPile();
            BuildBleachersAndShadySidelines();
            BuildExtendedBarangayZones();
            BuildTournamentArena();
            BuildAreaGates();
            BuildBarangayHousesAndUtilityPoles();
            SpawnAllOpponents();
            SpawnHiddenCollectibleCards();

            if (Application.isPlaying)
            {
                SubscribeRuntimeEvents();
            }

            RefreshStoreDisplayCards();
            RefreshAreaGates();
            RefreshHiddenCardStates();
        }

        public void RefreshAreaGates()
        {
            for (int i = 0; i < SpawnedAreaGates.Count; i++)
            {
                if (SpawnedAreaGates[i] != null)
                {
                    SpawnedAreaGates[i].RefreshLockState();
                }
            }

            var all = AreaGateBarrier.AllBarriers;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null)
                {
                    all[i].RefreshLockState();
                }
            }
        }

        public void RefreshHiddenCardStates()
        {
            for (int i = 0; i < SpawnedHiddenCards.Count; i++)
            {
                if (SpawnedHiddenCards[i] != null)
                {
                    SpawnedHiddenCards[i].RefreshCollectedState();
                }
            }
        }

        public List<string> GetHiddenCardIdsInArea(BarangayAreaId area)
        {
            var ids = new List<string>();
            for (int i = 0; i < SpawnedHiddenCards.Count; i++)
            {
                var card = SpawnedHiddenCards[i];
                if (card != null && card.AreaId == area && !string.IsNullOrEmpty(card.HiddenId))
                {
                    ids.Add(card.HiddenId);
                }
            }
            return ids;
        }

        public int GetTotalHiddenCardCount()
        {
            return SpawnedHiddenCards.Count;
        }

        private void CollectDynamicReferences()
        {
            SpawnedOpponents.Clear();
            SpawnedOpponents.AddRange(FindObjectsByType<NpcOpponent>());
            SpawnedHiddenCards.Clear();
            if (CourtRoot != null)
            {
                SpawnedHiddenCards.AddRange(CourtRoot.GetComponentsInChildren<HiddenCardCollectible>(true));
                SpawnedAreaGates.Clear();
                SpawnedAreaGates.AddRange(CourtRoot.GetComponentsInChildren<AreaGateBarrier>(true));
            }
            _storeTimerMeshes.Clear();
            if (CourtRoot != null)
            {
                foreach (var tm in CourtRoot.GetComponentsInChildren<TextMesh>())
                {
                    if (tm.gameObject.name.StartsWith("StoreTimerText"))
                    {
                        _storeTimerMeshes.Add(tm);
                    }
                }
                _storeRackCardRenderers.Clear();
                foreach (var rend in CourtRoot.GetComponentsInChildren<Renderer>())
                {
                    if (rend.gameObject.name.StartsWith("StoreRackCard_"))
                    {
                        _storeRackCardRenderers.Add(rend);
                    }
                }
            }
        }

        private void SetupLighting()
        {
            Light sun = null;
            foreach (var light in FindObjectsByType<Light>())
            {
                if (light.type == LightType.Directional)
                {
                    sun = light;
                    break;
                }
            }

            if (sun == null)
            {
                var sunGo = new GameObject("BarangaySunLight");
                sunGo.transform.SetParent(CourtRoot, false);
                sun = sunGo.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.transform.rotation = Quaternion.Euler(36f, -38f, 0f);
            sun.color = new Color(1.0f, 0.94f, 0.82f);
            sun.intensity = 2.1f;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.78f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.84f, 0.74f, 0.60f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.32f, 0.28f);
        }

        private void BuildGroundAndCourt()
        {
            // Dusty barangay road & ground apron covering the extended neighborhood & South arenas
            var groundMat = CardArtGenerator.CreateLitMaterial(new Color(0.47f, 0.44f, 0.40f), 0.1f);
            CreateBox("StreetGround", CourtRoot, new Vector3(0f, -0.06f, -12f), new Vector3(115f, 0.1f, 105f), groundMat, keepCollider: true);

            // Main weathered concrete basketball court
            var courtMat = CardArtGenerator.CreateLitMaterial(Color.white, 0.14f, 0f, CardArtGenerator.GetCourtFloorTexture());
            var courtQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            courtQuad.name = "ConcreteBasketballCourt";
            courtQuad.transform.SetParent(CourtRoot, false);
            courtQuad.transform.position = new Vector3(0f, 0.002f, 0f);
            courtQuad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            courtQuad.transform.localScale = new Vector3(28f, 16f, 1f);
            courtQuad.GetComponent<Renderer>().sharedMaterial = courtMat;
            var quadCol = courtQuad.GetComponent<Collider>();
            if (quadCol != null) SafeDestroy(quadCol);

            // Invisible physics surface collider at y=0 across ALL Barangay zones so cards land cleanly anywhere
            var floorColliderGo = new GameObject("CourtPhysicsSurface");
            floorColliderGo.transform.SetParent(CourtRoot, false);
            floorColliderGo.transform.position = new Vector3(0f, -0.05f, -12f);
            var box = floorColliderGo.AddComponent<BoxCollider>();
            box.size = new Vector3(115f, 0.1f, 105f);

            // Extended Barangay perimeter barriers (spanning z in [-38.5, +12.2])
            CreateInvisibleWall("BoundaryNorth", new Vector3(0f, 2f, 12.2f), new Vector3(72f, 5f, 0.4f));
            CreateInvisibleWall("BoundarySouth", new Vector3(0f, 2f, -38.5f), new Vector3(72f, 5f, 0.4f));
            CreateInvisibleWall("BoundaryWest", new Vector3(-31.5f, 2f, -13.15f), new Vector3(0.4f, 5f, 52f));
            CreateInvisibleWall("BoundaryEast", new Vector3(32.5f, 2f, -13.15f), new Vector3(0.4f, 5f, 52f));
        }

        private void CreateInvisibleWall(string name, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(CourtRoot, false);
            go.transform.position = pos;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
        }

        private void BuildPerimeterChainLinkFence()
        {
            var fenceGroup = new GameObject("ChainLinkFence").transform;
            fenceGroup.SetParent(CourtRoot, false);

            Material postMat = CardArtGenerator.CreateLitMaterial(new Color(0.42f, 0.42f, 0.44f), 0.35f, 0.5f);
            Material wireMat = CardArtGenerator.CreateLitMaterial(Color.white, 0.1f, 0.2f, CardArtGenerator.GetChainLinkTexture(), transparent: true);
            if (wireMat.HasProperty("_BaseMap")) wireMat.SetTextureScale("_BaseMap", new Vector2(8f, 3f));
            if (wireMat.HasProperty("_MainTex")) wireMat.SetTextureScale("_MainTex", new Vector2(8f, 3f));

            // Continuous North basketball court fence from (-14.4, 0, 8.4) to (14.4, 0, 8.4)
            BuildFenceSpan(fenceGroup, new Vector3(-14.4f, 0f, 8.4f), new Vector3(14.4f, 0f, 8.4f), 3.1f, postMat, wireMat);

            // Seal NW & NE perimeter gaps north of z = 8.4 up to BoundaryNorth (z = 12.2) so players cannot bypass locked gates
            BuildFenceSpan(fenceGroup, new Vector3(-14.4f, 0f, 8.4f), new Vector3(-14.4f, 0f, 12.2f), 3.1f, postMat, wireMat);
            BuildFenceSpan(fenceGroup, new Vector3(14.4f, 0f, 8.4f), new Vector3(14.4f, 0f, 12.2f), 3.1f, postMat, wireMat);

            // Left court fence with wide walkthrough gate (z in [-3.0, +3.0]) to Old Neighborhood (West Tambayan Alley)
            BuildFenceSpan(fenceGroup, new Vector3(-14.4f, 0f, -8.4f), new Vector3(-14.4f, 0f, -3.0f), 3.1f, postMat, wireMat);
            BuildFenceSpan(fenceGroup, new Vector3(-14.4f, 0f, 3.0f), new Vector3(-14.4f, 0f, 8.4f), 3.1f, postMat, wireMat);

            // Right court fence with wide walkthrough gate (z in [-3.0, +3.0]) to Player's Neighborhood (East Plaza)
            BuildFenceSpan(fenceGroup, new Vector3(14.4f, 0f, -8.4f), new Vector3(14.4f, 0f, -3.0f), 3.1f, postMat, wireMat);
            BuildFenceSpan(fenceGroup, new Vector3(14.4f, 0f, 3.0f), new Vector3(14.4f, 0f, 8.4f), 3.1f, postMat, wireMat);

            // South court fence along z = -8.4 with centered walkthrough gateway (x in [-3.2, +3.2]) to Sari-Sari & Kalye
            BuildFenceSpan(fenceGroup, new Vector3(-14.4f, 0f, -8.4f), new Vector3(-3.2f, 0f, -8.4f), 2.5f, postMat, wireMat);
            BuildFenceSpan(fenceGroup, new Vector3(3.2f, 0f, -8.4f), new Vector3(14.4f, 0f, -8.4f), 2.5f, postMat, wireMat);

            // Old Neighborhood South border along z = -8.4 (North of Highway, leaving Kalye West gate x in [-25.1, -18.9])
            BuildFenceSpan(fenceGroup, new Vector3(-31.5f, 0f, -8.4f), new Vector3(-25.1f, 0f, -8.4f), 2.5f, postMat, wireMat);
            BuildFenceSpan(fenceGroup, new Vector3(-18.9f, 0f, -8.4f), new Vector3(-14.4f, 0f, -8.4f), 2.5f, postMat, wireMat);

            // NOTE: Highway corridor z in [-18.5, -11.5] stays 100% CLEAR of fences and gates from West to East.
            // Starter walkway between Area 0 (PlayersNeighborhood) & Area 1 (SariSariAndKalye) at x = 14.4, z in [-11.5, -8.4] stays OPEN.

            // Fiesta Arena West border along x = 14.5 (South of Highway, z in [-38.5, -20.8])
            BuildFenceSpan(fenceGroup, new Vector3(14.5f, 0f, -38.5f), new Vector3(14.5f, 0f, -20.8f), 2.6f, postMat, wireMat);

            // Fiesta Arena North border along z = -20.8 (South of Highway, leaving single entrance gate x in [20.4, 26.6])
            BuildFenceSpan(fenceGroup, new Vector3(14.5f, 0f, -20.8f), new Vector3(20.4f, 0f, -20.8f), 2.4f, postMat, wireMat);
            BuildFenceSpan(fenceGroup, new Vector3(26.6f, 0f, -20.8f), new Vector3(32.5f, 0f, -20.8f), 2.4f, postMat, wireMat);
        }

        private void BuildFenceSpan(Transform parent, Vector3 start, Vector3 end, float height, Material postMat, Material wireMat)
        {
            Vector3 diff = end - start;
            float length = diff.magnitude;
            if (length < 0.2f) return;

            Vector3 dir = diff / length;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);

            int postCount = Mathf.Max(2, Mathf.RoundToInt(length / 3.2f) + 1);
            for (int i = 0; i < postCount; i++)
            {
                float t = (float)i / (postCount - 1);
                Vector3 p = Vector3.Lerp(start, end, t);
                CreateCylinder("FencePost", parent, p + Vector3.up * (height * 0.5f), new Vector3(0.11f, height * 0.5f, 0.11f), postMat);
            }

            Vector3 mid = (start + end) * 0.5f;
            // Solid collision volume so players cannot bypass locked area gates through fences
            var fenceColliderGo = new GameObject("FenceSpanCollider");
            fenceColliderGo.transform.SetParent(parent, false);
            fenceColliderGo.transform.position = mid + Vector3.up * (height * 0.5f);
            fenceColliderGo.transform.rotation = rot;
            var fenceBox = fenceColliderGo.AddComponent<BoxCollider>();
            fenceBox.size = new Vector3(length, height, 0.30f);

            // Top & middle rail
            var topRail = CreateBox("TopRail", parent, mid + Vector3.up * (height - 0.05f), new Vector3(length, 0.08f, 0.08f), postMat, false);
            topRail.transform.rotation = rot;
            var midRail = CreateBox("MidRail", parent, mid + Vector3.up * (height * 0.5f), new Vector3(length, 0.06f, 0.06f), postMat, false);
            midRail.transform.rotation = rot;

            // Chain link mesh panel (double-sided visual via 2 thin quads)
            var panelFront = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panelFront.name = "ChainLinkPanel";
            panelFront.transform.SetParent(parent, false);
            panelFront.transform.position = mid + Vector3.up * (height * 0.5f);
            panelFront.transform.rotation = rot;
            panelFront.transform.localScale = new Vector3(length, height - 0.1f, 1f);
            panelFront.GetComponent<Renderer>().sharedMaterial = wireMat;
            SafeDestroy(panelFront.GetComponent<Collider>());

            var panelBack = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panelBack.name = "ChainLinkPanelBack";
            panelBack.transform.SetParent(parent, false);
            panelBack.transform.position = mid + Vector3.up * (height * 0.5f);
            panelBack.transform.rotation = rot * Quaternion.Euler(0f, 180f, 0f);
            panelBack.transform.localScale = new Vector3(length, height - 0.1f, 1f);
            panelBack.GetComponent<Renderer>().sharedMaterial = wireMat;
            SafeDestroy(panelBack.GetComponent<Collider>());
        }

        private void BuildAreaGates()
        {
            var gateGroup = new GameObject("AreaGates").transform;
            gateGroup.SetParent(CourtRoot, false);

            // Area 2: Basketball Court (East & South gateways — front sign faces approaching player!)
            CreateAreaGate(
                gateGroup, "Gate_Court_East", BarangayAreaId.BasketballCourt,
                "Basketball Court", new Vector3(14.4f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), 6.0f, 2.4f);
            CreateAreaGate(
                gateGroup, "Gate_Court_South", BarangayAreaId.BasketballCourt,
                "Basketball Court", new Vector3(0f, 0f, -8.4f), Quaternion.identity, 6.4f, 2.4f);

            // Area 3: Old Neighborhood (Court West & Kalye West North-of-Highway gateways)
            CreateAreaGate(
                gateGroup, "Gate_OldNeighborhood_CourtWest", BarangayAreaId.OldNeighborhood,
                "Old Neighborhood", new Vector3(-14.4f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), 6.0f, 2.4f);
            CreateAreaGate(
                gateGroup, "Gate_OldNeighborhood_KalyeWest", BarangayAreaId.OldNeighborhood,
                "Old Neighborhood", new Vector3(-22.0f, 0f, -8.4f), Quaternion.identity, 6.2f, 2.4f);

            // Area 4: Fiesta Teks Arena (Single North entrance South of Highway — faces North toward Kalye Highway!)
            CreateAreaGate(
                gateGroup, "Gate_Fiesta_North", BarangayAreaId.FiestaArena,
                "Fiesta Teks Arena", new Vector3(23.5f, 0f, -20.8f), Quaternion.Euler(0f, 180f, 0f), 6.2f, 2.4f);
        }

        private void CreateAreaGate(
            Transform parent,
            string gateName,
            BarangayAreaId areaId,
            string gateTitle,
            Vector3 worldPos,
            Quaternion worldRot,
            float width,
            float height)
        {
            var go = new GameObject(gateName);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = worldRot;

            string hint = PlayerInventory.Instance != null
                ? PlayerInventory.Instance.GetAreaUnlockRequirementText(areaId)
                : GetZoneInfo(areaId).UnlockHint;

            var barrier = go.AddComponent<AreaGateBarrier>();
            barrier.Initialize(areaId, gateTitle, hint, width, height);
            SpawnedAreaGates.Add(barrier);
        }

        private void BuildBasketballHoop(string name, Vector3 basePos, bool faceRight)
        {
            var hoopRoot = new GameObject(name).transform;
            hoopRoot.SetParent(CourtRoot, false);
            hoopRoot.position = basePos;
            hoopRoot.rotation = Quaternion.Euler(0f, faceRight ? 90f : -90f, 0f);

            Material rustSteel = CardArtGenerator.CreateLitMaterial(new Color(0.35f, 0.25f, 0.20f), 0.25f, 0.4f);
            Material boardMat = CardArtGenerator.CreateLitMaterial(new Color(0.84f, 0.78f, 0.70f), 0.15f);
            Material rustTrim = CardArtGenerator.CreateLitMaterial(new Color(0.56f, 0.26f, 0.15f), 0.2f);
            Material rimMat = CardArtGenerator.CreateLitMaterial(new Color(0.88f, 0.30f, 0.10f), 0.35f, 0.3f);
            Material netMat = CardArtGenerator.CreateLitMaterial(new Color(0.86f, 0.86f, 0.84f), 0.1f);

            // Support post + concrete base
            CreateCylinder("HoopBase", hoopRoot, new Vector3(0f, 0.15f, -0.55f), new Vector3(0.45f, 0.15f, 0.45f), rustSteel, keepCollider: true);
            CreateCylinder("MainPole", hoopRoot, new Vector3(0f, 1.95f, -0.55f), new Vector3(0.16f, 1.95f, 0.16f), rustSteel, keepCollider: true);

            // Angled extension arm
            var arm = CreateBox("SupportArm", hoopRoot, new Vector3(0f, 3.65f, -0.05f), new Vector3(0.14f, 0.14f, 1.1f), rustSteel, false);
            arm.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);

            // Backboard + frame + shooter's square
            CreateBox("Backboard", hoopRoot, new Vector3(0f, 3.55f, 0.46f), new Vector3(1.85f, 1.18f, 0.08f), boardMat, false);
            CreateBox("BackboardBorderTop", hoopRoot, new Vector3(0f, 4.11f, 0.50f), new Vector3(1.87f, 0.06f, 0.03f), rustTrim, false);
            CreateBox("BackboardBorderBot", hoopRoot, new Vector3(0f, 2.99f, 0.50f), new Vector3(1.87f, 0.06f, 0.03f), rustTrim, false);
            CreateBox("ShooterSquare", hoopRoot, new Vector3(0f, 3.35f, 0.505f), new Vector3(0.56f, 0.42f, 0.02f), rustTrim, false);
            CreateBox("ShooterSquareInner", hoopRoot, new Vector3(0f, 3.35f, 0.51f), new Vector3(0.46f, 0.32f, 0.02f), boardMat, false);

            // Rim ring + net cords
            Vector3 rimCenter = new Vector3(0f, 3.10f, 0.92f);
            int rimSegments = 10;
            float rimRadius = 0.28f;
            for (int i = 0; i < rimSegments; i++)
            {
                float a0 = (i / (float)rimSegments) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)rimSegments) * Mathf.PI * 2f;
                Vector3 p0 = rimCenter + new Vector3(Mathf.Cos(a0) * rimRadius, 0f, Mathf.Sin(a0) * rimRadius);
                Vector3 p1 = rimCenter + new Vector3(Mathf.Cos(a1) * rimRadius, 0f, Mathf.Sin(a1) * rimRadius);
                Vector3 mid = (p0 + p1) * 0.5f;
                var seg = CreateBox("RimSeg", hoopRoot, mid, new Vector3(0.04f, 0.04f, Vector3.Distance(p0, p1) + 0.02f), rimMat, false);
                seg.transform.localRotation = Quaternion.LookRotation((p1 - p0).normalized, Vector3.up);

                // Net cord
                var cord = CreateCylinder("NetCord", hoopRoot, p0 + Vector3.down * 0.22f, new Vector3(0.015f, 0.22f, 0.015f), netMat, false);
                cord.transform.localRotation = Quaternion.Euler(Mathf.Sin(a0) * 14f, 0f, -Mathf.Cos(a0) * 14f);
            }
        }

        private void BuildCenterCircleTeksPile()
        {
            var pileRoot = new GameObject("PlayCircleTeksPile").transform;
            pileRoot.SetParent(CourtRoot, false);

            var backMat = CardArtGenerator.CreateLitMaterial(Color.white, 0.1f, 0f, CardArtGenerator.GetCardBackTexture());
            var allCards = TeksDatabase.AllCards;

            for (int i = 0; i < 18; i++)
            {
                float angle = i * 1.18f;
                float radius = 0.12f + (i % 5) * 0.17f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0.008f + (i % 3) * 0.004f, Mathf.Sin(angle) * radius);

                var cardGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cardGo.name = "ScatteredTeksCard_" + i;
                cardGo.transform.SetParent(pileRoot, false);
                cardGo.transform.position = pos;
                cardGo.transform.rotation = Quaternion.Euler(0f, (i * 47f) % 360f, 0f);
                cardGo.transform.localScale = new Vector3(0.11f, 0.004f, 0.16f);
                SafeDestroy(cardGo.GetComponent<Collider>());

                bool faceUp = i % 2 == 0;
                if (faceUp && allCards.Count > 0)
                {
                    var def = allCards[i % allCards.Count];
                    var faceTex = CardArtGenerator.GetCardPortraitTexture(def, 3);
                    cardGo.GetComponent<Renderer>().sharedMaterial = CardArtGenerator.CreateLitMaterial(Color.white, 0.25f, 0f, faceTex);
                }
                else
                {
                    cardGo.GetComponent<Renderer>().sharedMaterial = backMat;
                }
            }
        }

        private void BuildBleachersAndShadySidelines()
        {
            Material concreteMat = CardArtGenerator.CreateLitMaterial(new Color(0.58f, 0.56f, 0.52f), 0.15f);
            Material woodBenchMat = CardArtGenerator.CreateLitMaterial(new Color(0.54f, 0.36f, 0.20f), 0.2f);
            Material umbrellaMat = CardArtGenerator.CreateLitMaterial(new Color(0.16f, 0.48f, 0.72f), 0.2f);
            Material trunkMat = CardArtGenerator.CreateLitMaterial(new Color(0.42f, 0.29f, 0.19f), 0.15f);

            // Bleachers along North-West sidelines
            var bleachers = new GameObject("SidelineBleachers").transform;
            bleachers.SetParent(CourtRoot, false);
            bleachers.position = new Vector3(-5.2f, 0f, 7.2f);

            CreateBox("BleacherTier1", bleachers, new Vector3(0f, 0.22f, 0f), new Vector3(7.5f, 0.44f, 1.0f), concreteMat, keepCollider: true);
            CreateBox("BleacherTier2", bleachers, new Vector3(0f, 0.55f, 0.85f), new Vector3(7.5f, 1.1f, 0.9f), concreteMat, keepCollider: true);
            CreateBox("BleacherPlank1", bleachers, new Vector3(0f, 0.46f, -0.05f), new Vector3(7.4f, 0.06f, 0.55f), woodBenchMat, false);
            CreateBox("BleacherPlank2", bleachers, new Vector3(0f, 1.12f, 0.85f), new Vector3(7.4f, 0.06f, 0.55f), woodBenchMat, false);

            // Shady Boss Sideline Corner (South-East)
            var shady = new GameObject("ShadyBossCorner").transform;
            shady.SetParent(CourtRoot, false);
            shady.position = new Vector3(11.4f, 0f, -6.2f);

            CreateBox("ShadyBench", shady, new Vector3(0f, 0.42f, 0f), new Vector3(2.8f, 0.12f, 0.7f), woodBenchMat, keepCollider: true);
            CreateBox("BenchLegL", shady, new Vector3(-1.1f, 0.20f, 0f), new Vector3(0.18f, 0.40f, 0.6f), concreteMat, false);
            CreateBox("BenchLegR", shady, new Vector3(1.1f, 0.20f, 0f), new Vector3(0.18f, 0.40f, 0.6f), concreteMat, false);
            CreateCylinder("UmbrellaPole", shady, new Vector3(0.8f, 1.4f, 0.2f), new Vector3(0.06f, 1.4f, 0.06f), trunkMat, false);
            CreateCylinder("UmbrellaCanopy", shady, new Vector3(0.8f, 2.7f, 0.2f), new Vector3(2.8f, 0.14f, 2.8f), umbrellaMat, false);
        }

        public void RefreshStoreDisplayCards()
        {
            if (ShopManager.Instance == null) return;
            var slots = ShopManager.Instance.Slots;
            for (int i = 0; i < _storeRackCardRenderers.Count && i < slots.Count; i++)
            {
                var rend = _storeRackCardRenderers[i];
                var slot = slots[i];
                if (rend == null || slot == null || slot.Card == null) continue;

                Texture2D tex = slot.IsSoldOut
                    ? CardArtGenerator.GetCardBackTexture()
                    : CardArtGenerator.GetCardPortraitTexture(slot.Card, slot.IsSuperSlot ? 4 : 2);

                if (rend.sharedMaterial.HasProperty("_BaseMap"))
                    rend.sharedMaterial.SetTexture("_BaseMap", tex);
                if (rend.sharedMaterial.HasProperty("_MainTex"))
                    rend.sharedMaterial.SetTexture("_MainTex", tex);
            }
        }

        private void BuildExtendedBarangayZones()
        {
            var zones = new GameObject("ExtendedBarangayZones").transform;
            zones.SetParent(CourtRoot, false);

            Material asphaltMat = CardArtGenerator.CreateLitMaterial(new Color(0.24f, 0.25f, 0.27f), 0.14f);
            Material lanePaintMat = CardArtGenerator.CreateLitMaterial(new Color(0.92f, 0.88f, 0.68f), 0.12f);
            Material plazaTileMat = CardArtGenerator.CreateLitMaterial(new Color(0.72f, 0.66f, 0.56f), 0.16f);
            Material grassMat = CardArtGenerator.CreateLitMaterial(new Color(0.32f, 0.54f, 0.26f), 0.12f);
            Material woodMat = CardArtGenerator.CreateLitMaterial(new Color(0.48f, 0.30f, 0.16f), 0.20f);
            Material roofMat = CardArtGenerator.CreateLitMaterial(new Color(0.68f, 0.28f, 0.18f), 0.25f);
            Material jeepRedMat = CardArtGenerator.CreateLitMaterial(new Color(0.86f, 0.18f, 0.20f), 0.45f, 0.35f);
            Material jeepBlueMat = CardArtGenerator.CreateLitMaterial(new Color(0.14f, 0.42f, 0.86f), 0.45f, 0.35f);
            Material chromeMat = CardArtGenerator.CreateLitMaterial(new Color(0.84f, 0.86f, 0.90f), 0.70f, 0.75f);
            Material tireMat = CardArtGenerator.CreateLitMaterial(new Color(0.10f, 0.10f, 0.12f), 0.15f);
            Material cartYellowMat = CardArtGenerator.CreateLitMaterial(new Color(0.96f, 0.78f, 0.18f), 0.30f);

            // 1. SOUTH ZONE: Clear Kalye Highway (z in [-18.5, -11.5], center z = -15.0, width 7.0, length 68)
            CreateBox("KalyeAsphaltRoad", zones, new Vector3(0f, -0.008f, -15.0f), new Vector3(68f, 0.02f, 7.0f), asphaltMat, false);
            for (int i = -5; i <= 5; i++)
            {
                CreateBox("RoadDash_" + i, zones, new Vector3(i * 6.0f, 0.004f, -15.0f), new Vector3(3.0f, 0.01f, 0.22f), lanePaintMat, false);
            }

            // Parked Pinoy Jeepney in the South lane of the Kalye Asphalt Road (z in [-17.8, -15.6], leaving North lane open)
            var jeep = new GameObject("BarangayJeepney").transform;
            jeep.SetParent(zones, false);
            jeep.position = new Vector3(8.5f, 0f, -16.7f);
            jeep.rotation = Quaternion.Euler(0f, -90f, 0f);
            CreateBox("JeepLowerBody", jeep, new Vector3(0f, 0.95f, 0f), new Vector3(2.2f, 1.15f, 5.4f), jeepRedMat, true);
            CreateBox("JeepUpperStripe", jeep, new Vector3(0f, 1.58f, -0.3f), new Vector3(2.15f, 0.35f, 4.4f), jeepBlueMat, false);
            CreateBox("JeepRoof", jeep, new Vector3(0f, 2.25f, -0.35f), new Vector3(2.18f, 0.14f, 4.3f), chromeMat, false);
            CreateBox("JeepHood", jeep, new Vector3(0f, 1.05f, 2.85f), new Vector3(1.85f, 0.85f, 1.35f), chromeMat, true);
            CreateBox("JeepBumper", jeep, new Vector3(0f, 0.55f, 3.60f), new Vector3(2.25f, 0.26f, 0.25f), chromeMat, false);
            CreateCylinder("WheelFL", jeep, new Vector3(-1.12f, 0.42f, 1.85f), new Vector3(0.42f, 0.16f, 0.42f), tireMat, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            CreateCylinder("WheelFR", jeep, new Vector3(1.12f, 0.42f, 1.85f), new Vector3(0.42f, 0.16f, 0.42f), tireMat, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            CreateCylinder("WheelRL", jeep, new Vector3(-1.12f, 0.42f, -1.65f), new Vector3(0.42f, 0.16f, 0.42f), tireMat, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            CreateCylinder("WheelRR", jeep, new Vector3(1.12f, 0.42f, -1.65f), new Vector3(0.42f, 0.16f, 0.42f), tireMat, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var jeepSign = CreateWorldText("JeepSign", jeep, new Vector3(0f, 1.95f, 1.92f), "BARANGAY EXPRESS", 48, new Color(1.0f, 0.88f, 0.20f), 0.18f, 0.12f);
            jeepSign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Street Food / Fishball Cart shifted left on the Kalye North Sidewalk so Basketball Court entrance stays wide open
            var cart = new GameObject("FishballCart").transform;
            cart.SetParent(zones, false);
            cart.position = new Vector3(-12.4f, 0f, -10.0f);
            CreateBox("CartBody", cart, new Vector3(0f, 0.68f, 0f), new Vector3(1.8f, 0.95f, 1.0f), cartYellowMat, true);
            CreateBox("CartTop", cart, new Vector3(0f, 1.18f, 0f), new Vector3(1.9f, 0.08f, 1.1f), chromeMat, false);
            CreateCylinder("CartUmbrellaPole", cart, new Vector3(0f, 1.95f, 0f), new Vector3(0.06f, 1.0f, 0.06f), woodMat, false);
            CreateCylinder("CartUmbrellaTop", cart, new Vector3(0f, 2.85f, 0f), new Vector3(2.5f, 0.12f, 2.5f), jeepRedMat, false);
            CreateWorldText("FishballSign", cart, new Vector3(0f, 0.85f, -0.54f), "FISHBALL & TAHO", 48, new Color(0.35f, 0.12f, 0.05f), 0.18f, 0.11f);

            // Upgraded Kalye Sari-Sari Store Kiosk shifted left so the Basketball Court South Entrance (x in [-3.2, +3.2]) is completely open
            var kalyeKiosk = new GameObject("KalyeSariSariKiosk").transform;
            kalyeKiosk.SetParent(zones, false);
            kalyeKiosk.position = new Vector3(-7.6f, 0f, -9.8f);
            Material kioskWallMat = CardArtGenerator.CreateLitMaterial(new Color(0.50f, 0.78f, 0.66f), 0.18f);
            Material kioskSignBg = CardArtGenerator.CreateLitMaterial(new Color(0.96f, 0.86f, 0.64f), 0.15f);
            Material kioskLedBg = CardArtGenerator.CreateLitMaterial(new Color(0.10f, 0.14f, 0.16f), 0.20f);
            Material darkInterior = CardArtGenerator.CreateLitMaterial(new Color(0.12f, 0.10f, 0.09f), 0.05f);
            Material teksBannerMat = CardArtGenerator.CreateLitMaterial(new Color(0.12f, 0.36f, 0.26f), 0.20f);

            // Left counter bay (Aling Nena window) + Right display bay (4-card Teks Rack)
            CreateBox("KioskBackWall", kalyeKiosk, new Vector3(0f, 1.45f, 0.82f), new Vector3(5.6f, 2.9f, 0.22f), kioskWallMat, true);
            CreateBox("KioskLeftWall", kalyeKiosk, new Vector3(-2.70f, 1.45f, 0f), new Vector3(0.22f, 2.9f, 1.75f), kioskWallMat, true);
            CreateBox("KioskRightWall", kalyeKiosk, new Vector3(2.70f, 1.45f, 0f), new Vector3(0.22f, 2.9f, 1.75f), kioskWallMat, true);
            CreateBox("KioskInteriorBack", kalyeKiosk, new Vector3(-1.15f, 1.45f, 0.68f), new Vector3(3.0f, 2.55f, 0.05f), darkInterior, false);
            CreateBox("KioskBase", kalyeKiosk, new Vector3(-1.15f, 0.56f, -0.78f), new Vector3(3.05f, 1.12f, 0.24f), kioskWallMat, true);
            CreateBox("KioskCounter", kalyeKiosk, new Vector3(-1.15f, 1.16f, -0.86f), new Vector3(3.18f, 0.10f, 0.48f), woodMat, true);
            CreateBox("KioskRightWallFront", kalyeKiosk, new Vector3(1.52f, 1.45f, -0.78f), new Vector3(2.35f, 2.9f, 0.24f), kioskWallMat, true);
            CreateBox("KioskWoodenCrate", kalyeKiosk, new Vector3(-2.45f, 0.28f, -1.25f), new Vector3(0.62f, 0.56f, 0.52f), woodMat, true);
            var kioskRoof = CreateBox("KioskAwning", kalyeKiosk, new Vector3(0f, 3.05f, -0.18f), new Vector3(6.2f, 0.14f, 2.65f), roofMat, false);
            kioskRoof.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

            // Roof Signboard + Roof LED Timer
            CreateBox("KioskSignBg", kalyeKiosk, new Vector3(-1.25f, 3.56f, -0.95f), new Vector3(3.0f, 0.84f, 0.14f), kioskSignBg, false);
            CreateWorldText("KioskSignText", kalyeKiosk, new Vector3(-1.25f, 3.56f, -1.04f), "ALING NENA'S\nSARI-SARI STORE", 52, new Color(0.42f, 0.16f, 0.08f), 0.19f, 0.13f);
            CreateBox("KioskTimerBg", kalyeKiosk, new Vector3(1.52f, 3.56f, -0.95f), new Vector3(2.35f, 0.84f, 0.14f), kioskLedBg, false);
            var kioskTimer = CreateWorldText("StoreTimerText_Kalye", kalyeKiosk, new Vector3(1.52f, 3.56f, -1.04f), "NEW CARDS IN\n04:59", 48, new Color(0.55f, 1.0f, 0.45f), 0.18f, 0.12f);
            _storeTimerMeshes.Add(kioskTimer);

            // Right wall "TEKS CARDS" display rack + 4 rotating 3D shop cards
            CreateBox("TeksCardsHeaderBg", kalyeKiosk, new Vector3(1.52f, 2.52f, -0.93f), new Vector3(2.18f, 0.36f, 0.08f), teksBannerMat, false);
            CreateWorldText("TeksRackHeaderText", kalyeKiosk, new Vector3(1.52f, 2.52f, -0.99f), "TEKS CARDS", 48, new Color(1.0f, 0.88f, 0.25f), 0.18f, 0.12f);
            CreateBox("TeksRackBoard", kalyeKiosk, new Vector3(1.52f, 1.78f, -0.92f), new Vector3(2.18f, 0.88f, 0.06f), darkInterior, false);
            for (int i = 0; i < 4; i++)
            {
                float cardX = 0.74f + i * 0.52f;
                var cardQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cardQuad.name = "StoreRackCard_" + i;
                cardQuad.transform.SetParent(kalyeKiosk, false);
                cardQuad.transform.localPosition = new Vector3(cardX, 1.78f, -0.965f);
                cardQuad.transform.localScale = new Vector3(0.42f, 0.60f, 1f);
                SafeDestroy(cardQuad.GetComponent<Collider>());
                var rend = cardQuad.GetComponent<Renderer>();
                rend.sharedMaterial = CardArtGenerator.CreateLitMaterial(Color.white, 0.25f);
                _storeRackCardRenderers.Add(rend);
            }

            CreateBox("KioskWallTimerBg", kalyeKiosk, new Vector3(1.52f, 0.88f, -0.93f), new Vector3(2.18f, 0.72f, 0.08f), kioskLedBg, false);
            var wallTimer = CreateWorldText("StoreTimerText_Wall", kalyeKiosk, new Vector3(1.52f, 0.88f, -0.99f), "NEW CARDS IN\n04:59", 48, new Color(0.55f, 1.0f, 0.45f), 0.17f, 0.11f);
            _storeTimerMeshes.Add(wallTimer);

            Color[] kioskSnacks = {
                new Color(0.86f, 0.18f, 0.20f),
                new Color(0.96f, 0.78f, 0.18f),
                new Color(0.32f, 0.54f, 0.26f),
                new Color(0.14f, 0.42f, 0.86f)
            };
            for (int c = 0; c < 4; c++)
            {
                Material sm = CardArtGenerator.CreateLitMaterial(kioskSnacks[c % kioskSnacks.Length], 0.30f);
                CreateBox("KioskSnack_" + c, kalyeKiosk, new Vector3(-2.10f + c * 0.62f, 2.24f, -0.84f), new Vector3(0.20f, 0.24f, 0.05f), sm, false);
            }

            // Aling Nena Tindera inside the Kalye Sari-Sari Kiosk window
            var tindera = new GameObject("AlingNena_Tindera");
            tindera.transform.SetParent(kalyeKiosk, false);
            tindera.transform.localPosition = new Vector3(-1.15f, 0.12f, -0.10f);
            tindera.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            CharacterVisualBuilder.BuildPackCharacterVisual(tindera, 58);

            StoreInteractionPoint = new Vector3(-7.6f, 0f, -11.2f);
            if (!StoreInteractionPoints.Contains(StoreInteractionPoint))
            {
                StoreInteractionPoints.Add(StoreInteractionPoint);
            }

            // 2. EAST ZONE: Player's Neighborhood & Barangay Plaza Park (x = +15 .. +31, z > -11.5)
            CreateBox("EastPlazaLawn", zones, new Vector3(22.5f, -0.009f, 0f), new Vector3(14f, 0.02f, 18f), grassMat, false);
            CreateBox("EastPlazaPatio", zones, new Vector3(22.2f, 0.002f, 0f), new Vector3(10.5f, 0.02f, 12.5f), plazaTileMat, false);

            // Player's House (Starter Home Landmark in Area 0, clear of spawn (24.8, 0, 5.2) & behind-house hidden card (27.5, 0.35, 9.6))
            var playerHouse = new GameObject("PlayersHouse").transform;
            playerHouse.SetParent(zones, false);
            playerHouse.position = new Vector3(27.5f, 0f, 6.2f);
            if (SpawnHousePackModel(playerHouse, variantIndex: 2, targetWidth: 3.6f, targetDepth: 3.6f, targetMinHeight: 3.6f, targetMaxHeight: 4.4f, localYawOffset: -90f, addCollider: true))
            {
                Material homeTrimMat = CardArtGenerator.CreateLitMaterial(new Color(0.20f, 0.48f, 0.66f), 0.22f);
                var yardSign = new GameObject("PlayersHouseSign").transform;
                yardSign.SetParent(playerHouse, false);
                yardSign.localPosition = new Vector3(-2.05f, 0f, -1.35f);
                CreateCylinder("HomeSignPost", yardSign, new Vector3(0f, 0.85f, 0f), new Vector3(0.06f, 0.85f, 0.06f), woodMat, false);
                CreateBox("HomeSignBg", yardSign, new Vector3(0f, 1.58f, 0f), new Vector3(0.08f, 0.46f, 1.48f), homeTrimMat, false);
                var homeLabel = CreateWorldText("HomeSignText", yardSign, new Vector3(-0.06f, 1.58f, 0f), "MY HOUSE", 52, new Color(1.0f, 0.94f, 0.40f), 0.20f, 0.12f);
                homeLabel.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
            else
            {
                Material homeWallMat = CardArtGenerator.CreateLitMaterial(new Color(0.95f, 0.89f, 0.76f), 0.16f);
                Material homeTrimMat = CardArtGenerator.CreateLitMaterial(new Color(0.20f, 0.48f, 0.66f), 0.22f);
                Material homeDoorMat = CardArtGenerator.CreateLitMaterial(new Color(0.46f, 0.26f, 0.14f), 0.20f);
                Material porchGlowMat = CardArtGenerator.CreateUnlitEmissiveMaterial(new Color(1.0f, 0.90f, 0.58f), 1.45f);
                CreateBox("HomeBody", playerHouse, new Vector3(0f, 1.70f, 0f), new Vector3(3.4f, 3.4f, 3.4f), homeWallMat, true);
                CreateBox("HomePorchStep", playerHouse, new Vector3(-1.90f, 0.12f, 0f), new Vector3(0.55f, 0.24f, 1.85f), plazaTileMat, true);
                CreateBox("HomeDoor", playerHouse, new Vector3(-1.72f, 1.12f, 0f), new Vector3(0.08f, 1.95f, 1.05f), homeDoorMat, false);
                CreateBox("HomeWindowFront", playerHouse, new Vector3(-1.72f, 2.25f, 1.05f), new Vector3(0.08f, 0.75f, 0.75f), homeTrimMat, false);
                CreateBox("HomeWindowSouth", playerHouse, new Vector3(0f, 1.85f, -1.72f), new Vector3(1.15f, 0.85f, 0.08f), homeTrimMat, false);
                CreateBox("HomeRoof", playerHouse, new Vector3(-0.12f, 3.56f, 0f), new Vector3(4.35f, 0.24f, 4.10f), roofMat, false);
                CreateBox("HomeRoofPeak", playerHouse, new Vector3(0f, 3.92f, 0f), new Vector3(2.80f, 0.46f, 2.90f), roofMat, false);
                CreateBox("HomePorchLamp", playerHouse, new Vector3(-1.82f, 2.42f, 0f), new Vector3(0.18f, 0.18f, 0.18f), porchGlowMat, false);
                CreateBox("HomeSignBg", playerHouse, new Vector3(-1.76f, 2.95f, 0f), new Vector3(0.10f, 0.52f, 2.15f), homeTrimMat, false);
                var homeLabel = CreateWorldText("HomeSignText", playerHouse, new Vector3(-1.83f, 2.95f, 0f), "MY HOUSE", 52, new Color(1.0f, 0.94f, 0.40f), 0.20f, 0.13f);
                homeLabel.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // Plaza Bench (for bench sitter Ate Lorna)
            var plazaBench = new GameObject("PlazaBench").transform;
            plazaBench.SetParent(zones, false);
            plazaBench.position = new Vector3(25.8f, 0f, -2.6f);
            Material benchLegMat = CardArtGenerator.CreateLitMaterial(new Color(0.50f, 0.48f, 0.45f), 0.15f);
            CreateBox("PlazaBenchSeat", plazaBench, new Vector3(0f, 0.42f, 0f), new Vector3(0.70f, 0.12f, 2.55f), woodMat, true);
            CreateBox("PlazaBenchBack", plazaBench, new Vector3(0.30f, 0.80f, 0f), new Vector3(0.10f, 0.56f, 2.55f), woodMat, true);
            CreateBox("PlazaBenchLegN", plazaBench, new Vector3(0f, 0.20f, 0.95f), new Vector3(0.58f, 0.40f, 0.18f), benchLegMat, false);
            CreateBox("PlazaBenchLegS", plazaBench, new Vector3(0f, 0.20f, -0.95f), new Vector3(0.58f, 0.40f, 0.18f), benchLegMat, false);

            var kubo = new GameObject("BarangayPlazaGazebo").transform;
            kubo.SetParent(zones, false);
            kubo.position = new Vector3(23.5f, 0f, 2.2f);
            CreateBox("KuboFloor", kubo, new Vector3(0f, 0.14f, 0f), new Vector3(4.6f, 0.28f, 4.6f), woodMat, true);
            CreateCylinder("KuboPost1", kubo, new Vector3(-2.0f, 1.55f, -2.0f), new Vector3(0.14f, 1.45f, 0.14f), woodMat, true);
            CreateCylinder("KuboPost2", kubo, new Vector3(2.0f, 1.55f, -2.0f), new Vector3(0.14f, 1.45f, 0.14f), woodMat, true);
            CreateCylinder("KuboPost3", kubo, new Vector3(-2.0f, 1.55f, 2.0f), new Vector3(0.14f, 1.45f, 0.14f), woodMat, true);
            CreateCylinder("KuboPost4", kubo, new Vector3(2.0f, 1.55f, 2.0f), new Vector3(0.14f, 1.45f, 0.14f), woodMat, true);
            CreateBox("KuboRoofLower", kubo, new Vector3(0f, 3.15f, 0f), new Vector3(5.6f, 0.24f, 5.6f), roofMat, false);
            CreateBox("KuboRoofPeak", kubo, new Vector3(0f, 3.55f, 0f), new Vector3(3.4f, 0.55f, 3.4f), roofMat, false);
            Material signBoardMat = CardArtGenerator.CreateLitMaterial(new Color(0.14f, 0.30f, 0.22f), 0.16f);
            CreateBox("PlazaSignBg", kubo, new Vector3(0f, 3.28f, -2.86f), new Vector3(3.1f, 0.68f, 0.12f), signBoardMat, false);
            CreateWorldText("PlazaSign", kubo, new Vector3(0f, 3.28f, -2.94f), "BARANGAY PLAZA", 52, new Color(1.0f, 0.90f, 0.35f), 0.20f, 0.13f);

            // 3. WEST ZONE: Old Neighborhood & Waiting Shed (x = -16 .. -29, z > -8.4)
            CreateBox("WestTambayanPad", zones, new Vector3(-22.2f, 0.002f, 0f), new Vector3(13f, 0.02f, 16f), plazaTileMat, false);

            var shed = new GameObject("WestWaitingShed").transform;
            shed.SetParent(zones, false);
            shed.position = new Vector3(-23.5f, 0f, 3.5f);
            CreateBox("ShedBackWall", shed, new Vector3(0f, 1.35f, 1.2f), new Vector3(5.2f, 2.7f, 0.22f), CardArtGenerator.CreateLitMaterial(new Color(0.84f, 0.78f, 0.64f), 0.15f), true);
            CreateBox("ShedBench", shed, new Vector3(0f, 0.42f, 0.55f), new Vector3(4.6f, 0.12f, 0.75f), woodMat, true);
            CreateCylinder("ShedPostL", shed, new Vector3(-2.4f, 1.45f, -1.1f), new Vector3(0.12f, 1.45f, 0.12f), chromeMat, true);
            CreateCylinder("ShedPostR", shed, new Vector3(2.4f, 1.45f, -1.1f), new Vector3(0.12f, 1.45f, 0.12f), chromeMat, true);
            var shedRoof = CreateBox("ShedRoof", shed, new Vector3(0f, 2.85f, 0f), new Vector3(5.8f, 0.16f, 3.2f), jeepBlueMat, false);
            shedRoof.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            CreateBox("ShedSignBg", shed, new Vector3(0f, 2.98f, -1.66f), new Vector3(3.4f, 0.86f, 0.12f), signBoardMat, false);
            CreateWorldText("ShedSign", shed, new Vector3(0f, 2.98f, -1.74f), "OLD NEIGHBORHOOD\nTAMBAYAN SHED", 50, new Color(1.0f, 0.88f, 0.24f), 0.19f, 0.13f);

            // 4. SOUTH-EAST SEPARATE ZONE: Barangay Fiesta Stage & Teks Arena (South of Highway: x in [14.5, 32.0], z in [-36.5, -20.8])
            var fiesta = new GameObject("BarangayFiestaArena").transform;
            fiesta.SetParent(zones, false);
            fiesta.position = new Vector3(23.5f, 0f, -28.5f);

            CreateBox("FiestaFloorPad", fiesta, new Vector3(0f, 0.004f, 0f), new Vector3(13.5f, 0.02f, 11.5f), plazaTileMat, false);
            CreateBox("FiestaStagePlatform", fiesta, new Vector3(3.4f, 0.35f, -2.6f), new Vector3(5.4f, 0.70f, 3.6f), woodMat, true);
            CreateBox("FiestaStageBackdrop", fiesta, new Vector3(3.4f, 2.30f, -4.3f), new Vector3(5.4f, 3.2f, 0.25f), jeepBlueMat, true);
            CreateBox("FiestaStageRoof", fiesta, new Vector3(3.4f, 4.05f, -2.5f), new Vector3(6.2f, 0.22f, 4.4f), roofMat, false);
            var fiestaSign = CreateWorldText("FiestaStageSign", fiesta, new Vector3(3.4f, 3.15f, -4.12f), "BARANGAY FIESTA\nTEKS ARENA", 56, new Color(1.0f, 0.88f, 0.22f), 0.22f, 0.14f);
            fiestaSign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Overhead colorful Filipino Fiesta Banderitas (bunting flags) strung across the Fiesta Arena
            Material[] flagMats = { jeepRedMat, cartYellowMat, jeepBlueMat, grassMat };
            CreateCylinder("FlagPoleL", fiesta, new Vector3(-5.8f, 2.4f, 3.8f), new Vector3(0.10f, 2.4f, 0.10f), chromeMat, true);
            CreateCylinder("FlagPoleR", fiesta, new Vector3(5.8f, 2.4f, 3.8f), new Vector3(0.10f, 2.4f, 0.10f), chromeMat, true);
            for (int f = 0; f < 11; f++)
            {
                float t = f / 10f;
                float fx = Mathf.Lerp(-5.4f, 5.4f, t);
                float fy = 4.35f - Mathf.Sin(t * Mathf.PI) * 0.65f;
                var flag = CreateBox("Banderita_" + f, fiesta, new Vector3(fx, fy, 3.8f), new Vector3(0.42f, 0.52f, 0.05f), flagMats[f % flagMats.Length], false);
                flag.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            // Directional Double-Sided Barangay Street Signposts at Area Entrances (clear of fences, gates & roads)
            BuildDirectionSign(zones, new Vector3(-16.2f, 0f, 4.9f), Quaternion.Euler(0f, 90f, 0f), "BASKETBALL COURT\nWEST GATE", "OLD NEIGHBORHOOD\nWEST ALLEY", woodMat);
            BuildDirectionSign(zones, new Vector3(16.2f, 0f, 4.9f), Quaternion.Euler(0f, -90f, 0f), "BASKETBALL COURT\nEAST GATE", "PLAYER'S\nNEIGHBORHOOD", woodMat);
            BuildDirectionSign(zones, new Vector3(5.4f, 0f, -9.9f), Quaternion.identity, "BASKETBALL COURT\nSOUTH GATE", "KALYE, SARI-SARI &\nSOUTH ARENAS", woodMat);
            BuildDirectionSign(zones, new Vector3(17.8f, 0f, -19.6f), Quaternion.Euler(0f, 180f, 0f), "FIESTA TEKS ARENA\nSOUTH ENTRANCE", "KALYE HIGHWAY &\nMAIN COURT", woodMat);
        }

        private void BuildDirectionSign(Transform parent, Vector3 pos, Quaternion rot, string frontText, string backText, Material woodMat)
        {
            var sign = new GameObject("DirectionSign").transform;
            sign.SetParent(parent, false);
            sign.position = pos;
            sign.rotation = rot;

            CreateCylinder("SignPostL", sign, new Vector3(-0.88f, 1.08f, 0f), new Vector3(0.075f, 1.08f, 0.075f), woodMat, false);
            CreateCylinder("SignPostR", sign, new Vector3(0.88f, 1.08f, 0f), new Vector3(0.075f, 1.08f, 0.075f), woodMat, false);
            CreateBox("SignBoardTrim", sign, new Vector3(0f, 2.25f, 0f), new Vector3(2.94f, 1.04f, 0.08f), woodMat, false);
            CreateBox("SignBoard", sign, new Vector3(0f, 2.25f, 0f), new Vector3(2.80f, 0.92f, 0.11f), CardArtGenerator.CreateLitMaterial(new Color(0.14f, 0.34f, 0.22f), 0.15f), false);

            CreateWorldText("SignLabelFront", sign, new Vector3(0f, 2.25f, -0.07f), frontText, 50, new Color(1.0f, 0.92f, 0.32f), 0.19f, 0.12f);
            if (!string.IsNullOrEmpty(backText))
            {
                var backTm = CreateWorldText("SignLabelBack", sign, new Vector3(0f, 2.25f, 0.07f), backText, 50, new Color(1.0f, 0.92f, 0.32f), 0.19f, 0.12f);
                backTm.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

        private void BuildTournamentArena()
        {
            // South-West 3D Tournament Arena: x in [-31.0, -14.5], z in [-36.5, -20.8], center (-22.8, 0, -28.5)
            var arena = new GameObject("BarangayTournamentArena").transform;
            arena.SetParent(CourtRoot, false);
            arena.position = new Vector3(-22.8f, 0f, -28.5f);

            Material padMat = CardArtGenerator.CreateLitMaterial(new Color(0.24f, 0.30f, 0.42f), 0.18f);
            Material ringOuterMat = CardArtGenerator.CreateLitMaterial(new Color(0.94f, 0.72f, 0.16f), 0.35f, 0.25f);
            Material ringInnerMat = CardArtGenerator.CreateLitMaterial(new Color(0.16f, 0.22f, 0.32f), 0.16f);
            Material pillarMat = CardArtGenerator.CreateLitMaterial(new Color(0.18f, 0.24f, 0.38f), 0.25f);
            Material goldMat = CardArtGenerator.CreateLitMaterial(new Color(0.98f, 0.80f, 0.18f), 0.55f, 0.55f);
            Material silverMat = CardArtGenerator.CreateLitMaterial(new Color(0.76f, 0.80f, 0.86f), 0.50f, 0.50f);
            Material bronzeMat = CardArtGenerator.CreateLitMaterial(new Color(0.78f, 0.48f, 0.24f), 0.45f, 0.40f);
            Material trophyGlowMat = CardArtGenerator.CreateUnlitEmissiveMaterial(new Color(1.0f, 0.86f, 0.24f), 1.9f);
            Material concreteMat = CardArtGenerator.CreateLitMaterial(new Color(0.56f, 0.55f, 0.52f), 0.15f);
            Material woodMat = CardArtGenerator.CreateLitMaterial(new Color(0.52f, 0.34f, 0.18f), 0.20f);
            Material boothWallMat = CardArtGenerator.CreateLitMaterial(new Color(0.16f, 0.34f, 0.58f), 0.22f);
            Material boothAwningMat = CardArtGenerator.CreateLitMaterial(new Color(0.84f, 0.22f, 0.18f), 0.25f);
            Material signBoardMat = CardArtGenerator.CreateLitMaterial(new Color(0.10f, 0.14f, 0.24f), 0.20f);

            // Arena Floor Pad (14m x 12m)
            CreateBox("TournamentFloorPad", arena, new Vector3(0f, 0.005f, 0f), new Vector3(14.0f, 0.02f, 12.0f), padMat, false);

            // Central Battle Ring (centered at (-22.8, 0, -28.5))
            CreateBox("BattleRingOuter", arena, new Vector3(0f, 0.012f, 0f), new Vector3(5.6f, 0.02f, 5.6f), ringOuterMat, false);
            CreateBox("BattleRingInner", arena, new Vector3(0f, 0.018f, 0f), new Vector3(5.0f, 0.02f, 5.0f), ringInnerMat, false);
            CreateBox("BattleRingCenterLine", arena, new Vector3(0f, 0.024f, 0f), new Vector3(4.8f, 0.01f, 0.12f), ringOuterMat, false);

            // Low perimeter walls around SW arena (leaving North entrance open at x in [-3.1, +3.1], local z = +7.7 -> world z = -20.8)
            CreateBox("ArenaWallEast", arena, new Vector3(8.1f, 0.55f, 0f), new Vector3(0.28f, 1.1f, 15.6f), pillarMat, true);
            CreateBox("ArenaWallWest", arena, new Vector3(-8.1f, 0.55f, 0f), new Vector3(0.28f, 1.1f, 15.6f), pillarMat, true);
            CreateBox("ArenaWallSouth", arena, new Vector3(0f, 0.55f, -7.8f), new Vector3(16.4f, 1.1f, 0.28f), pillarMat, true);
            CreateBox("ArenaWallNorthL", arena, new Vector3(-5.6f, 0.55f, 7.7f), new Vector3(5.0f, 1.1f, 0.28f), pillarMat, true);
            CreateBox("ArenaWallNorthR", arena, new Vector3(5.6f, 0.55f, 7.7f), new Vector3(5.0f, 1.1f, 0.28f), pillarMat, true);

            // Entrance Archway at world (-22.8, 0, -20.8) -> local (0, 0, +7.7)
            var arch = new GameObject("TournamentEntranceArchway").transform;
            arch.SetParent(arena, false);
            arch.localPosition = new Vector3(0f, 0f, 7.7f);
            CreateBox("ArchPillarL", arch, new Vector3(-3.1f, 2.0f, 0f), new Vector3(0.48f, 4.0f, 0.48f), pillarMat, true);
            CreateBox("ArchPillarR", arch, new Vector3(3.1f, 2.0f, 0f), new Vector3(0.48f, 4.0f, 0.48f), pillarMat, true);
            CreateBox("ArchHeaderFrame", arch, new Vector3(0f, 3.85f, 0f), new Vector3(6.8f, 1.15f, 0.22f), goldMat, false);
            CreateBox("ArchHeaderBoard", arch, new Vector3(0f, 3.85f, 0f), new Vector3(6.4f, 0.96f, 0.26f), signBoardMat, false);
            var archSignFront = CreateWorldText("TournamentArchSignFront", arch, new Vector3(0f, 3.85f, 0.16f), "TOURNAMENT ARENA", 56, new Color(1.0f, 0.88f, 0.24f), 0.22f, 0.15f);
            archSignFront.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // West Bleachers (local x = -6.4, facing East)
            var westBleachers = new GameObject("TournamentBleachersWest").transform;
            westBleachers.SetParent(arena, false);
            westBleachers.localPosition = new Vector3(-6.4f, 0f, 0f);
            CreateBox("WestTier1", westBleachers, new Vector3(0.45f, 0.22f, 0f), new Vector3(0.95f, 0.44f, 8.6f), concreteMat, true);
            CreateBox("WestTier2", westBleachers, new Vector3(-0.45f, 0.55f, 0f), new Vector3(0.95f, 1.10f, 8.6f), concreteMat, true);
            CreateBox("WestPlank1", westBleachers, new Vector3(0.45f, 0.46f, 0f), new Vector3(0.55f, 0.06f, 8.5f), woodMat, false);
            CreateBox("WestPlank2", westBleachers, new Vector3(-0.45f, 1.12f, 0f), new Vector3(0.55f, 0.06f, 8.5f), woodMat, false);

            // South Bleachers (local z = -6.2, facing North)
            var southBleachers = new GameObject("TournamentBleachersSouth").transform;
            southBleachers.SetParent(arena, false);
            southBleachers.localPosition = new Vector3(0f, 0f, -6.2f);
            CreateBox("SouthTier1", southBleachers, new Vector3(0f, 0.22f, 0.45f), new Vector3(9.0f, 0.44f, 0.95f), concreteMat, true);
            CreateBox("SouthTier2", southBleachers, new Vector3(0f, 0.55f, -0.45f), new Vector3(9.0f, 1.10f, 0.95f), concreteMat, true);
            CreateBox("SouthPlank1", southBleachers, new Vector3(0f, 0.46f, 0.45f), new Vector3(8.9f, 0.06f, 0.55f), woodMat, false);
            CreateBox("SouthPlank2", southBleachers, new Vector3(0f, 1.12f, -0.45f), new Vector3(8.9f, 0.06f, 0.55f), woodMat, false);

            // Golden Trophy Podium on East side of Arena (local x = +6.0, z = 0)
            var podium = new GameObject("TournamentTrophyPodium").transform;
            podium.SetParent(arena, false);
            podium.localPosition = new Vector3(6.0f, 0f, 0f);
            CreateBox("Podium1st", podium, new Vector3(0f, 0.55f, 0f), new Vector3(1.4f, 1.10f, 1.6f), goldMat, true);
            CreateBox("Podium2nd", podium, new Vector3(0f, 0.38f, 1.55f), new Vector3(1.3f, 0.76f, 1.3f), silverMat, true);
            CreateBox("Podium3rd", podium, new Vector3(0f, 0.26f, -1.55f), new Vector3(1.3f, 0.52f, 1.3f), bronzeMat, true);
            CreateCylinder("TrophyBase", podium, new Vector3(0f, 1.20f, 0f), new Vector3(0.36f, 0.10f, 0.36f), goldMat, false);
            CreateCylinder("TrophyStem", podium, new Vector3(0f, 1.48f, 0f), new Vector3(0.12f, 0.22f, 0.12f), goldMat, false);
            CreateCylinder("TrophyCup", podium, new Vector3(0f, 1.88f, 0f), new Vector3(0.42f, 0.28f, 0.42f), trophyGlowMat, false);
            var podiumSign = CreateWorldText("TrophyPodiumSign", podium, new Vector3(-0.74f, 0.58f, 0f), "1ST PRIZE\n600 COINS", 48, new Color(0.14f, 0.10f, 0.04f), 0.17f, 0.11f);
            podiumSign.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // Registration Booth + Host NPC ("Kuya Ref") at world (-22.8, 0, -23.0) -> local (0, 0, +5.5)
            var booth = new GameObject("TournamentRegistrationBooth").transform;
            booth.SetParent(arena, false);
            booth.localPosition = new Vector3(0f, 0f, 5.5f);
            CreateBox("BoothDesk", booth, new Vector3(0f, 0.55f, 0.35f), new Vector3(2.8f, 1.10f, 0.75f), boothWallMat, true);
            CreateBox("BoothCounterTop", booth, new Vector3(0f, 1.13f, 0.38f), new Vector3(3.0f, 0.08f, 0.90f), woodMat, true);
            CreateBox("BoothPostL", booth, new Vector3(-1.35f, 1.45f, 0.35f), new Vector3(0.14f, 2.90f, 0.14f), goldMat, false);
            CreateBox("BoothPostR", booth, new Vector3(1.35f, 1.45f, 0.35f), new Vector3(0.14f, 2.90f, 0.14f), goldMat, false);
            var boothRoof = CreateBox("BoothAwning", booth, new Vector3(0f, 2.88f, 0.25f), new Vector3(3.3f, 0.14f, 1.75f), boothAwningMat, false);
            boothRoof.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            CreateBox("BoothSignBg", booth, new Vector3(0f, 3.30f, 0.45f), new Vector3(3.0f, 0.76f, 0.12f), signBoardMat, false);
            var boothSignFront = CreateWorldText("BoothSignFront", booth, new Vector3(0f, 3.30f, 0.53f), "KUYA REF BOOTH\n[E] 3-ROUND CUP", 48, new Color(1.0f, 0.90f, 0.30f), 0.18f, 0.12f);
            boothSignFront.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Host NPC ("Kuya Ref") standing behind the registration desk facing North
            var kuyaRef = new GameObject("KuyaRef_TournamentHost");
            kuyaRef.transform.SetParent(booth, false);
            kuyaRef.transform.localPosition = new Vector3(0f, 0f, -0.35f);
            kuyaRef.transform.localRotation = Quaternion.identity;
            TournamentHostTransform = kuyaRef.transform;
            CharacterVisualBuilder.BuildPackCharacterVisual(kuyaRef, 40);

            TournamentInteractionPoint = new Vector3(-22.8f, 0f, -22.2f);
            TournamentBattleCenter = new Vector3(-22.8f, 0f, -28.5f);
        }

        private void BuildBarangayHousesAndUtilityPoles()
        {
            var backdrop = new GameObject("BarangayBackdrop").transform;
            backdrop.SetParent(CourtRoot, false);

            int houseIndex = 0;

            // North Perimeter Houses (yaw = 0 -> front faces South toward court, positioned cleanly north of BoundaryNorth z = 12.2)
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(-29.2f, 0f, 15.0f), 0f, false, variantOverride: 1, targetWidth: 5.8f, targetDepth: 3.8f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(-23.0f, 0f, 15.0f), 0f, false, variantOverride: 0, targetWidth: 4.8f, targetDepth: 4.4f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(-16.0f, 0f, 15.0f), 0f, false, variantOverride: 2, targetWidth: 4.4f, targetDepth: 4.4f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(-9.5f, 0f, 15.2f), 0f, false, variantOverride: 0, targetWidth: 4.8f, targetDepth: 4.4f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(-2.5f, 0f, 15.0f), 0f, false, variantOverride: 2, targetWidth: 4.4f, targetDepth: 4.4f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(3.2f, 0f, 15.8f), 0f, false, variantOverride: 0, targetWidth: 4.8f, targetDepth: 4.4f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(9.8f, 0f, 15.0f), 0f, false, variantOverride: 2, targetWidth: 4.4f, targetDepth: 4.4f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(16.8f, 0f, 15.0f), 0f, false, variantOverride: 0, targetWidth: 4.8f, targetDepth: 4.4f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(23.4f, 0f, 15.0f), 0f, false, variantOverride: 2, targetWidth: 4.4f, targetDepth: 4.4f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(29.8f, 0f, 15.0f), 0f, false, variantOverride: 1, targetWidth: 5.8f, targetDepth: 3.8f);

            // West & East Side Perimeter Houses (cleanly outside BoundaryWest = -31.5 and BoundaryEast = 32.5)
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(-34.2f, 0f, 4.5f), -90f, false, variantOverride: 1, targetWidth: 6.8f, targetDepth: 4.0f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(-34.2f, 0f, -5.5f), -90f, false, variantOverride: 0, targetWidth: 5.2f, targetDepth: 4.2f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(35.0f, 0f, 3.0f), 90f, false, variantOverride: 0, targetWidth: 5.2f, targetDepth: 4.2f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(35.0f, 0f, -7.0f), 90f, false, variantOverride: 1, targetWidth: 6.8f, targetDepth: 4.0f);

            // South Highway Residential Row (yaw = 180 -> front doors face North toward Kalye Highway, solid BoxColliders)
            Vector3[] southHighwayRow = {
                new Vector3(-11.2f, 0f, -23.6f),
                new Vector3(-5.2f, 0f, -24.0f),
                new Vector3(0.8f, 0f, -23.6f),
                new Vector3(6.8f, 0f, -24.0f),
                new Vector3(11.8f, 0f, -23.6f)
            };
            for (int i = 0; i < southHighwayRow.Length; i++)
            {
                int variant = (i % 2 == 0) ? 0 : 2;
                float w = (variant == 0) ? 4.5f : 4.2f;
                BuildBarangayHouse(backdrop, houseIndex++, southHighwayRow[i], 180f, true, variantOverride: variant, targetWidth: w, targetDepth: 4.3f);
            }

            // South Backdrop Row (yaw = 180, non-colliding depth skyline showcasing all 3 house models)
            Vector3[] southBackdropRow = {
                new Vector3(-9.5f, 0f, -31.8f),
                new Vector3(-2.5f, 0f, -32.2f),
                new Vector3(4.5f, 0f, -31.8f),
                new Vector3(10.5f, 0f, -32.2f)
            };
            int[] southBackVariants = { 1, 0, 2, 1 };
            for (int i = 0; i < southBackdropRow.Length; i++)
            {
                int v = southBackVariants[i % southBackVariants.Length];
                float w = (v == 1) ? 5.6f : 4.6f;
                BuildBarangayHouse(backdrop, houseIndex++, southBackdropRow[i], 180f, false, variantOverride: v, targetWidth: w, targetDepth: 4.2f);
            }

            // West & East South Perimeter Extensions
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(-34.2f, 0f, -23.5f), -90f, false, variantOverride: 2, targetWidth: 4.6f, targetDepth: 4.2f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(-34.2f, 0f, -31.5f), -90f, false, variantOverride: 0, targetWidth: 5.2f, targetDepth: 4.2f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(35.2f, 0f, -23.5f), 90f, false, variantOverride: 2, targetWidth: 4.6f, targetDepth: 4.2f);
            BuildBarangayHouse(backdrop, houseIndex++, new Vector3(35.2f, 0f, -31.5f), 90f, false, variantOverride: 1, targetWidth: 6.4f, targetDepth: 4.0f);

            // Wooden Meralco Utility Poles & Overhead Power Lines
            Material poleMat = CardArtGenerator.CreateLitMaterial(new Color(0.36f, 0.25f, 0.16f), 0.15f);
            Material wireMat = CardArtGenerator.CreateLitMaterial(new Color(0.10f, 0.10f, 0.12f), 0.2f);

            Vector3 poleA = new Vector3(-6.8f, 0f, 10.6f);
            Vector3 poleB = new Vector3(4.5f, 0f, 10.6f);
            Vector3 poleC = new Vector3(15.2f, 0f, 9.8f);

            BuildUtilityPole(backdrop, poleA, poleMat);
            BuildUtilityPole(backdrop, poleB, poleMat);
            BuildUtilityPole(backdrop, poleC, poleMat);

            BuildSaggingWire(backdrop, poleA + Vector3.up * 7.8f, poleB + Vector3.up * 7.8f, wireMat);
            BuildSaggingWire(backdrop, poleA + Vector3.up * 7.2f, poleB + Vector3.up * 7.2f, wireMat);
            BuildSaggingWire(backdrop, poleB + Vector3.up * 7.8f, poleC + Vector3.up * 7.8f, wireMat);

            // Banana / Palm Trees around the perimeter
            Material leafMat = CardArtGenerator.CreateLitMaterial(new Color(0.24f, 0.56f, 0.22f), 0.2f);
            Vector3[] treePositions = {
                new Vector3(7.5f, 0f, 11.4f),
                new Vector3(11.5f, 0f, 11.2f),
                new Vector3(-15.8f, 0f, 10.5f),
                new Vector3(16.2f, 0f, -2.5f)
            };

            for (int i = 0; i < treePositions.Length; i++)
            {
                var tree = new GameObject("TropicalTree_" + i).transform;
                tree.SetParent(backdrop, false);
                tree.position = treePositions[i];
                CreateCylinder("Trunk", tree, new Vector3(0f, 2.6f, 0f), new Vector3(0.22f, 2.6f, 0.22f), poleMat, false);

                for (int f = 0; f < 6; f++)
                {
                    float ang = f * 60f;
                    var frond = CreateBox("PalmFrond", tree, new Vector3(0f, 5.1f, 0f), new Vector3(0.65f, 0.08f, 2.6f), leafMat, false);
                    frond.transform.localRotation = Quaternion.Euler(22f, ang, 0f);
                    frond.transform.localPosition += frond.transform.forward * 1.15f;
                }
            }
        }

        private void BuildBarangayHouse(
            Transform parent,
            int index,
            Vector3 pos,
            float yaw,
            bool keepCollider,
            int? variantOverride = null,
            float targetWidth = 4.8f,
            float targetDepth = 4.4f)
        {
            var house = new GameObject("BarangayHouse_" + index).transform;
            house.SetParent(parent, false);
            house.position = pos;
            house.rotation = Quaternion.Euler(0f, yaw, 0f);

            int variant = variantOverride ?? (index % 3);
            if (SpawnHousePackModel(house, variant, targetWidth, targetDepth, targetMinHeight: 3.6f, targetMaxHeight: 5.4f, localYawOffset: 180f, addCollider: keepCollider))
            {
                return;
            }

            // Defensive procedural fallback if house catalog asset has not been imported yet
            float hHeight = 4.2f + (index % 3) * 1.2f;
            float hWidth = 4.6f + (index % 2) * 0.9f;
            Material wallMat = CardArtGenerator.CreateLitMaterial(new Color(0.84f, 0.72f, 0.54f), 0.1f);
            Material tinRoofMat = CardArtGenerator.CreateLitMaterial(new Color(0.56f, 0.38f, 0.30f), 0.25f, 0.25f);
            Material windowMat = CardArtGenerator.CreateLitMaterial(new Color(0.18f, 0.22f, 0.28f), 0.4f);

            CreateBox("Walls", house, new Vector3(0f, hHeight * 0.5f, 0f), new Vector3(hWidth, hHeight, 4.4f), wallMat, keepCollider);
            var roof = CreateBox("Roof", house, new Vector3(0f, hHeight + 0.3f, 0f), new Vector3(hWidth + 0.7f, 0.25f, 5.1f), tinRoofMat, false);
            roof.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            CreateBox("Win1", house, new Vector3(-1.1f, hHeight * 0.65f, -2.23f), new Vector3(0.95f, 0.85f, 0.08f), windowMat, false);
            CreateBox("Win2", house, new Vector3(1.1f, hHeight * 0.65f, -2.23f), new Vector3(0.95f, 0.85f, 0.08f), windowMat, false);
        }

        private bool SpawnHousePackModel(
            Transform parent,
            int variantIndex,
            float targetWidth,
            float targetDepth,
            float targetMinHeight,
            float targetMaxHeight,
            float localYawOffset,
            bool addCollider)
        {
            GameObject prefab = TeksHouseCatalog.LoadPrefab(variantIndex);
            if (prefab == null) return false;

            var inst = Instantiate(prefab, parent, false);
            inst.name = prefab.name + "_Model";

            var colliders = inst.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                SafeDestroy(colliders[i]);
            }

            ConvertHouseMaterialsToUrp(inst);

            var mf = inst.GetComponentInChildren<MeshFilter>(true);
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds rawBounds = mf.sharedMesh.bounds;
                Vector3 rawSize = rawBounds.size;

                bool rotatedSideways = Mathf.Abs(Mathf.Sin(localYawOffset * Mathf.Deg2Rad)) > 0.5f;
                float rawHorizForWidth = Mathf.Max(0.1f, rotatedSideways ? rawSize.z : rawSize.x);
                float rawHorizForDepth = Mathf.Max(0.1f, rotatedSideways ? rawSize.x : rawSize.z);
                float uniformHoriz = Mathf.Min(targetWidth / rawHorizForWidth, targetDepth / rawHorizForDepth);

                float rawHeight = Mathf.Max(0.1f, rawSize.y);
                float desiredHeight = Mathf.Clamp(rawHeight * uniformHoriz, targetMinHeight, targetMaxHeight);
                float sy = Mathf.Clamp(desiredHeight / rawHeight, uniformHoriz * 0.78f, uniformHoriz * 1.32f);

                Vector3 scaleVec = new Vector3(uniformHoriz, sy, uniformHoriz);
                Quaternion rot = Quaternion.Euler(0f, localYawOffset, 0f);
                inst.transform.localScale = scaleVec;
                inst.transform.localRotation = rot;

                Vector3 rawBottomCenter = new Vector3(rawBounds.center.x, rawBounds.min.y, rawBounds.center.z);
                Vector3 scaledOffset = Vector3.Scale(rawBottomCenter, scaleVec);
                inst.transform.localPosition = -(rot * scaledOffset);

                if (addCollider)
                {
                    Vector3 scaledSize = Vector3.Scale(rawSize, scaleVec);
                    float rad = localYawOffset * Mathf.Deg2Rad;
                    float absCos = Mathf.Abs(Mathf.Cos(rad));
                    float absSin = Mathf.Abs(Mathf.Sin(rad));
                    float colX = scaledSize.x * absCos + scaledSize.z * absSin;
                    float colZ = scaledSize.x * absSin + scaledSize.z * absCos;

                    var box = parent.gameObject.AddComponent<BoxCollider>();
                    box.center = new Vector3(0f, scaledSize.y * 0.5f, 0f);
                    box.size = new Vector3(colX, scaledSize.y, colZ);
                }
            }

            return true;
        }

        private static void ConvertHouseMaterialsToUrp(GameObject houseInstance)
        {
            if (houseInstance == null) return;
            var renderers = houseInstance.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                var rend = renderers[r];
                if (rend == null) continue;
                var srcMats = rend.sharedMaterials;
                if (srcMats == null || srcMats.Length == 0) continue;

                var dstMats = new Material[srcMats.Length];
                for (int m = 0; m < srcMats.Length; m++)
                {
                    dstMats[m] = GetOrCreateUrpHouseMaterial(srcMats[m]);
                }
                rend.sharedMaterials = dstMats;
            }
        }

        private static Material GetOrCreateUrpHouseMaterial(Material srcMat)
        {
            string cleanName = srcMat != null
                ? srcMat.name.Replace(" (Instance)", string.Empty).Trim()
                : "Default";

            if (_houseUrpMatCache.TryGetValue(cleanName, out var cached) &&
                cached != null &&
                cached.shader != null &&
                cached.shader.name != "Hidden/InternalErrorShader" &&
                (!Application.isPlaying || (cached.hideFlags & HideFlags.DontSaveInBuild) == 0))
            {
                return cached;
            }

            bool isMirror = string.Equals(cleanName, "Mirror", System.StringComparison.OrdinalIgnoreCase);
            Color baseColor = ResolveHousePaletteColor(cleanName, srcMat, isMirror);
            float smoothness = isMirror ? 0.78f : 0.16f;
            float metallic = isMirror ? 0.25f : 0.0f;

            var urpMat = CardArtGenerator.CreateLitMaterial(baseColor, smoothness, metallic);
            urpMat.name = "URP_House_" + cleanName;
            _houseUrpMatCache[cleanName] = urpMat;
            return urpMat;
        }

        private static Color ResolveHousePaletteColor(string cleanName, Material srcMat, bool isMirror)
        {
            if (isMirror)
            {
                // Reflective architectural window glass instead of opaque flat black
                return new Color(0.16f, 0.30f, 0.46f, 1f);
            }

            if (srcMat != null)
            {
                if (srcMat.HasProperty("_BaseColor")) return srcMat.GetColor("_BaseColor");
                if (srcMat.HasProperty("_Color")) return srcMat.GetColor("_Color");
            }

            return cleanName switch
            {
                "Black" => new Color(0.05f, 0.05f, 0.06f, 1f),
                "Blue" => new Color(0.0f, 0.18f, 0.96f, 1f),
                "Brown" => new Color(0.38f, 0.08f, 0.06f, 1f),
                "Gray" => new Color(0.24f, 0.24f, 0.26f, 1f),
                "Light_black" => new Color(0.11f, 0.12f, 0.14f, 1f),
                "Light_brown" => new Color(0.45f, 0.28f, 0.26f, 1f),
                "Light_gray" => new Color(0.54f, 0.54f, 0.56f, 1f),
                "Orange" => new Color(1.0f, 0.48f, 0.04f, 1f),
                "Orange_2" => new Color(1.0f, 0.69f, 0.26f, 1f),
                "Red" => new Color(0.95f, 0.08f, 0.14f, 1f),
                "Red_2" => new Color(0.76f, 0.08f, 0.08f, 1f),
                "White" => new Color(0.96f, 0.96f, 0.96f, 1f),
                "White_2" => new Color(0.84f, 0.84f, 0.84f, 1f),
                "Yellow" => new Color(1.0f, 0.72f, 0.08f, 1f),
                _ => new Color(0.80f, 0.76f, 0.70f, 1f)
            };
        }

        private void BuildUtilityPole(Transform parent, Vector3 pos, Material poleMat)
        {
            CreateCylinder("UtilityPole", parent, pos + Vector3.up * 4.2f, new Vector3(0.18f, 4.2f, 0.18f), poleMat, false);
            CreateBox("CrossArm", parent, pos + Vector3.up * 7.8f, new Vector3(1.8f, 0.14f, 0.14f), poleMat, false);
            CreateBox("CrossArmLow", parent, pos + Vector3.up * 7.2f, new Vector3(1.5f, 0.12f, 0.12f), poleMat, false);
        }

        private void BuildSaggingWire(Transform parent, Vector3 a, Vector3 b, Material wireMat)
        {
            const int segs = 8;
            for (int i = 0; i < segs; i++)
            {
                float t0 = i / (float)segs;
                float t1 = (i + 1) / (float)segs;
                Vector3 p0 = Vector3.Lerp(a, b, t0) + Vector3.down * (Mathf.Sin(t0 * Mathf.PI) * 0.55f);
                Vector3 p1 = Vector3.Lerp(a, b, t1) + Vector3.down * (Mathf.Sin(t1 * Mathf.PI) * 0.55f);
                Vector3 mid = (p0 + p1) * 0.5f;
                var seg = CreateBox("PowerLine", parent, mid, new Vector3(0.03f, 0.03f, Vector3.Distance(p0, p1)), wireMat, false);
                seg.transform.rotation = Quaternion.LookRotation((p1 - p0).normalized, Vector3.up);
            }
        }

        private void SpawnAllOpponents()
        {
            SpawnedOpponents.Clear();

            // 0. NAMED STORY NPCs (5 Story Progression Opponents — Dodong MUST be SpawnedOpponents[0])
            // Story 1: Dodong (Easy) — Player's Neighborhood (clear of KuboFloor at 23.5 ± 2.3)
            var dodong = CreateOpponent("Dodong", AIDifficulty.Easy, "bantay_kalye", 1, 40,
                new Vector3(20.4f, 0f, 2.6f), Quaternion.Euler(0f, 35f, 0f),
                7,
                isSeated: false, isWandering: false, isBenchSitter: false,
                npcId: "dodong", areaOverride: BarangayAreaId.PlayersNeighborhood, isStory: true, storyOrder: 1);
            dodong.ChallengeQuote = "Uy kapitbahay! Warm-up Teks muna tayo bago ka pumunta ng Court!";
            dodong.AmbientCallouts = new[] { "Tara kapitbahay, 1v1 Teks!", "Easy match muna tayo!", "Talunin mo kami ni Mark para buksan ang Court!" };
            dodong.WinBanter = "Haha! Sabi sa'yo swerte 'tong Bantay Kalye ko eh!";
            dodong.LoseBanter = "Ayos! Galing mo na — puntahan mo naman si Mark sa Kalye!";

            // Story 2: Mark (Easy) — Sari-Sari & Kalye (North Sidewalk beside Kalye Kiosk, clear of Court Entrance)
            var mark = CreateOpponent("Mark", AIDifficulty.Easy, "tumbang_striker", 2, 60,
                new Vector3(-3.8f, 0f, -10.8f), Quaternion.Euler(0f, 165f, 0f),
                27,
                isSeated: false, isWandering: false, isBenchSitter: false,
                npcId: "mark", areaOverride: BarangayAreaId.SariSariAndKalye, isStory: true, storyOrder: 2);
            mark.ChallengeQuote = "Dito sa Kalye Kiosk ako nagte-Teks! Ready ka na ba sa Tumbang Striker ko?";
            mark.AmbientCallouts = new[] { "Teks tayo dito sa Kalye!", "Bili ka muna ng packs sa Kiosk!", "Talunin mo ako para buksan ang Basketball Court!" };
            mark.WinBanter = "Hehe! Subukan mo ulit, walang atrasan!";
            mark.LoseBanter = "Grabe solid ng flick mo! Bukas na pinto papuntang Basketball Court!";

            // Story 3: Carlo (Medium) — Basketball Court (replaces old East Plaza AI Carlo)
            var carlo = CreateOpponent("Carlo", AIDifficulty.Medium, "agila_kid", 3, 140,
                new Vector3(4.8f, 0f, -4.8f), Quaternion.Euler(0f, -35f, 0f),
                35,
                isSeated: false, isWandering: true, isBenchSitter: false,
                npcId: "carlo", areaOverride: BarangayAreaId.BasketballCourt, isStory: true, storyOrder: 3);
            carlo.ChallengeQuote = "Welcome sa Basketball Court! Talunin mo Agila Kid ko para makapasok sa Old Neighborhood!";
            carlo.AmbientCallouts = new[] { "Court Ace Carlo 'to!", "Subukan mo Agila Kid ko!", "Talunin mo ako para sa Old Neighborhood!" };
            carlo.WinBanter = "Nice try! Malakas talaga ang Court rotation ko!";
            carlo.LoseBanter = "Galing! Handa ka na kay Joshua sa Old Neighborhood!";

            // Story 4: Joshua (Hard) — Old Neighborhood
            var joshua = CreateOpponent("Joshua", AIDifficulty.Hard, "panday_blade", 4, 260,
                new Vector3(-21.2f, 0f, -3.4f), Quaternion.Euler(0f, 65f, 0f),
                25,
                isSeated: false, isWandering: false, isBenchSitter: false,
                npcId: "joshua", areaOverride: BarangayAreaId.OldNeighborhood, isStory: true, storyOrder: 4);
            joshua.ChallengeQuote = "Bihira ang nakakarating dito sa Old Neighborhood! Harapin mo Panday Blade ko!";
            joshua.AmbientCallouts = new[] { "Old Neighborhood Veteran ako!", "Hard difficulty duel — palag?", "Talunin mo ako para buksan ang Fiesta Arena!" };
            joshua.WinBanter = "Matibay pa rin ang Old Neighborhood! Balik ka pag handa ka na!";
            joshua.LoseBanter = "Respeto! Bukas na ang Fiesta Teks Arena — naghihintay si Teks Master!";

            // Story 5: Teks Master (Extreme Champion) — Fiesta Teks Arena (South of Highway, Δz = -10.0)
            var teksMaster = CreateOpponent("Teks Master", AIDifficulty.Extreme, "adarna_phoenix", 5, 500,
                new Vector3(26.4f, 0f, -27.4f), Quaternion.Euler(0f, -80f, 0f),
                42,
                isSeated: false, isWandering: false, isBenchSitter: false,
                npcId: "teks_master", areaOverride: BarangayAreaId.FiestaArena, isStory: true, isChampion: true, storyOrder: 5);
            teksMaster.ChallengeQuote = "Ako ang Barangay Teks Master! Patunayan mong karapat-dapat ka sa korona ng Fiesta Arena!";
            teksMaster.AmbientCallouts = new[] { "BARANGAY TEKS MASTER!", "Extreme Champion Challenge!", "Harapin ang Adarna Solaris!" };
            teksMaster.WinBanter = "Isang tunay na Master lang ang makakatalo sa Adarna Solaris!";
            teksMaster.LoseBanter = "MABUHAY! Ikaw na ang tunay na TEKS MASTER ng buong Barangay!!";

            // 1. Free-throw zone standing opponent (matches AI Gelo (Med) in image_0.png)
            var gelo = CreateOpponent("AI Gelo", AIDifficulty.Medium, "kid_kidlat", 3, 120,
                new Vector3(-5.8f, 0f, -1.2f), Quaternion.Euler(0f, 105f, 0f),
                5,
                isSeated: false, isWandering: false);
            gelo.ChallengeQuote = "Hoy! Teks tayo! Kaya mo ba Kid Kidlat ko?";
            gelo.AmbientCallouts = new[] { "Hoy! Laban tayo Teks!", "Pusta 120 Coins, tara!", "Chub lagi Pamato ko!" };
            gelo.WinBanter = "Haha! Sabi sa'yo Chub akin eh!";
            gelo.LoseBanter = "Aray! Ang lakas ng flick mo, rematch!";

            // 2. Play Circle seated kids around center (0,0,0)
            var kiko = CreateOpponent("AI Kiko", AIDifficulty.Medium, "tikbalang_striker", 3, 100,
                new Vector3(-1.35f, 0f, 0.25f), Quaternion.Euler(0f, 95f, 0f),
                32,
                isSeated: true, isWandering: false);
            kiko.ChallengeQuote = "Upo ka dito sa bilog! Subukan mo Tikbalang ko!";
            kiko.AmbientCallouts = new[] { "Tara sa play circle!", "Sino may pusta dyan?", "Laban tayo 1v1!" };

            var may = CreateOpponent("AI May", AIDifficulty.Medium, "sarimanok", 3, 110,
                new Vector3(0.75f, 0f, 1.35f), Quaternion.Euler(0f, 200f, 0f),
                52,
                isSeated: true, isWandering: false);
            may.ChallengeQuote = "Swerte ang Sarimanok ko — laban ka ba?";
            may.AmbientCallouts = new[] { "Gusto mo mag-Teks?", "Bago kong Sarimanok 'to!", "Pusta na!" };

            var sarah = CreateOpponent("AI Sarah", AIDifficulty.Easy, "daragang_mandirigma", 2, 60,
                new Vector3(1.25f, 0f, -0.95f), Quaternion.Euler(0f, -45f, 0f),
                54,
                isSeated: true, isWandering: false);
            sarah.ChallengeQuote = "Practice tayo! Friendly Teks lang, 60 Coins!";
            sarah.AmbientCallouts = new[] { "Laro tayo Teks!", "Friendly match lang!", "Tara, flick na!" };

            // Spectator kids seated on circle (aesthetic crowd matching image_0.png)
            CreateSpectatorKid("Spectator_Boy", new Vector3(-0.35f, 0f, 1.55f), Quaternion.Euler(0f, 170f, 0f), 10);
            CreateSpectatorKid("Spectator_Girl", new Vector3(1.65f, 0f, 0.25f), Quaternion.Euler(0f, -95f, 0f), 57);

            // 3. Storefront / Right Court kids (AI Galo & AI Mia)
            var galo = CreateOpponent("AI Galo", AIDifficulty.Easy, "bantay_kalye", 1, 50,
                new Vector3(5.3f, 0f, 3.8f), Quaternion.Euler(0f, 195f, 0f),
                9,
                isSeated: false, isWandering: false);
            galo.ChallengeQuote = "Kakabili ko lang kay Aling Nena! Laban tayo!";
            galo.AmbientCallouts = new[] { "Bagong bili 'tong Teks ko!", "Tara 1v1, 50 Coins lang!", "Punta ka rin sa Sari-Sari Store!" };

            var mia = CreateOpponent("AI Mia", AIDifficulty.Easy, "sigbin_runner", 2, 75,
                new Vector3(7.4f, 0f, 1.2f), Quaternion.Euler(0f, -120f, 0f),
                63,
                isSeated: false, isWandering: true);
            mia.ChallengeQuote = "Uy! Challenge kita — bilis ng Sigbin Runner ko!";
            mia.AmbientCallouts = new[] { "Uy! Teks tayo!", "Habulan ng swerte!", "Sino palag dyan?" };

            // 4. Sidelines Stronger Opponents & Bosses (Bleachers Bench Sitter + Shady Area)
            var tisoy = CreateOpponent("Boss Tisoy", AIDifficulty.Hard, "mecha_bathala", 4, 250,
                new Vector3(-3.2f, 0f, 6.92f), Quaternion.Euler(0f, 180f, 0f),
                22,
                isSeated: false, isWandering: false, isBenchSitter: true);
            tisoy.ChallengeQuote = "Lakasan mo flick mo, bata — hindi ka uubra sa Mecha Bathala ko!";
            tisoy.AmbientCallouts = new[] { "May lakas ng loob ka ba?", "250 Coins pusta — palag?", "Bleachers Boss ako dito!" };
            tisoy.WinBanter = "Hahaha! Balik ka pag 5-star na Pamato mo!";
            tisoy.LoseBanter = "Teka lang... natalo mo Mecha Bathala ko?!";

            var kapitan = CreateOpponent("Kapitan Kid", AIDifficulty.Boss, "bakunawa", 5, 450,
                new Vector3(10.6f, 0f, -4.6f), Quaternion.Euler(0f, -45f, 0f),
                33,
                isSeated: false, isWandering: false);
            kapitan.ChallengeQuote = "Ako hari ng court na 'to! Laban kung matapang ka — 450 Coins!";
            kapitan.AmbientCallouts = new[] { "Hari ng Barangay Court!", "Subukan mo Bakunawa ko!", "Boss Fight — 450 Coins!" };
            kapitan.WinBanter = "Walang makakatalo sa Hari ng Court!";
            kapitan.LoseBanter = "Ikaw na bagong Teks Master ng Barangay!!";

            // 5. SOUTH ZONE (Sari-Sari & Kalye — North Sidewalk & South Shoulder, clear of Highway asphalt)
            var junjun = CreateOpponent("AI Junjun", AIDifficulty.Easy, "tumbang_striker", 2, 70,
                new Vector3(-7.4f, 0f, -10.8f), Quaternion.Euler(0f, 160f, 0f),
                3,
                isSeated: false, isWandering: true);
            junjun.ChallengeQuote = "Fishball muna tapos Teks tayo dito sa kalye!";
            junjun.AmbientCallouts = new[] { "Fishball & Teks!", "70 Coins lang, tara!", "Tumbang Striker ko 'to!" };

            var bea = CreateOpponent("AI Bea", AIDifficulty.Medium, "patintero_flash", 3, 140,
                new Vector3(2.8f, 0f, -10.8f), Quaternion.Euler(0f, -165f, 0f),
                68,
                isSeated: false, isWandering: true);
            bea.ChallengeQuote = "Teks showdown dito sa kalye! Mabilis ang Patintero Flash ko!";
            bea.AmbientCallouts = new[] { "Kalye Showdown tayo!", "140 Coins, palag?", "Wala kang laban sa bilis ko!" };

            var manong = CreateOpponent("Manong Driver", AIDifficulty.Hard, "jeepney_mech", 4, 300,
                new Vector3(4.6f, 0f, -19.4f), Quaternion.Euler(0f, -25f, 0f),
                30,
                isSeated: false, isWandering: false);
            manong.ChallengeQuote = "Sakay na! Kaya mo ba Jeepney Mech MK-V ko, bata?";
            manong.AmbientCallouts = new[] { "Jeepney Mech pusta!", "300 Coins — tara!", "Barya lang sa umaga!" };
            manong.WinBanter = "Hahaha! Bayad muna ng pamasahe, bata!";
            manong.LoseBanter = "Aba, ang galing mo mag-Teks ah! Libre sakay ka na!";

            // 6. EAST ZONE (Player's Neighborhood & Plaza Gazebo) AI Opponents
            var lorna = CreateOpponent("Ate Lorna", AIDifficulty.Hard, "diwata_maria", 4, 320,
                new Vector3(25.52f, 0f, -2.6f), Quaternion.Euler(0f, -90f, 0f),
                88,
                isSeated: false, isWandering: false, isBenchSitter: true);
            lorna.ChallengeQuote = "Swerte ng Diwata Maria ang kalaban mo! Ready ka na ba?";
            lorna.AmbientCallouts = new[] { "Diwata Maria blessing!", "320 Coins challenge!", "Pahinga muna sa Plaza Bench!" };

            var panday = CreateOpponent("Master Panday", AIDifficulty.Boss, "panday_blade", 5, 500,
                new Vector3(23.5f, 0f, -0.8f), Quaternion.Euler(0f, -90f, 0f),
                45,
                isSeated: false, isWandering: false);
            panday.ChallengeQuote = "Apoy ng Panday Blade ang haharapin mo — 500 Coins!";
            panday.AmbientCallouts = new[] { "Plaza Boss Challenge!", "500 Coins pusta!", "Panday Blade awaits!" };
            panday.WinBanter = "Hindi pa hinog ang Pamato mo, bata!";
            panday.LoseBanter = "Matibay ka! Karapat-dapat ka sa Panday Blade!";

            // 7. WEST ZONE (Old Neighborhood & Waiting Shed) AI Opponents
            var toto = CreateOpponent("AI Toto", AIDifficulty.Easy, "sipa_master", 2, 80,
                new Vector3(-19.5f, 0f, -1.6f), Quaternion.Euler(0f, 80f, 0f),
                41,
                isSeated: false, isWandering: true);
            toto.ChallengeQuote = "Tambay Teks tayo dito sa lilim! 80 Coins lang!";
            toto.AmbientCallouts = new[] { "Tambay Teks tayo!", "Presko dito sa West Shed!", "80 Coins pusta!" };

            var bert = CreateOpponent("Kuya Bert", AIDifficulty.Hard, "kapre_titan", 4, 280,
                new Vector3(-22.5f, 0f, 3.72f), Quaternion.Euler(0f, 180f, 0f),
                48,
                isSeated: false, isWandering: false, isBenchSitter: true);
            bert.ChallengeQuote = "Lahat ng bata dito talo sa Kapre Titan ko! Palag ka?";
            bert.AmbientCallouts = new[] { "Tambayan Veteran!", "280 Coins — laban?", "Kapre Titan unbeatable!" };

            var lolo = CreateOpponent("Lolo Teks", AIDifficulty.Boss, "ibong_adarna", 5, 600,
                new Vector3(-25.8f, 0f, -4.8f), Quaternion.Euler(0f, 55f, 0f),
                44,
                isSeated: false, isWandering: false);
            lolo.ChallengeQuote = "50 taon na akong nagte-Teks! Patunayan mong karapat-dapat ka — 600 Coins!";
            lolo.AmbientCallouts = new[] { "Grandmaster ng Barangay!", "600 Coins Boss Fight!", "Ibong Adarna Pamato!" };
            lolo.WinBanter = "Kailangan mo pa ng praktis, apo!";
            lolo.LoseBanter = "Kahanga-hanga! Ikaw na ang susunod na Teks Legend!";

            // 8. EXTRA NEIGHBORHOOD OPPONENTS (Placed ONLY in Separate External Areas — Keeping Basketball Court Clean!)
            // South Kalye Street (by Fishball Cart on North Sidewalk)
            var tonton = CreateOpponent("AI Tonton", AIDifficulty.Easy, "balut_drone", 2, 65,
                new Vector3(-12.2f, 0f, -11.2f), Quaternion.Euler(0f, 150f, 0f),
                28,
                isSeated: false, isWandering: true);
            tonton.ChallengeQuote = "Fishball muna dito sa Kalye tapos Teks tayo!";
            tonton.AmbientCallouts = new[] { "Teks sa Kalye!", "65 Coins lang!", "Balut Drone Pamato ko!" };

            // East Player's Neighborhood / Plaza Park
            var nica = CreateOpponent("AI Nica", AIDifficulty.Medium, "halo_halo_core", 3, 130,
                new Vector3(21.2f, 0f, 5.2f), Quaternion.Euler(0f, -145f, 0f),
                76,
                isSeated: false, isWandering: true);
            nica.ChallengeQuote = "Ang presko dito sa Plaza Park — Teks showdown, 130 Coins!";
            nica.AmbientCallouts = new[] { "Halo-Halo Core power!", "130 Coins pusta!", "Laban sa Plaza Park!" };

            // West Old Neighborhood
            var marco = CreateOpponent("AI Marco", AIDifficulty.Medium, "luksong_tinik", 3, 125,
                new Vector3(-19.2f, 0f, 4.2f), Quaternion.Euler(0f, 125f, 0f),
                34,
                isSeated: false, isWandering: true);
            marco.ChallengeQuote = "Dito sa Old Neighborhood — mainit ang laban ng Teks!";
            marco.AmbientCallouts = new[] { "West Alley Teks!", "125 Coins tara!", "Luksong Tinik jump!" };

            var ella = CreateOpponent("AI Ella", AIDifficulty.Easy, "sorbetes_bot", 2, 75,
                new Vector3(-25.4f, 0f, 0.8f), Quaternion.Euler(0f, 75f, 0f),
                69,
                isSeated: false, isWandering: true);
            ella.ChallengeQuote = "Tara dito sa Waiting Shed! Friendly Teks, 75 Coins!";
            ella.AmbientCallouts = new[] { "Waiting Shed Teks!", "Sorbetes Bot freeze!", "75 Coins tara!" };

            // 9. SOUTH-EAST FIESTA TEKS ARENA ELITE CHALLENGERS (shifted South of Highway by Δz = -10.0)
            var ronnie = CreateOpponent("Kuya Ronnie", AIDifficulty.Hard, "rizal_vanguard", 4, 270,
                new Vector3(19.6f, 0f, -28.5f), Quaternion.Euler(0f, 65f, 0f),
                37,
                isSeated: false, isWandering: false);
            ronnie.ChallengeQuote = "Welcome sa Fiesta Teks Arena! Subukan mo Manila Vanguard ko — 270 Coins!";
            ronnie.AmbientCallouts = new[] { "Manila Vanguard!", "270 Coins Fiesta Match!", "Fiesta Arena Challenger!" };

            var macky = CreateOpponent("Boss Macky", AIDifficulty.Hard, "minokawa", 4, 290,
                new Vector3(23.2f, 0f, -25.6f), Quaternion.Euler(0f, -165f, 0f),
                14,
                isSeated: false, isWandering: false);
            macky.ChallengeQuote = "Eclipse ng Minokawa ang haharapin mo sa Fiesta Arena — 290 Coins!";
            macky.AmbientCallouts = new[] { "Minokawa Eclipse!", "290 Coins — palag?", "Fiesta Semi-Finalist!" };
            macky.WinBanter = "Haha! Eclipse ka ng Minokawa ko!";
            macky.LoseBanter = "Solid ng Pamato mo! Harapin mo sina Rico at Teks Master!";

            var rico = CreateOpponent("Fiesta Champ Rico", AIDifficulty.Boss, "sun_dragon_hero", 5, 750,
                new Vector3(21.2f, 0f, -30.8f), Quaternion.Euler(0f, 35f, 0f),
                46,
                isSeated: false, isWandering: false);
            rico.ChallengeQuote = "Ako ang dating Fiesta Teks Champion! Subukan mo Sun Dragon Hero ko!";
            rico.AmbientCallouts = new[] { "FIESTA BOSS RICO!", "Sun Dragon Hero Pamato!", "Warm-up bago si Teks Master!" };
            rico.WinBanter = "Hindi pa sapat ang lakas mo para sa Fiesta Trophy!";
            rico.LoseBanter = "MABUHAY! Handa ka na talaga kay Teks Master!!";

            // 10. SOUTH-WEST TOURNAMENT ARENA CONTENDERS & BLEACHER CROWD (Battle Ring center (-22.8, 0, -28.5) kept clear)
            var enzo = CreateOpponent("Arena Kid Enzo", AIDifficulty.Medium, "patintero_flash", 3, 150,
                new Vector3(-18.2f, 0f, -24.6f), Quaternion.Euler(0f, -135f, 0f),
                10,
                isSeated: false, isWandering: false, isBenchSitter: false,
                npcId: "arena_enzo", areaOverride: BarangayAreaId.TournamentArena);
            enzo.ChallengeQuote = "Warm-up Teks sa Tournament Arena! Subukan mo ang bilis ng Patintero Flash ko!";
            enzo.AmbientCallouts = new[] { "Handa na sa 3-Round Cup!", "150 Coins warm-up match?", "Mabilis 'tong Patintero Flash!" };
            enzo.WinBanter = "Solid! Mag-ensayo ka muna bago mag-register kay Kuya Ref!";
            enzo.LoseBanter = "Lupet! Puwedeng-puwede ka na sa Tournament Bracket!";

            var maya = CreateOpponent("Contender Maya", AIDifficulty.Medium, "agila_kid", 3, 180,
                new Vector3(-26.8f, 0f, -24.8f), Quaternion.Euler(0f, 135f, 0f),
                57,
                isSeated: false, isWandering: false, isBenchSitter: false,
                npcId: "arena_maya", areaOverride: BarangayAreaId.TournamentArena);
            maya.ChallengeQuote = "Galing din akong Quarterfinals! Kaya mo ba ang Agila Kid ko — 180 Coins?";
            maya.AmbientCallouts = new[] { "Gusto ko rin ng Gold Trophy!", "Tara exhibition match muna!", "Agila Kid, lipad!" };
            maya.WinBanter = "Hehe, hindi basta-basta natatalo ang Arena contender!";
            maya.LoseBanter = "Ang galing! Good luck sa Championship!";

            var vince = CreateOpponent("Arena Ace Vince", AIDifficulty.Hard, "cyber_tikbalang", 4, 280,
                new Vector3(-18.6f, 0f, -32.2f), Quaternion.Euler(0f, -45f, 0f),
                72,
                isSeated: false, isWandering: false, isBenchSitter: false,
                npcId: "arena_vince", areaOverride: BarangayAreaId.TournamentArena);
            vince.ChallengeQuote = "Ako ang paborito sa Semifinals bracket! Harapin mo Cyber Tikbalang ko!";
            vince.AmbientCallouts = new[] { "Fused Cyber Tikbalang 'to!", "280 Coins Arena Duel!", "Malakas mga kalaban dito!" };
            vince.WinBanter = "Mabigat talaga ang Cyber Tikbalang ko!";
            vince.LoseBanter = "Ayos! Karapat-dapat ka ngang challenger sa Trophy!";

            var chloe = CreateOpponent("Tourney Pro Chloe", AIDifficulty.Hard, "eclipse_sarimanok", 4, 320,
                new Vector3(-26.8f, 0f, -32.0f), Quaternion.Euler(0f, 45f, 0f),
                92,
                isSeated: false, isWandering: false, isBenchSitter: false,
                npcId: "arena_chloe", areaOverride: BarangayAreaId.TournamentArena);
            chloe.ChallengeQuote = "Bago ka sumabak sa 600-Coin Cup, daanan mo muna ang Eclipse Sarimanok ko!";
            chloe.AmbientCallouts = new[] { "Eclipse Sarimanok Pamato!", "320 Coins Pro Warm-up!", "Target ko ang 1st Prize!" };
            chloe.WinBanter = "Swerte at lakas ang Eclipse Sarimanok ko!";
            chloe.LoseBanter = "Solid! Ikaw na ang mukhang magcha-champion dito!";

            // Seated spectators cheering along the West & South Tournament Bleachers
            CreateSpectatorKid("ArenaSpectator_W1", new Vector3(-28.52f, 0f, -25.6f), Quaternion.Euler(0f, 90f, 0f), 12, CharacterPoseMode.SittingOnBench);
            CreateSpectatorKid("ArenaSpectator_W2", new Vector3(-28.52f, 0f, -28.5f), Quaternion.Euler(0f, 90f, 0f), 55, CharacterPoseMode.SittingOnBench);
            CreateSpectatorKid("ArenaSpectator_W3", new Vector3(-28.52f, 0f, -31.4f), Quaternion.Euler(0f, 90f, 0f), 16, CharacterPoseMode.SittingOnBench);
            CreateSpectatorKid("ArenaSpectator_W4", new Vector3(-29.42f, 0.66f, -27.0f), Quaternion.Euler(0f, 90f, 0f), 64, CharacterPoseMode.SittingOnBench);
            CreateSpectatorKid("ArenaSpectator_W5", new Vector3(-29.42f, 0.66f, -30.0f), Quaternion.Euler(0f, 90f, 0f), 21, CharacterPoseMode.SittingOnBench);

            CreateSpectatorKid("ArenaSpectator_S1", new Vector3(-25.4f, 0f, -34.02f), Quaternion.identity, 79, CharacterPoseMode.SittingOnBench);
            CreateSpectatorKid("ArenaSpectator_S2", new Vector3(-22.8f, 0f, -34.02f), Quaternion.identity, 31, CharacterPoseMode.SittingOnBench);
            CreateSpectatorKid("ArenaSpectator_S3", new Vector3(-20.2f, 0f, -34.02f), Quaternion.identity, 66, CharacterPoseMode.SittingOnBench);
            CreateSpectatorKid("ArenaSpectator_S4", new Vector3(-24.1f, 0.66f, -34.92f), Quaternion.identity, 15, CharacterPoseMode.SittingOnBench);
            CreateSpectatorKid("ArenaSpectator_S5", new Vector3(-21.5f, 0.66f, -34.92f), Quaternion.identity, 83, CharacterPoseMode.SittingOnBench);
        }

        private void SpawnHiddenCollectibleCards()
        {
            var hiddenGroup = new GameObject("HiddenCards").transform;
            hiddenGroup.SetParent(CourtRoot, false);

            // Area 0: Player's Neighborhood (3 Hidden Cards)
            CreateHiddenCard(hiddenGroup, "hidden_behind_player_house", "duwende_trickster",
                BarangayAreaId.PlayersNeighborhood, new Vector3(27.5f, 0.35f, 9.6f), "Behind Your House", 50, 5);
            CreateHiddenCard(hiddenGroup, "hidden_plaza_palm_tree", "sipa_master",
                BarangayAreaId.PlayersNeighborhood, new Vector3(16.8f, 0.30f, -2.8f), "Near Plaza Palm Tree", 60, 5);
            CreateHiddenCard(hiddenGroup, "hidden_behind_gazebo", "sarimanok",
                BarangayAreaId.PlayersNeighborhood, new Vector3(23.5f, 0.35f, 5.4f), "Behind Plaza Gazebo", 85, 8);

            // Area 1: Sari-Sari & Kalye (2 Hidden Cards)
            CreateHiddenCard(hiddenGroup, "hidden_behind_jeepney", "scrap_trisikad",
                BarangayAreaId.SariSariAndKalye, new Vector3(12.4f, 0.30f, -16.7f), "Behind Barangay Jeepney", 50, 5);
            CreateHiddenCard(hiddenGroup, "hidden_fishball_corner", "cyber_taho_bot",
                BarangayAreaId.SariSariAndKalye, new Vector3(-13.8f, 0.30f, -9.4f), "Fishball Alley Corner", 60, 5);

            // Area 2: Basketball Court (4 Hidden Cards)
            CreateHiddenCard(hiddenGroup, "hidden_behind_sarisari", "agila_kid",
                BarangayAreaId.BasketballCourt, new Vector3(12.2f, 0.35f, 6.8f), "NE Court Corner", 75, 8);
            CreateHiddenCard(hiddenGroup, "hidden_under_shady_bench", "tikbalang_striker",
                BarangayAreaId.BasketballCourt, new Vector3(11.4f, 0.22f, -6.65f), "Under Shady Bench", 75, 8);
            CreateHiddenCard(hiddenGroup, "hidden_beside_bleachers", "luksong_tinik",
                BarangayAreaId.BasketballCourt, new Vector3(-9.8f, 0.30f, 7.6f), "Beside Sideline Bleachers", 55, 5);
            CreateHiddenCard(hiddenGroup, "hidden_sw_court_fence", "kid_kidlat",
                BarangayAreaId.BasketballCourt, new Vector3(-13.4f, 0.30f, -7.2f), "SW Court Chain-Link Fence", 75, 8);

            // Area 3: Old Neighborhood (2 Hidden Cards)
            CreateHiddenCard(hiddenGroup, "hidden_behind_waiting_shed", "kapre_titan",
                BarangayAreaId.OldNeighborhood, new Vector3(-23.5f, 0.30f, 5.8f), "Behind Waiting Shed", 80, 8);
            CreateHiddenCard(hiddenGroup, "hidden_nw_alley_tree", "manananggal_wing",
                BarangayAreaId.OldNeighborhood, new Vector3(-16.6f, 0.30f, 10.6f), "Near NW Alley Palm Tree", 95, 10);

            // Area 4: Fiesta Teks Arena (1 Hidden Card — shifted South of Highway by Δz = -10.0)
            CreateHiddenCard(hiddenGroup, "hidden_behind_fiesta_stage", "ibong_adarna",
                BarangayAreaId.FiestaArena, new Vector3(26.9f, 0.35f, -34.1f), "Behind Fiesta Stage", 150, 15);
        }

        private void CreateHiddenCard(
            Transform parent,
            string hiddenId,
            string cardId,
            BarangayAreaId areaId,
            Vector3 worldPos,
            string spotLabel,
            int coinBonus,
            int gemBonus)
        {
            var go = new GameObject("HiddenCard_" + hiddenId);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            var collectible = go.AddComponent<HiddenCardCollectible>();
            collectible.Initialize(hiddenId, cardId, areaId, spotLabel, coinBonus, gemBonus);
            SpawnedHiddenCards.Add(collectible);
        }

        private NpcOpponent CreateOpponent(
            string npcName,
            AIDifficulty difficulty,
            string favCardId,
            int starRank,
            int wager,
            Vector3 pos,
            Quaternion rot,
            int prefabNumber,
            bool isSeated,
            bool isWandering,
            bool isBenchSitter = false,
            string npcId = null,
            BarangayAreaId? areaOverride = null,
            bool isStory = false,
            bool isChampion = false,
            int storyOrder = 0)
        {
            var go = new GameObject(npcName.Replace(" ", "_"));
            go.transform.SetParent(CourtRoot, false);
            go.transform.position = pos;
            go.transform.rotation = rot;

            var actor = CharacterVisualBuilder.BuildPackCharacterVisual(go, prefabNumber);

            if (isBenchSitter)
            {
                actor.SetPose(CharacterPoseMode.SittingOnBench);
            }
            else if (isSeated)
            {
                actor.SetPose(CharacterPoseMode.SittingOnCourt);
            }

            BarangayAreaId area = areaOverride.HasValue ? areaOverride.Value : GetAreaAtPosition(pos);

            var npc = go.AddComponent<NpcOpponent>();
            npc.NpcName = npcName;
            npc.CharacterPrefabNumber = prefabNumber;
            npc.NpcId = !string.IsNullOrWhiteSpace(npcId) ? npcId : npc.GetEffectiveNpcId();
            npc.Difficulty = difficulty;
            npc.AreaId = area;
            npc.IsStoryOpponent = isStory;
            npc.IsChampion = isChampion;
            npc.StoryOrder = storyOrder;
            npc.FavoriteCardId = favCardId;
            npc.CardStarRank = starRank;
            npc.BaseCoinWager = wager;
            npc.IsSeated = isSeated;
            npc.IsBenchSitter = isBenchSitter;
            npc.IsWandering = isWandering && !isSeated && !isBenchSitter;
            npc.WanderCenter = pos;
            npc.WanderRadius = 2.4f;
            npc.PatrolBounds = NpcOpponent.GetDefaultZoneBounds(area);

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.8f, 0f);
            col.radius = 0.32f;
            col.height = 1.6f;

            SpawnedOpponents.Add(npc);
            return npc;
        }

        private void CreateSpectatorKid(string name, Vector3 pos, Quaternion rot, int prefabNumber, CharacterPoseMode pose = CharacterPoseMode.SittingOnCourt)
        {
            var go = new GameObject(name);
            go.transform.SetParent(CourtRoot, false);
            go.transform.position = pos;
            go.transform.rotation = rot;

            var actor = CharacterVisualBuilder.BuildPackCharacterVisual(go, prefabNumber);
            actor.SetPose(pose);
        }

        private TextMesh CreateWorldText(
            string name,
            Transform parent,
            Vector3 localPos,
            string text,
            int fontSize,
            Color color,
            float characterSize = 0.20f,
            float worldScale = 0.13f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * worldScale;

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = Mathf.Clamp(fontSize, 48, 60);
            tm.characterSize = Mathf.Clamp(characterSize, 0.16f, 0.24f);
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;
            go.AddComponent<FrontOnlySignText>();
            return tm;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (!keepCollider) SafeDestroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (!keepCollider) SafeDestroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
