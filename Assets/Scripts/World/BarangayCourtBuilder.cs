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

        public Transform CourtRoot { get; private set; }
        public Vector3 StoreInteractionPoint { get; private set; } = new Vector3(9.2f, 0f, 5.3f);
        public List<NpcOpponent> SpawnedOpponents { get; } = new List<NpcOpponent>();

        private readonly List<TextMesh> _storeTimerMeshes = new List<TextMesh>();
        private readonly List<Renderer> _storeRackCardRenderers = new List<Renderer>();
        private bool _built;

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
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnShopUpdated += RefreshStoreDisplayCards;
                RefreshStoreDisplayCards();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnShopUpdated -= RefreshStoreDisplayCards;
            }
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
            public string Name;
            public string Subtitle;
            public Vector3 SpawnPosition;
            public float SpawnYaw;
        }

        public static readonly BarangayZoneInfo[] WorldZones = new[]
        {
            new BarangayZoneInfo { Name = "BASKETBALL COURT", Subtitle = "Main Hub & Sari-Sari Store (8 Challengers)", SpawnPosition = new Vector3(-3.8f, 0f, -4.6f), SpawnYaw = 34f },
            new BarangayZoneInfo { Name = "KALYE MAIN STREET", Subtitle = "Jeepney & Fishball Cart (4 Challengers)", SpawnPosition = new Vector3(-1.5f, 0f, -13.5f), SpawnYaw = 180f },
            new BarangayZoneInfo { Name = "BARANGAY PLAZA PARK", Subtitle = "Gazebo & Park Lawn (4 Challengers)", SpawnPosition = new Vector3(17.8f, 0f, -0.5f), SpawnYaw = 90f },
            new BarangayZoneInfo { Name = "WEST TAMBAYAN ALLEY", Subtitle = "Waiting Shed & Grandmaster (5 Challengers)", SpawnPosition = new Vector3(-17.8f, 0f, -0.5f), SpawnYaw = -90f },
            new BarangayZoneInfo { Name = "FIESTA TEKS ARENA", Subtitle = "Covered Stage & Elite Bosses (3 Challengers)", SpawnPosition = new Vector3(18.5f, 0f, -15.5f), SpawnYaw = 115f }
        };

        public static string GetZoneNameAtPosition(Vector3 pos)
        {
            if (pos.x >= 14.5f && pos.z <= -9.5f) return "FIESTA TEKS ARENA";
            if (pos.x >= 14.5f) return "BARANGAY PLAZA PARK";
            if (pos.x <= -14.5f) return "WEST TAMBAYAN ALLEY";
            if (pos.z <= -9.5f) return "KALYE MAIN STREET";
            return "BASKETBALL COURT";
        }

        public void BuildWorldIfNeeded()
        {
            const string versionMarker = "WorldVersion_V4";
            if (_built && CourtRoot != null && CourtRoot.Find(versionMarker) != null)
            {
                // Verify materials aren't stale/missing shaders (prevents magenta screen when Reload Domain is disabled)
                var firstRend = CourtRoot.GetComponentInChildren<Renderer>();
                if (firstRend != null && firstRend.sharedMaterial != null && firstRend.sharedMaterial.shader != null &&
                    firstRend.sharedMaterial.shader.name != "Hidden/InternalErrorShader")
                {
                    return;
                }
            }

            var existing = GameObject.Find("BarangayCourtWorld");
            if (existing != null)
            {
                SafeDestroy(existing);
            }

            _built = true;
            SpawnedOpponents.Clear();
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
            BuildSariSariStore();
            BuildExtendedBarangayZones();
            BuildBarangayHousesAndUtilityPoles();
            SpawnAllOpponents();

            RefreshStoreDisplayCards();
        }

        private void CollectDynamicReferences()
        {
            SpawnedOpponents.Clear();
            SpawnedOpponents.AddRange(FindObjectsByType<NpcOpponent>());
            _storeTimerMeshes.Clear();
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
            // Dusty barangay road & ground apron covering the extended neighborhood
            var groundMat = CardArtGenerator.CreateLitMaterial(new Color(0.47f, 0.44f, 0.40f), 0.1f);
            CreateBox("StreetGround", CourtRoot, new Vector3(0f, -0.06f, 0f), new Vector3(110f, 0.1f, 95f), groundMat, keepCollider: true);

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
            floorColliderGo.transform.position = new Vector3(0f, -0.05f, 0f);
            var box = floorColliderGo.AddComponent<BoxCollider>();
            box.size = new Vector3(110f, 0.1f, 95f);

            // Extended Barangay perimeter barriers (allows exploring South Street, East Plaza, West Tambayan, Fiesta Arena)
            CreateInvisibleWall("BoundaryNorth", new Vector3(0f, 2f, 12.2f), new Vector3(72f, 5f, 0.4f));
            CreateInvisibleWall("BoundarySouth", new Vector3(0f, 2f, -25.5f), new Vector3(72f, 5f, 0.4f));
            CreateInvisibleWall("BoundaryWest", new Vector3(-31.5f, 2f, -6f), new Vector3(0.4f, 5f, 44f));
            CreateInvisibleWall("BoundaryEast", new Vector3(32.5f, 2f, -6f), new Vector3(0.4f, 5f, 44f));
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

            // Back fence segments (leaving space for the Sari-Sari store at x = 5.6..13.0)
            BuildFenceSpan(fenceGroup, new Vector3(-14.4f, 0f, 8.4f), new Vector3(5.6f, 0f, 8.4f), 3.1f, postMat, wireMat);
            // Left fence with wide walkthrough gate (-3.0 .. +3.0) to West Tambayan Alley
            BuildFenceSpan(fenceGroup, new Vector3(-14.4f, 0f, -8.2f), new Vector3(-14.4f, 0f, -3.0f), 3.1f, postMat, wireMat);
            BuildFenceSpan(fenceGroup, new Vector3(-14.4f, 0f, 3.0f), new Vector3(-14.4f, 0f, 8.4f), 3.1f, postMat, wireMat);
            // Right fence with wide walkthrough gate (-3.0 .. +3.0) to East Barangay Plaza
            BuildFenceSpan(fenceGroup, new Vector3(14.4f, 0f, -8.2f), new Vector3(14.4f, 0f, -3.0f), 3.1f, postMat, wireMat);
            BuildFenceSpan(fenceGroup, new Vector3(14.4f, 0f, 3.0f), new Vector3(14.4f, 0f, 8.4f), 3.1f, postMat, wireMat);
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

        private void BuildSariSariStore()
        {
            var store = new GameObject("SariSariStore").transform;
            store.SetParent(CourtRoot, false);
            store.position = new Vector3(9.2f, 0f, 7.6f);

            Material mintWallMat = CardArtGenerator.CreateLitMaterial(new Color(0.55f, 0.78f, 0.67f), 0.18f);
            Material woodMat = CardArtGenerator.CreateLitMaterial(new Color(0.46f, 0.29f, 0.16f), 0.2f);
            Material roofMat = CardArtGenerator.CreateLitMaterial(new Color(0.46f, 0.50f, 0.55f), 0.35f, 0.4f);
            Material darkInterior = CardArtGenerator.CreateLitMaterial(new Color(0.12f, 0.10f, 0.09f), 0.05f);
            Material signBoardBg = CardArtGenerator.CreateLitMaterial(new Color(0.96f, 0.86f, 0.64f), 0.15f);
            Material ledBg = CardArtGenerator.CreateLitMaterial(new Color(0.10f, 0.14f, 0.16f), 0.2f);
            Material teksBannerMat = CardArtGenerator.CreateLitMaterial(new Color(0.12f, 0.36f, 0.26f), 0.2f);

            // Store structure
            CreateBox("StoreBackWall", store, new Vector3(0f, 1.55f, 1.3f), new Vector3(6.8f, 3.1f, 0.25f), mintWallMat, true);
            CreateBox("StoreLeftWall", store, new Vector3(-3.3f, 1.55f, 0f), new Vector3(0.25f, 3.1f, 2.8f), mintWallMat, true);
            CreateBox("StoreRightWall", store, new Vector3(3.3f, 1.55f, 0f), new Vector3(0.25f, 3.1f, 2.8f), mintWallMat, true);
            CreateBox("StoreInteriorBack", store, new Vector3(-1.3f, 1.55f, 1.15f), new Vector3(3.8f, 2.8f, 0.05f), darkInterior, false);

            // Front lower wall & counter ledge
            CreateBox("FrontLowerWall", store, new Vector3(-1.35f, 0.62f, -1.25f), new Vector3(3.75f, 1.24f, 0.25f), mintWallMat, true);
            CreateBox("CounterTop", store, new Vector3(-1.35f, 1.28f, -1.32f), new Vector3(3.85f, 0.10f, 0.55f), woodMat, true);
            CreateBox("RightWallFront", store, new Vector3(1.9f, 1.55f, -1.25f), new Vector3(2.8f, 3.1f, 0.25f), mintWallMat, true);

            // Corrugated tin roof overhang
            var roof = CreateBox("StoreRoof", store, new Vector3(0f, 3.35f, -0.35f), new Vector3(7.4f, 0.14f, 4.1f), roofMat, false);
            roof.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

            // Wooden crate beside counter (like in image_0.png)
            CreateBox("WoodenCrate", store, new Vector3(-2.85f, 0.30f, -1.85f), new Vector3(0.75f, 0.60f, 0.65f), woodMat, true);

            // Colorful hanging snack packets (chichirya strips) in front window
            Color[] snackColors = {
                new Color(0.94f, 0.24f, 0.22f),
                new Color(0.98f, 0.80f, 0.16f),
                new Color(0.22f, 0.78f, 0.36f),
                new Color(0.24f, 0.56f, 0.94f)
            };
            for (int col = 0; col < 4; col++)
            {
                float px = -2.65f + col * 0.82f;
                for (int row = 0; row < 3; row++)
                {
                    Material snackMat = CardArtGenerator.CreateLitMaterial(snackColors[(col + row) % snackColors.Length], 0.35f);
                    CreateBox($"SnackPacket_{col}_{row}", store, new Vector3(px, 2.38f - row * 0.30f, -1.35f), new Vector3(0.22f, 0.24f, 0.05f), snackMat, false);
                }
            }

            // Roof Signboard: "SARI-SARI STORE" + Roof LED Timer: "NEW CARDS IN 04:59"
            CreateBox("SignSariSariBg", store, new Vector3(-1.45f, 3.88f, -1.55f), new Vector3(3.1f, 1.05f, 0.16f), signBoardBg, false);
            CreateWorldText("SariSariTitleText", store, new Vector3(-1.45f, 3.88f, -1.66f), "SARI-SARI\nSTORE", 26, new Color(0.42f, 0.16f, 0.08f));

            CreateBox("RoofTimerBg", store, new Vector3(1.85f, 3.88f, -1.55f), new Vector3(2.9f, 1.05f, 0.16f), ledBg, false);
            var roofTimer = CreateWorldText("StoreTimerText_Roof", store, new Vector3(1.85f, 3.88f, -1.66f), "NEW CARDS IN\n04:59", 24, new Color(0.55f, 1.0f, 0.45f));
            _storeTimerMeshes.Add(roofTimer);

            // Right wall "TEKS CARDS" display rack + 4 miniature 3D rotating shop cards + lower LED timer
            CreateBox("TeksCardsHeaderBg", store, new Vector3(1.85f, 2.75f, -1.42f), new Vector3(2.55f, 0.42f, 0.08f), teksBannerMat, false);
            CreateWorldText("TeksRackHeaderText", store, new Vector3(1.85f, 2.75f, -1.48f), "TEKS CARDS", 22, new Color(1.0f, 0.88f, 0.25f));

            CreateBox("TeksRackBoard", store, new Vector3(1.85f, 2.02f, -1.40f), new Vector3(2.55f, 0.96f, 0.06f), darkInterior, false);
            for (int i = 0; i < 4; i++)
            {
                float cardX = 0.92f + i * 0.62f;
                var cardQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cardQuad.name = "StoreRackCard_" + i;
                cardQuad.transform.SetParent(store, false);
                cardQuad.transform.localPosition = new Vector3(cardX, 2.02f, -1.445f);
                cardQuad.transform.localScale = new Vector3(0.48f, 0.68f, 1f);
                SafeDestroy(cardQuad.GetComponent<Collider>());
                var rend = cardQuad.GetComponent<Renderer>();
                rend.sharedMaterial = CardArtGenerator.CreateLitMaterial(Color.white, 0.25f);
                _storeRackCardRenderers.Add(rend);
            }

            CreateBox("WallTimerBg", store, new Vector3(1.85f, 0.98f, -1.42f), new Vector3(2.55f, 0.88f, 0.08f), ledBg, false);
            var wallTimer = CreateWorldText("StoreTimerText_Wall", store, new Vector3(1.85f, 0.98f, -1.48f), "NEW CARDS IN\n04:59", 21, new Color(0.55f, 1.0f, 0.45f));
            _storeTimerMeshes.Add(wallTimer);

            // Tindera Vendor (Aling Nena) inside the store window
            var tindera = new GameObject("AlingNena_Tindera");
            tindera.transform.SetParent(store, false);
            tindera.transform.localPosition = new Vector3(-1.35f, 0.15f, -0.35f);
            tindera.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            CharacterVisualBuilder.BuildCharacterVisual(
                tindera,
                CharacterOutfitRole.Tindera,
                new Color(0.92f, 0.56f, 0.20f),
                new Color(0.24f, 0.28f, 0.42f),
                new Color(0.75f, 0.52f, 0.37f),
                new Color(0.18f, 0.42f, 0.68f)
            );

            StoreInteractionPoint = new Vector3(7.9f, 0f, 5.4f);
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

            // 1. SOUTH ZONE: Kalye / Barangay Main Street (z = -11 .. -21)
            CreateBox("KalyeAsphaltRoad", zones, new Vector3(0f, -0.008f, -15.5f), new Vector3(62f, 0.02f, 8.6f), asphaltMat, false);
            for (int i = -4; i <= 4; i++)
            {
                CreateBox("RoadDash_" + i, zones, new Vector3(i * 6.0f, 0.004f, -15.5f), new Vector3(3.0f, 0.01f, 0.22f), lanePaintMat, false);
            }

            // Parked Pinoy Jeepney on the Kalye (South-Right)
            var jeep = new GameObject("BarangayJeepney").transform;
            jeep.SetParent(zones, false);
            jeep.position = new Vector3(8.5f, 0f, -17.5f);
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
            CreateWorldText("JeepSign", jeep, new Vector3(0f, 1.95f, 1.92f), "BARANGAY EXPRESS", 18, new Color(1.0f, 0.88f, 0.20f));

            // Street Food / Fishball Cart on the Kalye sidewalk (South-Left)
            var cart = new GameObject("FishballCart").transform;
            cart.SetParent(zones, false);
            cart.position = new Vector3(-8.5f, 0f, -11.4f);
            CreateBox("CartBody", cart, new Vector3(0f, 0.68f, 0f), new Vector3(1.8f, 0.95f, 1.0f), cartYellowMat, true);
            CreateBox("CartTop", cart, new Vector3(0f, 1.18f, 0f), new Vector3(1.9f, 0.08f, 1.1f), chromeMat, false);
            CreateCylinder("CartUmbrellaPole", cart, new Vector3(0f, 1.95f, 0f), new Vector3(0.06f, 1.0f, 0.06f), woodMat, false);
            CreateCylinder("CartUmbrellaTop", cart, new Vector3(0f, 2.85f, 0f), new Vector3(2.5f, 0.12f, 2.5f), jeepRedMat, false);
            CreateWorldText("FishballSign", cart, new Vector3(0f, 0.85f, -0.54f), "FISHBALL & TAHO", 18, new Color(0.35f, 0.12f, 0.05f));

            // 2. EAST ZONE: Barangay Plaza & Kubo Park (x = +16 .. +29)
            CreateBox("EastPlazaLawn", zones, new Vector3(22.5f, -0.009f, 0f), new Vector3(14f, 0.02f, 18f), grassMat, false);
            CreateBox("EastPlazaPatio", zones, new Vector3(22.2f, 0.002f, 0f), new Vector3(10.5f, 0.02f, 12.5f), plazaTileMat, false);

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
            CreateWorldText("PlazaSign", kubo, new Vector3(0f, 3.25f, -2.85f), "BARANGAY PLAZA", 20, new Color(1.0f, 0.90f, 0.35f));

            // 3. WEST ZONE: West Tambayan Alley & Waiting Shed (x = -16 .. -29)
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
            CreateWorldText("ShedSign", shed, new Vector3(0f, 2.95f, -1.65f), "TAMBAYAN SHED", 20, new Color(1.0f, 0.88f, 0.24f));

            // 4. SOUTH-EAST SEPARATE ZONE: Barangay Fiesta Stage & Teks Arena (x = +17 .. +31, z = -12 .. -24)
            var fiesta = new GameObject("BarangayFiestaArena").transform;
            fiesta.SetParent(zones, false);
            fiesta.position = new Vector3(23.5f, 0f, -18.5f);

            CreateBox("FiestaFloorPad", fiesta, new Vector3(0f, 0.004f, 0f), new Vector3(13.5f, 0.02f, 11.5f), plazaTileMat, false);
            CreateBox("FiestaStagePlatform", fiesta, new Vector3(3.4f, 0.35f, -2.6f), new Vector3(5.4f, 0.70f, 3.6f), woodMat, true);
            CreateBox("FiestaStageBackdrop", fiesta, new Vector3(3.4f, 2.30f, -4.3f), new Vector3(5.4f, 3.2f, 0.25f), jeepBlueMat, true);
            CreateBox("FiestaStageRoof", fiesta, new Vector3(3.4f, 4.05f, -2.5f), new Vector3(6.2f, 0.22f, 4.4f), roofMat, false);
            CreateWorldText("FiestaStageSign", fiesta, new Vector3(3.4f, 3.15f, -4.12f), "BARANGAY FIESTA\nTEKS ARENA", 22, new Color(1.0f, 0.88f, 0.22f));

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

            // Directional Wooden Barangay Street Signposts at the Basketball Court Exits
            BuildDirectionSign(zones, new Vector3(-14.8f, 0f, 3.4f), Quaternion.Euler(0f, -90f, 0f), "WEST TAMBAYAN\nALLEY ->", woodMat);
            BuildDirectionSign(zones, new Vector3(14.8f, 0f, 3.4f), Quaternion.Euler(0f, 90f, 0f), "BARANGAY PLAZA\nPARK ->", woodMat);
            BuildDirectionSign(zones, new Vector3(0f, 0f, -9.2f), Quaternion.Euler(0f, 180f, 0f), "KALYE STREET &\nFIESTA ARENA", woodMat);
        }

        private void BuildDirectionSign(Transform parent, Vector3 pos, Quaternion rot, string text, Material woodMat)
        {
            var sign = new GameObject("DirectionSign").transform;
            sign.SetParent(parent, false);
            sign.position = pos;
            sign.rotation = rot;
            CreateCylinder("SignPost", sign, new Vector3(0f, 1.3f, 0f), new Vector3(0.08f, 1.3f, 0.08f), woodMat, false);
            CreateBox("SignBoard", sign, new Vector3(0f, 2.25f, 0f), new Vector3(2.2f, 0.78f, 0.10f), CardArtGenerator.CreateLitMaterial(new Color(0.14f, 0.34f, 0.22f), 0.15f), false);
            CreateWorldText("SignLabel", sign, new Vector3(0f, 2.25f, -0.08f), text, 17, new Color(1.0f, 0.92f, 0.32f));
        }

        private void BuildBarangayHousesAndUtilityPoles()
        {
            var backdrop = new GameObject("BarangayBackdrop").transform;
            backdrop.SetParent(CourtRoot, false);

            Material[] houseWalls = {
                CardArtGenerator.CreateLitMaterial(new Color(0.84f, 0.72f, 0.54f), 0.1f),
                CardArtGenerator.CreateLitMaterial(new Color(0.78f, 0.64f, 0.50f), 0.1f),
                CardArtGenerator.CreateLitMaterial(new Color(0.68f, 0.76f, 0.72f), 0.1f),
                CardArtGenerator.CreateLitMaterial(new Color(0.82f, 0.62f, 0.56f), 0.1f)
            };
            Material tinRoofMat = CardArtGenerator.CreateLitMaterial(new Color(0.56f, 0.38f, 0.30f), 0.25f, 0.25f);
            Material windowMat = CardArtGenerator.CreateLitMaterial(new Color(0.18f, 0.22f, 0.28f), 0.4f);

            Vector3[] housePositions = {
                new Vector3(-16f, 0f, 14f),
                new Vector3(-9.5f, 0f, 15.2f),
                new Vector3(-2.5f, 0f, 14.8f),
                new Vector3(3.2f, 0f, 15.8f),
                new Vector3(16.8f, 0f, 14.5f),
                new Vector3(-33.5f, 0f, 4.5f),
                new Vector3(-33.2f, 0f, -5.5f),
                new Vector3(34.5f, 0f, 3.0f),
                new Vector3(34.5f, 0f, -7.0f)
            };

            for (int i = 0; i < housePositions.Length; i++)
            {
                float hHeight = 4.2f + (i % 3) * 1.4f;
                float hWidth = 4.8f + (i % 2) * 1.1f;
                Vector3 pos = housePositions[i];

                var house = new GameObject("BarangayHouse_" + i).transform;
                house.SetParent(backdrop, false);
                house.position = pos;

                CreateBox("Walls", house, new Vector3(0f, hHeight * 0.5f, 0f), new Vector3(hWidth, hHeight, 4.4f), houseWalls[i % houseWalls.Length], false);
                var roof = CreateBox("Roof", house, new Vector3(0f, hHeight + 0.3f, 0f), new Vector3(hWidth + 0.7f, 0.25f, 5.1f), tinRoofMat, false);
                roof.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);

                CreateBox("Win1", house, new Vector3(-1.1f, hHeight * 0.65f, -2.23f), new Vector3(0.95f, 0.85f, 0.08f), windowMat, false);
                CreateBox("Win2", house, new Vector3(1.1f, hHeight * 0.65f, -2.23f), new Vector3(0.95f, 0.85f, 0.08f), windowMat, false);
            }

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

            // Banana / Palm Trees behind the Sari-Sari store & fence
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

            // 1. Free-throw zone standing opponent (matches AI Gelo (Med) in image_0.png)
            var gelo = CreateOpponent("AI Gelo", AIDifficulty.Medium, "kid_kidlat", 3, 120,
                new Vector3(-5.8f, 0f, -1.2f), Quaternion.Euler(0f, 105f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.95f, 0.95f, 0.95f), new Color(0.86f, 0.36f, 0.14f), new Color(0.20f, 0.20f, 0.25f),
                isSeated: false, isWandering: false);
            gelo.ChallengeQuote = "Hoy! Teks tayo! Kaya mo ba Kid Kidlat ko?";
            gelo.AmbientCallouts = new[] { "Hoy! Laban tayo Teks!", "Pusta 120 Coins, tara!", "Chub lagi Pamato ko!" };
            gelo.WinBanter = "Haha! Sabi sa'yo Chub akin eh!";
            gelo.LoseBanter = "Aray! Ang lakas ng flick mo, rematch!";

            // 2. Play Circle seated kids around center (0,0,0)
            var kiko = CreateOpponent("AI Kiko", AIDifficulty.Medium, "tikbalang_striker", 3, 100,
                new Vector3(-1.35f, 0f, 0.25f), Quaternion.Euler(0f, 95f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.96f, 0.78f, 0.16f), new Color(0.18f, 0.42f, 0.28f), new Color(0.20f, 0.40f, 0.75f),
                isSeated: true, isWandering: false);
            kiko.ChallengeQuote = "Upo ka dito sa bilog! Subukan mo Tikbalang ko!";
            kiko.AmbientCallouts = new[] { "Tara sa play circle!", "Sino may pusta dyan?", "Laban tayo 1v1!" };

            var may = CreateOpponent("AI May", AIDifficulty.Medium, "sarimanok", 3, 110,
                new Vector3(0.75f, 0f, 1.35f), Quaternion.Euler(0f, 200f, 0f),
                CharacterOutfitRole.FemaleKid,
                new Color(0.55f, 0.62f, 0.74f), new Color(0.18f, 0.26f, 0.45f), new Color(0.85f, 0.35f, 0.55f),
                isSeated: true, isWandering: false);
            may.ChallengeQuote = "Swerte ang Sarimanok ko — laban ka ba?";
            may.AmbientCallouts = new[] { "Gusto mo mag-Teks?", "Bago kong Sarimanok 'to!", "Pusta na!" };

            var sarah = CreateOpponent("AI Sarah", AIDifficulty.Easy, "daragang_mandirigma", 2, 60,
                new Vector3(1.25f, 0f, -0.95f), Quaternion.Euler(0f, -45f, 0f),
                CharacterOutfitRole.FemaleKid,
                new Color(0.95f, 0.55f, 0.72f), new Color(0.25f, 0.25f, 0.32f), new Color(0.92f, 0.40f, 0.65f),
                isSeated: true, isWandering: false);
            sarah.ChallengeQuote = "Practice tayo! Friendly Teks lang, 60 Coins!";
            sarah.AmbientCallouts = new[] { "Laro tayo Teks!", "Friendly match lang!", "Tara, flick na!" };

            // Spectator kids seated on circle (aesthetic crowd matching image_0.png)
            CreateSpectatorKid("Spectator_Boy", new Vector3(-0.35f, 0f, 1.55f), Quaternion.Euler(0f, 170f, 0f),
                CharacterOutfitRole.MaleKid, new Color(0.90f, 0.90f, 0.92f), new Color(0.22f, 0.24f, 0.30f));
            CreateSpectatorKid("Spectator_Girl", new Vector3(1.65f, 0f, 0.25f), Quaternion.Euler(0f, -95f, 0f),
                CharacterOutfitRole.FemaleKid, new Color(0.98f, 0.72f, 0.78f), new Color(0.32f, 0.22f, 0.38f));

            // 3. Storefront / Right Court kids (AI Galo & AI Mia)
            var galo = CreateOpponent("AI Galo", AIDifficulty.Easy, "bantay_kalye", 1, 50,
                new Vector3(5.3f, 0f, 3.8f), Quaternion.Euler(0f, 195f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.22f, 0.56f, 0.88f), new Color(0.76f, 0.72f, 0.58f), new Color(0.18f, 0.38f, 0.72f),
                isSeated: false, isWandering: false);
            galo.ChallengeQuote = "Kakabili ko lang kay Aling Nena! Laban tayo!";
            galo.AmbientCallouts = new[] { "Bagong bili 'tong Teks ko!", "Tara 1v1, 50 Coins lang!", "Punta ka rin sa Sari-Sari Store!" };

            var mia = CreateOpponent("AI Mia", AIDifficulty.Easy, "sigbin_runner", 2, 75,
                new Vector3(7.4f, 0f, 1.2f), Quaternion.Euler(0f, -120f, 0f),
                CharacterOutfitRole.FemaleKid,
                new Color(0.88f, 0.22f, 0.20f), new Color(0.20f, 0.26f, 0.42f), new Color(0.95f, 0.76f, 0.22f),
                isSeated: false, isWandering: true);
            mia.ChallengeQuote = "Uy! Challenge kita — bilis ng Sigbin Runner ko!";
            mia.AmbientCallouts = new[] { "Uy! Teks tayo!", "Habulan ng swerte!", "Sino palag dyan?" };

            // 4. Sidelines Stronger Opponents & Bosses (Bleachers + Shady Area)
            var tisoy = CreateOpponent("Boss Tisoy", AIDifficulty.Hard, "mecha_bathala", 4, 250,
                new Vector3(-3.2f, 0f, 5.4f), Quaternion.Euler(0f, 180f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.45f, 0.20f, 0.72f), new Color(0.14f, 0.14f, 0.18f), new Color(0.88f, 0.32f, 0.96f),
                isSeated: false, isWandering: false);
            tisoy.ChallengeQuote = "Lakasan mo flick mo, bata — hindi ka uubra sa Mecha Bathala ko!";
            tisoy.AmbientCallouts = new[] { "May lakas ng loob ka ba?", "250 Coins pusta — palag?", "Bleachers Boss ako dito!" };
            tisoy.WinBanter = "Hahaha! Balik ka pag 5-star na Pamato mo!";
            tisoy.LoseBanter = "Teka lang... natalo mo Mecha Bathala ko?!";

            var kapitan = CreateOpponent("Kapitan Kid", AIDifficulty.Boss, "bakunawa", 5, 450,
                new Vector3(10.6f, 0f, -4.6f), Quaternion.Euler(0f, -45f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.16f, 0.16f, 0.22f), new Color(0.92f, 0.66f, 0.14f), new Color(0.96f, 0.24f, 0.24f),
                isSeated: false, isWandering: false);
            kapitan.ChallengeQuote = "Ako hari ng court na 'to! Laban kung matapang ka — 450 Coins!";
            kapitan.AmbientCallouts = new[] { "Hari ng Barangay Court!", "Subukan mo Bakunawa ko!", "Boss Fight — 450 Coins!" };
            kapitan.WinBanter = "Walang makakatalo sa Hari ng Court!";
            kapitan.LoseBanter = "Ikaw na bagong Teks Master ng Barangay!!";

            // 5. SOUTH ZONE (Kalye / Main Street) AI Opponents
            var junjun = CreateOpponent("AI Junjun", AIDifficulty.Easy, "tumbang_striker", 2, 70,
                new Vector3(-6.2f, 0f, -12.6f), Quaternion.Euler(0f, 25f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.24f, 0.72f, 0.42f), new Color(0.20f, 0.24f, 0.34f), new Color(0.95f, 0.78f, 0.20f),
                isSeated: false, isWandering: true);
            junjun.ChallengeQuote = "Fishball muna tapos Teks tayo dito sa kalye!";
            junjun.AmbientCallouts = new[] { "Fishball & Teks!", "70 Coins lang, tara!", "Tumbang Striker ko 'to!" };

            var bea = CreateOpponent("AI Bea", AIDifficulty.Medium, "patintero_flash", 3, 140,
                new Vector3(2.8f, 0f, -13.2f), Quaternion.Euler(0f, -20f, 0f),
                CharacterOutfitRole.FemaleKid,
                new Color(0.96f, 0.50f, 0.16f), new Color(0.18f, 0.28f, 0.56f), new Color(1.0f, 0.86f, 0.22f),
                isSeated: false, isWandering: true);
            bea.ChallengeQuote = "Teks showdown dito sa kalye! Mabilis ang Patintero Flash ko!";
            bea.AmbientCallouts = new[] { "Kalye Showdown tayo!", "140 Coins, palag?", "Wala kang laban sa bilis ko!" };

            var manong = CreateOpponent("Manong Driver", AIDifficulty.Hard, "jeepney_mech", 4, 300,
                new Vector3(6.2f, 0f, -14.2f), Quaternion.Euler(0f, -45f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.86f, 0.20f, 0.22f), new Color(0.18f, 0.36f, 0.72f), new Color(0.95f, 0.85f, 0.22f),
                isSeated: false, isWandering: false);
            manong.ChallengeQuote = "Sakay na! Kaya mo ba Jeepney Mech MK-V ko, bata?";
            manong.AmbientCallouts = new[] { "Jeepney Mech pusta!", "300 Coins — tara!", "Barya lang sa umaga!" };
            manong.WinBanter = "Hahaha! Bayad muna ng pamasahe, bata!";
            manong.LoseBanter = "Aba, ang galing mo mag-Teks ah! Libre sakay ka na!";

            // 6. EAST ZONE (Barangay Plaza & Gazebo) AI Opponents
            var carlo = CreateOpponent("AI Carlo", AIDifficulty.Medium, "agila_kid", 3, 160,
                new Vector3(19.2f, 0f, -2.2f), Quaternion.Euler(0f, -75f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.20f, 0.48f, 0.92f), new Color(0.88f, 0.64f, 0.16f), new Color(0.95f, 0.95f, 0.95f),
                isSeated: false, isWandering: true);
            carlo.ChallengeQuote = "Plaza champion ako! Subukan mo Agila Kid ko!";
            carlo.AmbientCallouts = new[] { "Welcome sa Plaza!", "160 Coins pusta!", "Teks tayo sa Gazebo!" };

            var lorna = CreateOpponent("Ate Lorna", AIDifficulty.Hard, "diwata_maria", 4, 320,
                new Vector3(25.2f, 0f, -4.4f), Quaternion.Euler(0f, -40f, 0f),
                CharacterOutfitRole.FemaleKid,
                new Color(0.16f, 0.68f, 0.46f), new Color(0.92f, 0.76f, 0.24f), new Color(0.22f, 0.84f, 0.56f),
                isSeated: false, isWandering: false);
            lorna.ChallengeQuote = "Swerte ng Diwata Maria ang kalaban mo! Ready ka na ba?";
            lorna.AmbientCallouts = new[] { "Diwata Maria blessing!", "320 Coins challenge!", "Laban sa Plaza!" };

            var panday = CreateOpponent("Master Panday", AIDifficulty.Boss, "panday_blade", 5, 500,
                new Vector3(23.5f, 0f, -0.8f), Quaternion.Euler(0f, -90f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.76f, 0.18f, 0.12f), new Color(0.16f, 0.14f, 0.16f), new Color(1.0f, 0.76f, 0.18f),
                isSeated: false, isWandering: false);
            panday.ChallengeQuote = "Apoy ng Panday Blade ang haharapin mo — 500 Coins!";
            panday.AmbientCallouts = new[] { "Plaza Boss Challenge!", "500 Coins pusta!", "Panday Blade awaits!" };
            panday.WinBanter = "Hindi pa hinog ang Pamato mo, bata!";
            panday.LoseBanter = "Matibay ka! Karapat-dapat ka sa Panday Blade!";

            // 7. WEST ZONE (West Tambayan Alley & Waiting Shed) AI Opponents
            var toto = CreateOpponent("AI Toto", AIDifficulty.Easy, "sipa_master", 2, 80,
                new Vector3(-19.5f, 0f, -1.6f), Quaternion.Euler(0f, 80f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.92f, 0.40f, 0.20f), new Color(0.20f, 0.52f, 0.76f), new Color(0.95f, 0.85f, 0.25f),
                isSeated: false, isWandering: true);
            toto.ChallengeQuote = "Tambay Teks tayo dito sa lilim! 80 Coins lang!";
            toto.AmbientCallouts = new[] { "Tambay Teks tayo!", "Presko dito sa West Shed!", "80 Coins pusta!" };

            var bert = CreateOpponent("Kuya Bert", AIDifficulty.Hard, "kapre_titan", 4, 280,
                new Vector3(-22.5f, 0f, 1.4f), Quaternion.Euler(0f, 130f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.24f, 0.42f, 0.22f), new Color(0.42f, 0.24f, 0.14f), new Color(0.96f, 0.56f, 0.18f),
                isSeated: false, isWandering: false);
            bert.ChallengeQuote = "Lahat ng bata dito talo sa Kapre Titan ko! Palag ka?";
            bert.AmbientCallouts = new[] { "Tambayan Veteran!", "280 Coins — laban?", "Kapre Titan unbeatable!" };

            var lolo = CreateOpponent("Lolo Teks", AIDifficulty.Boss, "ibong_adarna", 5, 600,
                new Vector3(-25.8f, 0f, -4.8f), Quaternion.Euler(0f, 55f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.92f, 0.78f, 0.46f), new Color(0.28f, 0.22f, 0.18f), new Color(0.96f, 0.68f, 0.14f),
                isSeated: false, isWandering: false);
            lolo.ChallengeQuote = "50 taon na akong nagte-Teks! Patunayan mong karapat-dapat ka — 600 Coins!";
            lolo.AmbientCallouts = new[] { "Grandmaster ng Barangay!", "600 Coins Boss Fight!", "Ibong Adarna Pamato!" };
            lolo.WinBanter = "Kailangan mo pa ng praktis, apo!";
            lolo.LoseBanter = "Kahanga-hanga! Ikaw na ang susunod na Teks Legend!";

            // 8. EXTRA NEIGHBORHOOD OPPONENTS (Placed ONLY in Separate External Areas — Keeping Basketball Court Clean!)
            // South Kalye Street (by Fishball Cart)
            var tonton = CreateOpponent("AI Tonton", AIDifficulty.Easy, "balut_drone", 2, 65,
                new Vector3(-10.5f, 0f, -13.2f), Quaternion.Euler(0f, 40f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.96f, 0.82f, 0.22f), new Color(0.18f, 0.36f, 0.64f), new Color(0.88f, 0.24f, 0.24f),
                isSeated: false, isWandering: true);
            tonton.ChallengeQuote = "Fishball muna dito sa Kalye tapos Teks tayo!";
            tonton.AmbientCallouts = new[] { "Teks sa Kalye!", "65 Coins lang!", "Balut Drone Pamato ko!" };

            // East Barangay Plaza Park
            var nica = CreateOpponent("AI Nica", AIDifficulty.Medium, "halo_halo_core", 3, 130,
                new Vector3(21.2f, 0f, 5.2f), Quaternion.Euler(0f, -145f, 0f),
                CharacterOutfitRole.FemaleKid,
                new Color(0.62f, 0.26f, 0.88f), new Color(0.94f, 0.74f, 0.22f), new Color(0.24f, 0.86f, 0.80f),
                isSeated: false, isWandering: true);
            nica.ChallengeQuote = "Ang presko dito sa Plaza Park — Teks showdown, 130 Coins!";
            nica.AmbientCallouts = new[] { "Halo-Halo Core power!", "130 Coins pusta!", "Laban sa Plaza Park!" };

            // West Tambayan Alley
            var marco = CreateOpponent("AI Marco", AIDifficulty.Medium, "luksong_tinik", 3, 125,
                new Vector3(-19.2f, 0f, 4.2f), Quaternion.Euler(0f, 125f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.22f, 0.62f, 0.86f), new Color(0.18f, 0.20f, 0.28f), new Color(0.96f, 0.72f, 0.18f),
                isSeated: false, isWandering: true);
            marco.ChallengeQuote = "Dito sa West Tambayan Alley — mainit ang laban ng Teks!";
            marco.AmbientCallouts = new[] { "West Alley Teks!", "125 Coins tara!", "Luksong Tinik jump!" };

            var ella = CreateOpponent("AI Ella", AIDifficulty.Easy, "sorbetes_bot", 2, 75,
                new Vector3(-25.4f, 0f, 0.8f), Quaternion.Euler(0f, 75f, 0f),
                CharacterOutfitRole.FemaleKid,
                new Color(0.28f, 0.74f, 0.94f), new Color(0.96f, 0.46f, 0.68f), new Color(1.0f, 0.92f, 0.45f),
                isSeated: false, isWandering: true);
            ella.ChallengeQuote = "Tara dito sa Waiting Shed! Friendly Teks, 75 Coins!";
            ella.AmbientCallouts = new[] { "Waiting Shed Teks!", "Sorbetes Bot freeze!", "75 Coins tara!" };

            // 9. SOUTH-EAST FIESTA TEKS ARENA ELITE CHALLENGERS
            var ronnie = CreateOpponent("Kuya Ronnie", AIDifficulty.Hard, "rizal_vanguard", 4, 270,
                new Vector3(19.6f, 0f, -18.5f), Quaternion.Euler(0f, 65f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.16f, 0.34f, 0.78f), new Color(0.86f, 0.20f, 0.22f), new Color(1.0f, 0.86f, 0.24f),
                isSeated: false, isWandering: false);
            ronnie.ChallengeQuote = "Welcome sa Fiesta Teks Arena! Subukan mo Manila Vanguard ko — 270 Coins!";
            ronnie.AmbientCallouts = new[] { "Manila Vanguard!", "270 Coins Fiesta Match!", "Fiesta Arena Challenger!" };

            var macky = CreateOpponent("Boss Macky", AIDifficulty.Hard, "minokawa", 4, 290,
                new Vector3(23.2f, 0f, -15.6f), Quaternion.Euler(0f, -165f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(0.36f, 0.14f, 0.58f), new Color(0.88f, 0.30f, 0.14f), new Color(1.0f, 0.80f, 0.20f),
                isSeated: false, isWandering: false);
            macky.ChallengeQuote = "Eclipse ng Minokawa ang haharapin mo sa Fiesta Arena — 290 Coins!";
            macky.AmbientCallouts = new[] { "Minokawa Eclipse!", "290 Coins — palag?", "Fiesta Semi-Finalist!" };
            macky.WinBanter = "Haha! Eclipse ka ng Minokawa ko!";
            macky.LoseBanter = "Solid ng Pamato mo! Harapin mo si Fiesta Champ Rico!";

            var rico = CreateOpponent("Fiesta Champ Rico", AIDifficulty.Boss, "sun_dragon_hero", 5, 750,
                new Vector3(26.8f, 0f, -19.0f), Quaternion.Euler(0f, -65f, 0f),
                CharacterOutfitRole.MaleKid,
                new Color(1.0f, 0.68f, 0.12f), new Color(0.12f, 0.64f, 0.92f), new Color(1.0f, 0.90f, 0.28f),
                isSeated: false, isWandering: false);
            rico.ChallengeQuote = "Ako ang Fiesta Teks Champion! Pusta 750 Coins laban sa Sun Dragon Hero ko!";
            rico.AmbientCallouts = new[] { "FIESTA CHAMPION!", "750 Coins Grand Prize!", "Sun Dragon Hero Pamato!" };
            rico.WinBanter = "Hindi pa sapat ang lakas mo para sa Fiesta Trophy!";
            rico.LoseBanter = "MABUHAY! Ikaw ang bagong Barangay Fiesta Teks Champion!!";
        }

        private NpcOpponent CreateOpponent(
            string npcName,
            AIDifficulty difficulty,
            string favCardId,
            int starRank,
            int wager,
            Vector3 pos,
            Quaternion rot,
            CharacterOutfitRole role,
            Color shirt,
            Color shorts,
            Color slippers,
            bool isSeated,
            bool isWandering)
        {
            var go = new GameObject(npcName.Replace(" ", "_"));
            go.transform.SetParent(CourtRoot, false);
            go.transform.position = pos;
            go.transform.rotation = rot;

            var actor = CharacterVisualBuilder.BuildCharacterVisual(
                go, role, shirt, shorts, new Color(0.76f, 0.52f, 0.37f), slippers);

            if (isSeated)
            {
                actor.SetPose(CharacterPoseMode.SittingOnCourt);
            }

            var npc = go.AddComponent<NpcOpponent>();
            npc.NpcName = npcName;
            npc.Difficulty = difficulty;
            npc.FavoriteCardId = favCardId;
            npc.CardStarRank = starRank;
            npc.BaseCoinWager = wager;
            npc.IsSeated = isSeated;
            npc.IsWandering = isWandering;
            npc.WanderCenter = pos;
            npc.WanderRadius = 2.4f;

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.8f, 0f);
            col.radius = 0.32f;
            col.height = 1.6f;

            SpawnedOpponents.Add(npc);
            return npc;
        }

        private void CreateSpectatorKid(string name, Vector3 pos, Quaternion rot, CharacterOutfitRole role, Color shirt, Color shorts)
        {
            var go = new GameObject(name);
            go.transform.SetParent(CourtRoot, false);
            go.transform.position = pos;
            go.transform.rotation = rot;

            var actor = CharacterVisualBuilder.BuildCharacterVisual(
                go, role, shirt, shorts, new Color(0.74f, 0.50f, 0.36f), new Color(0.24f, 0.52f, 0.78f));
            actor.SetPose(CharacterPoseMode.SittingOnCourt);
        }

        private TextMesh CreateWorldText(string name, Transform parent, Vector3 localPos, string text, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * 0.1f;

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = fontSize;
            tm.characterSize = 0.85f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;
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
