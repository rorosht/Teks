using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TeksMaster.Audio;
using TeksMaster.Core;
using TeksMaster.Visuals;

namespace TeksMaster.World
{
    public class HiddenCardCollectible : MonoBehaviour
    {
        private const float VanishDuration = 0.65f;
        private const float BaseCardHeight = 0.24f;

        private static readonly List<HiddenCardCollectible> _allInstances = new List<HiddenCardCollectible>();
        public static IReadOnlyList<HiddenCardCollectible> AllInstances => _allInstances;
        public static event Action<HiddenCardCollectible, TeksCardDefinition, int, int> OnHiddenCardCollected;

        private static Mesh _beaconRingMesh;
        private static Mesh _diamondHaloMesh;

        public string HiddenId { get; private set; }
        public string CardId { get; private set; }
        public BarangayAreaId AreaId { get; private set; }
        public string SpotLabel { get; private set; }
        public int CoinBonus { get; private set; }
        public int GemBonus { get; private set; }
        public bool IsCollected { get; private set; }
        public bool CanCollect => !IsCollected && !_isVanishing && gameObject.activeInHierarchy;
        public TeksCardDefinition CardDefinition => TeksDatabase.GetCard(CardId);

        private GameObject _visualRoot;
        private Transform _cardPivot;
        private Transform _beaconRing;
        private Transform _haloPivot;

        private float _phaseOffset;
        private bool _isVanishing;
        private float _vanishTimer;
        private Vector3 _vanishStartCardLocalPos;
        private Vector3 _vanishStartRingScale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _allInstances.Clear();
            OnHiddenCardCollected = null;
            _beaconRingMesh = null;
            _diamondHaloMesh = null;
        }

        private void OnEnable()
        {
            if (!_allInstances.Contains(this))
            {
                _allInstances.Add(this);
            }
        }

        private void OnDisable()
        {
            _allInstances.Remove(this);
        }

        private void OnDestroy()
        {
            _allInstances.Remove(this);
        }

        public void Initialize(
            string hiddenId,
            string cardId,
            BarangayAreaId areaId,
            string spotLabel,
            int coinBonus = 50,
            int gemBonus = 5)
        {
            HiddenId = string.IsNullOrWhiteSpace(hiddenId) ? gameObject.name : hiddenId.Trim();
            CardId = cardId;
            AreaId = areaId;
            SpotLabel = string.IsNullOrWhiteSpace(spotLabel) ? "Secret Barangay Spot" : spotLabel.Trim();
            CoinBonus = Mathf.Max(0, coinBonus);
            GemBonus = Mathf.Max(0, gemBonus);
            _phaseOffset = Mathf.Abs(HiddenId.GetHashCode() % 628) * 0.01f;
            _isVanishing = false;
            _vanishTimer = 0f;

            BuildVisuals();
            RefreshCollectedState();
        }

        public void Configure(
            string hiddenId,
            string cardId,
            BarangayAreaId areaId,
            string spotLabel,
            int coinBonus = 50,
            int gemBonus = 5)
        {
            Initialize(hiddenId, cardId, areaId, spotLabel, coinBonus, gemBonus);
        }

        public void SetCollectedVisual(bool collected)
        {
            IsCollected = collected;
            _isVanishing = false;
            _vanishTimer = 0f;

            if (_visualRoot != null)
            {
                _visualRoot.SetActive(!collected);
            }

            if (!collected)
            {
                if (_cardPivot != null)
                {
                    _cardPivot.localPosition = new Vector3(0f, BaseCardHeight, 0f);
                    _cardPivot.localScale = Vector3.one;
                }
                if (_beaconRing != null)
                {
                    _beaconRing.localScale = Vector3.one;
                }
            }
        }

        public void RefreshCollectedState()
        {
            bool discovered = PlayerInventory.Instance != null && PlayerInventory.Instance.IsHiddenCardDiscovered(HiddenId);
            SetCollectedVisual(discovered);
        }

        public bool Collect()
        {
            return TryCollect();
        }

        public bool TryCollect()
        {
            if (!Application.isPlaying || IsCollected || _isVanishing)
            {
                return false;
            }

            var inv = PlayerInventory.Instance;
            TeksCardDefinition grantedCard = CardDefinition;

            if (inv != null)
            {
                if (!inv.DiscoverHiddenCard(HiddenId, CardId, AreaId, CoinBonus, GemBonus, out grantedCard))
                {
                    RefreshCollectedState();
                    return false;
                }
            }

            if (grantedCard == null)
            {
                grantedCard = CardDefinition;
            }

            IsCollected = true;
            _isVanishing = true;
            _vanishTimer = 0f;
            _vanishStartCardLocalPos = _cardPivot != null ? _cardPivot.localPosition : new Vector3(0f, BaseCardHeight, 0f);
            _vanishStartRingScale = _beaconRing != null ? _beaconRing.localScale : Vector3.one;

            TeksAudioManager.Instance?.PlayHiddenCardFound();

            CardRarity vfxRarity = (grantedCard != null && grantedCard.Rarity >= CardRarity.Rare)
                ? grantedCard.Rarity
                : CardRarity.Rare;
            CardVfxHelper.SpawnLandingVfx(transform, vfxRarity);

            OnHiddenCardCollected?.Invoke(this, grantedCard, CoinBonus, GemBonus);
            return true;
        }

        public string GetPromptText()
        {
            return $"[E] PICK UP HIDDEN CARD — {SpotLabel.ToUpperInvariant()}";
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (_isVanishing)
            {
                _vanishTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_vanishTimer / VanishDuration);
                float easeOut = 1f - (1f - t) * (1f - t);

                if (_cardPivot != null)
                {
                    _cardPivot.localPosition = _vanishStartCardLocalPos + Vector3.up * (1.25f * easeOut);
                    _cardPivot.Rotate(0f, 720f * Time.deltaTime, 0f, Space.Self);
                    float scaleFactor = Mathf.Lerp(1.15f, 0.01f, t * t);
                    _cardPivot.localScale = Vector3.one * scaleFactor;
                }

                if (_beaconRing != null)
                {
                    float baseRing = Mathf.Max(0.5f, _vanishStartRingScale.x);
                    float ringScale = Mathf.Lerp(baseRing, baseRing * 1.55f, easeOut) * (1f - t * 0.92f);
                    _beaconRing.localScale = new Vector3(ringScale, 1f, ringScale);
                }

                if (t >= 1f)
                {
                    _isVanishing = false;
                    if (_visualRoot != null)
                    {
                        _visualRoot.SetActive(false);
                    }
                }
                return;
            }

            if (IsCollected || _visualRoot == null || !_visualRoot.activeSelf) return;

            float time = Time.time;
            if (_cardPivot != null)
            {
                float bob = Mathf.Sin(time * 2.6f + _phaseOffset) * 0.08f;
                _cardPivot.localPosition = new Vector3(0f, BaseCardHeight + bob, 0f);
                _cardPivot.localRotation = Quaternion.Euler(12f, (time * 72f + _phaseOffset * 57.3f) % 360f, 0f);
            }

            if (_haloPivot != null)
            {
                _haloPivot.Rotate(0f, -110f * Time.deltaTime, 0f, Space.Self);
            }

            if (_beaconRing != null)
            {
                float pulse = 0.92f + 0.14f * Mathf.Sin(time * 3.4f + _phaseOffset);
                _beaconRing.localScale = new Vector3(pulse, 1f, pulse);
            }
        }

        private void BuildVisuals()
        {
            if (_visualRoot != null)
            {
                SafeDestroy(_visualRoot);
                _visualRoot = null;
            }

            _visualRoot = new GameObject("HiddenCardVisuals");
            _visualRoot.transform.SetParent(transform, false);

            TeksCardDefinition def = CardDefinition;
            Color glowColor = GetBeaconGlowColor(def != null ? def.Rarity : CardRarity.Rare);

            // Bobbing & rotating miniature 3D Teks card
            var pivotGo = new GameObject("CardPivot");
            pivotGo.transform.SetParent(_visualRoot.transform, false);
            pivotGo.transform.localPosition = new Vector3(0f, BaseCardHeight, 0f);
            _cardPivot = pivotGo.transform;

            var cardCore = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cardCore.name = "CardCore";
            cardCore.transform.SetParent(_cardPivot, false);
            cardCore.transform.localScale = new Vector3(0.28f, 0.39f, 0.014f);
            SafeDestroy(cardCore.GetComponent<Collider>());
            cardCore.GetComponent<Renderer>().sharedMaterial = CardArtGenerator.CreateLitMaterial(
                Color.Lerp(glowColor, new Color(0.98f, 0.86f, 0.32f), 0.45f), 0.35f, 0.25f);

            Texture2D frontTex = def != null
                ? CardArtGenerator.GetCardPortraitTexture(def, 3)
                : CardArtGenerator.GetCardBackTexture();
            Texture2D backTex = CardArtGenerator.GetCardBackTexture();

            var frontFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frontFace.name = "FrontFace";
            frontFace.transform.SetParent(_cardPivot, false);
            frontFace.transform.localPosition = new Vector3(0f, 0f, -0.0085f);
            frontFace.transform.localScale = new Vector3(0.255f, 0.36f, 1f);
            SafeDestroy(frontFace.GetComponent<Collider>());
            frontFace.GetComponent<Renderer>().sharedMaterial = CardArtGenerator.CreateLitMaterial(Color.white, 0.28f, 0f, frontTex);

            var backFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backFace.name = "BackFace";
            backFace.transform.SetParent(_cardPivot, false);
            backFace.transform.localPosition = new Vector3(0f, 0f, 0.0085f);
            backFace.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            backFace.transform.localScale = new Vector3(0.255f, 0.36f, 1f);
            SafeDestroy(backFace.GetComponent<Collider>());
            backFace.GetComponent<Renderer>().sharedMaterial = CardArtGenerator.CreateLitMaterial(Color.white, 0.16f, 0f, backTex);

            // Glowing floating diamond halo above the card
            var haloGo = new GameObject("DiamondHalo");
            haloGo.transform.SetParent(_cardPivot, false);
            haloGo.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            haloGo.transform.localScale = new Vector3(0.085f, 0.105f, 0.085f);
            var haloMf = haloGo.AddComponent<MeshFilter>();
            haloMf.sharedMesh = GetDiamondHaloMesh();
            var haloMr = haloGo.AddComponent<MeshRenderer>();
            haloMr.sharedMaterial = CardArtGenerator.CreateUnlitEmissiveMaterial(glowColor, 2.25f);
            haloMr.shadowCastingMode = ShadowCastingMode.Off;
            haloMr.receiveShadows = false;
            _haloPivot = haloGo.transform;

            // Ground emissive beacon ring (24-segment annulus sitting cleanly above floor)
            var ringGo = new GameObject("BeaconRing");
            ringGo.transform.SetParent(_visualRoot.transform, false);
            float groundLocalY = Mathf.Clamp(0.024f - transform.position.y, -0.35f, 0.03f);
            ringGo.transform.localPosition = new Vector3(0f, groundLocalY, 0f);
            var ringMf = ringGo.AddComponent<MeshFilter>();
            ringMf.sharedMesh = GetBeaconRingMesh();
            var ringMr = ringGo.AddComponent<MeshRenderer>();
            ringMr.sharedMaterial = CardArtGenerator.CreateUnlitEmissiveMaterial(glowColor, 1.95f);
            ringMr.shadowCastingMode = ShadowCastingMode.Off;
            ringMr.receiveShadows = false;
            _beaconRing = ringGo.transform;
        }

        private static Color GetBeaconGlowColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Legendary:
                    return new Color(1.0f, 0.84f, 0.18f);
                case CardRarity.Epic:
                    return new Color(0.82f, 0.34f, 1.0f);
                case CardRarity.Rare:
                    return new Color(0.20f, 0.90f, 1.0f);
                case CardRarity.Uncommon:
                    return new Color(0.32f, 0.95f, 0.56f);
                default:
                    return new Color(1.0f, 0.88f, 0.34f);
            }
        }

        private static Mesh GetBeaconRingMesh()
        {
            if (_beaconRingMesh != null) return _beaconRingMesh;

            const int segments = 24;
            const float innerR = 0.36f;
            const float outerR = 0.48f;

            var mesh = new Mesh { name = "HiddenCard_BeaconRing24" };
            var verts = new Vector3[segments * 2];
            var normals = new Vector3[segments * 2];
            var uvs = new Vector2[segments * 2];
            var tris = new int[segments * 6];

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                int vi = i * 2;
                verts[vi] = new Vector3(cos * innerR, 0f, sin * innerR);
                verts[vi + 1] = new Vector3(cos * outerR, 0f, sin * outerR);
                normals[vi] = Vector3.up;
                normals[vi + 1] = Vector3.up;
                uvs[vi] = new Vector2(i / (float)segments, 0f);
                uvs[vi + 1] = new Vector2(i / (float)segments, 1f);

                int next = ((i + 1) % segments) * 2;
                int ti = i * 6;
                tris[ti] = vi;
                tris[ti + 1] = next + 1;
                tris[ti + 2] = vi + 1;

                tris[ti + 3] = vi;
                tris[ti + 4] = next;
                tris[ti + 5] = next + 1;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            _beaconRingMesh = mesh;
            return _beaconRingMesh;
        }

        private static Mesh GetDiamondHaloMesh()
        {
            if (_diamondHaloMesh != null) return _diamondHaloMesh;

            var mesh = new Mesh { name = "HiddenCard_DiamondHalo" };
            Vector3[] verts = new Vector3[]
            {
                new Vector3(0f, 1.0f, 0f),
                new Vector3(0f, -1.0f, 0f),
                new Vector3(0.62f, 0f, 0f),
                new Vector3(0f, 0f, 0.62f),
                new Vector3(-0.62f, 0f, 0f),
                new Vector3(0f, 0f, -0.62f)
            };

            int[] tris = new int[]
            {
                0, 3, 2,
                0, 4, 3,
                0, 5, 4,
                0, 2, 5,
                1, 2, 3,
                1, 3, 4,
                1, 4, 5,
                1, 5, 2
            };

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _diamondHaloMesh = mesh;
            return _diamondHaloMesh;
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }

    public class AreaGateBarrier : MonoBehaviour
    {
        private static readonly List<AreaGateBarrier> _allBarriers = new List<AreaGateBarrier>();
        public static IReadOnlyList<AreaGateBarrier> AllBarriers => _allBarriers;

        public BarangayAreaId AreaId { get; private set; }
        public string GateTitle { get; private set; }
        public string UnlockHint { get; private set; }
        public bool IsLocked { get; private set; } = true;

        private BoxCollider _blockingCollider;
        private GameObject _visualRoot;
        private Transform _lockEmblemPivot;
        private TextMesh _frontSignText;
        private float _width = 6.0f;
        private float _height = 2.4f;
        private bool _initialized;
        private PlayerInventory _subscribedInventory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _allBarriers.Clear();
        }

        private void OnEnable()
        {
            if (!_allBarriers.Contains(this))
            {
                _allBarriers.Add(this);
            }

            if (Application.isPlaying)
            {
                SubscribeToInventory();
                if (_initialized)
                {
                    RefreshLockState();
                }
            }
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            SubscribeToInventory();
            if (_initialized)
            {
                RefreshLockState();
            }
        }

        private void OnDisable()
        {
            _allBarriers.Remove(this);
            UnsubscribeFromInventory();
        }

        private void OnDestroy()
        {
            _allBarriers.Remove(this);
            UnsubscribeFromInventory();
        }

        public void Initialize(
            BarangayAreaId areaId,
            string gateTitle,
            string unlockHint = null,
            float width = 6.0f,
            float height = 2.4f)
        {
            AreaId = areaId;
            GateTitle = string.IsNullOrWhiteSpace(gateTitle)
                ? BarangayCourtBuilder.GetAreaDisplayName(areaId)
                : gateTitle.Trim();
            UnlockHint = !string.IsNullOrWhiteSpace(unlockHint)
                ? unlockHint.Trim()
                : ResolveDefaultUnlockHint(areaId);
            _width = Mathf.Max(2.0f, width);
            _height = Mathf.Max(1.6f, height);
            _initialized = true;

            EnsureBlockingCollider();
            BuildGateVisuals();

            if (Application.isPlaying)
            {
                SubscribeToInventory();
            }

            RefreshLockState();
        }

        public void RefreshLockState()
        {
            if (PlayerInventory.Instance != null)
            {
                UnlockHint = ResolveDefaultUnlockHint(AreaId);
            }
            else if (string.IsNullOrWhiteSpace(UnlockHint))
            {
                UnlockHint = ResolveDefaultUnlockHint(AreaId);
            }

            UpdateSignText();

            bool unlocked;
            if (PlayerInventory.Instance != null)
            {
                unlocked = PlayerInventory.Instance.IsAreaUnlocked(AreaId);
            }
            else
            {
                unlocked = AreaId == BarangayAreaId.PlayersNeighborhood || AreaId == BarangayAreaId.SariSariAndKalye;
            }

            SetLocked(!unlocked);
        }

        private void UpdateSignText()
        {
            string wrappedHint = WrapHintText(UnlockHint, 26);
            string boardCopy = $"LOCKED: {GateTitle.ToUpperInvariant()}\n{wrappedHint}";
            if (_frontSignText != null && _frontSignText.text != boardCopy)
            {
                _frontSignText.text = boardCopy;
            }
        }

        public void SetLocked(bool locked)
        {
            IsLocked = locked;

            // Keep root GameObject active at all times so OnAreasChanged stays subscribed
            // even when toggling Free Play Unlock All ON -> OFF.
            if (_blockingCollider != null)
            {
                _blockingCollider.enabled = locked;
            }

            if (_visualRoot != null)
            {
                _visualRoot.SetActive(locked);
            }
        }

        public float GetDistanceTo(Vector3 worldPos)
        {
            if (!IsLocked || !gameObject.activeInHierarchy)
            {
                return float.MaxValue;
            }

            Vector3 closest;
            if (_blockingCollider != null && _blockingCollider.enabled)
            {
                Vector3 samplePoint = new Vector3(worldPos.x, _blockingCollider.bounds.center.y, worldPos.z);
                closest = _blockingCollider.ClosestPoint(samplePoint);
            }
            else
            {
                closest = transform.position;
            }

            float dx = worldPos.x - closest.x;
            float dz = worldPos.z - closest.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public string GetPromptText()
        {
            string hint = string.IsNullOrWhiteSpace(UnlockHint) ? ResolveDefaultUnlockHint(AreaId) : UnlockHint;
            return $"[LOCKED] {GateTitle.ToUpperInvariant()} — {hint}";
        }

        private void Update()
        {
            if (!Application.isPlaying || !IsLocked || _lockEmblemPivot == null) return;

            float pulse = 1.0f + 0.08f * Mathf.Sin(Time.time * 3.2f);
            _lockEmblemPivot.localScale = Vector3.one * pulse;
        }

        private void SubscribeToInventory()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null || _subscribedInventory == inv) return;

            UnsubscribeFromInventory();
            _subscribedInventory = inv;
            _subscribedInventory.OnAreasChanged += HandleAreasChanged;
            _subscribedInventory.OnAreaUnlocked += HandleAreaUnlocked;
        }

        private void UnsubscribeFromInventory()
        {
            if (_subscribedInventory == null) return;
            _subscribedInventory.OnAreasChanged -= HandleAreasChanged;
            _subscribedInventory.OnAreaUnlocked -= HandleAreaUnlocked;
            _subscribedInventory = null;
        }

        private void HandleAreasChanged()
        {
            RefreshLockState();
        }

        private void HandleAreaUnlocked(BarangayAreaId unlockedArea)
        {
            if (unlockedArea == AreaId)
            {
                RefreshLockState();
            }
        }

        private void EnsureBlockingCollider()
        {
            _blockingCollider = GetComponent<BoxCollider>();
            if (_blockingCollider == null)
            {
                _blockingCollider = gameObject.AddComponent<BoxCollider>();
            }
            _blockingCollider.center = new Vector3(0f, _height * 0.5f, 0f);
            _blockingCollider.size = new Vector3(_width, _height, 0.72f);
        }

        private void BuildGateVisuals()
        {
            if (_visualRoot != null)
            {
                SafeDestroy(_visualRoot);
                _visualRoot = null;
            }

            _visualRoot = new GameObject("BarrierVisuals");
            _visualRoot.transform.SetParent(transform, false);

            Material postMat = CardArtGenerator.CreateLitMaterial(new Color(0.24f, 0.26f, 0.30f), 0.35f, 0.45f);
            Material hazardYellowMat = CardArtGenerator.CreateLitMaterial(new Color(0.98f, 0.78f, 0.12f), 0.25f);
            Material hazardDarkMat = CardArtGenerator.CreateLitMaterial(new Color(0.12f, 0.12f, 0.14f), 0.20f);
            Material signBoardMat = CardArtGenerator.CreateLitMaterial(new Color(0.13f, 0.15f, 0.20f), 0.18f);
            Material signTrimMat = CardArtGenerator.CreateLitMaterial(new Color(0.96f, 0.56f, 0.14f), 0.30f);
            Material lockGlowMat = CardArtGenerator.CreateUnlitEmissiveMaterial(new Color(1.0f, 0.54f, 0.14f), 2.15f);
            Material shackleGlowMat = CardArtGenerator.CreateUnlitEmissiveMaterial(new Color(1.0f, 0.88f, 0.32f), 2.0f);

            float halfW = _width * 0.5f;

            // Left and right barricade posts
            CreateVisualCylinder("PostLeft", _visualRoot.transform, new Vector3(-halfW + 0.12f, _height * 0.5f, 0f),
                new Vector3(0.14f, _height * 0.5f, 0.14f), postMat);
            CreateVisualCylinder("PostRight", _visualRoot.transform, new Vector3(halfW - 0.12f, _height * 0.5f, 0f),
                new Vector3(0.14f, _height * 0.5f, 0.14f), postMat);

            // Two striped barricade rails across the span
            float[] railHeights = { 0.68f, 1.32f };
            float spanWidth = Mathf.Max(1.0f, _width - 0.32f);
            int stripeCount = Mathf.Clamp(Mathf.RoundToInt(spanWidth / 0.52f), 4, 16);
            float segWidth = spanWidth / stripeCount;

            for (int r = 0; r < railHeights.Length; r++)
            {
                float railY = railHeights[r];
                for (int s = 0; s < stripeCount; s++)
                {
                    float x = -spanWidth * 0.5f + segWidth * (s + 0.5f);
                    Material segMat = ((s + r) % 2 == 0) ? hazardYellowMat : hazardDarkMat;
                    CreateVisualBox($"Rail_{r}_Stripe_{s}", _visualRoot.transform, new Vector3(x, railY, 0f),
                        new Vector3(segWidth + 0.005f, 0.18f, 0.12f), segMat);
                }
            }

            // Central front-only signboard
            float signY = Mathf.Min(_height - 0.38f, 1.88f);
            float signW = Mathf.Clamp(_width * 0.66f, 2.4f, 4.1f);
            CreateVisualBox("SignFrame", _visualRoot.transform, new Vector3(0f, signY, 0f),
                new Vector3(signW + 0.14f, 0.96f, 0.10f), signTrimMat);
            CreateVisualBox("SignBoard", _visualRoot.transform, new Vector3(0f, signY, 0f),
                new Vector3(signW, 0.84f, 0.14f), signBoardMat);

            string wrappedHint = WrapHintText(UnlockHint, 26);
            string boardCopy = $"LOCKED: {GateTitle.ToUpperInvariant()}\n{wrappedHint}";

            _frontSignText = CreateGateSignText("SignTextFront", _visualRoot.transform,
                new Vector3(0f, signY, -0.09f), Quaternion.identity, boardCopy);

            // Glowing emissive lock emblem above the signboard
            var lockPivot = new GameObject("LockEmblem");
            lockPivot.transform.SetParent(_visualRoot.transform, false);
            lockPivot.transform.localPosition = new Vector3(0f, signY + 0.70f, 0f);
            _lockEmblemPivot = lockPivot.transform;

            CreateVisualBox("PadlockBody", _lockEmblemPivot, new Vector3(0f, -0.06f, 0f),
                new Vector3(0.38f, 0.32f, 0.16f), lockGlowMat);
            CreateVisualBox("PadlockShackleTop", _lockEmblemPivot, new Vector3(0f, 0.22f, 0f),
                new Vector3(0.24f, 0.06f, 0.08f), shackleGlowMat);
            CreateVisualBox("PadlockShackleLeft", _lockEmblemPivot, new Vector3(-0.10f, 0.13f, 0f),
                new Vector3(0.06f, 0.18f, 0.08f), shackleGlowMat);
            CreateVisualBox("PadlockShackleRight", _lockEmblemPivot, new Vector3(0.10f, 0.13f, 0f),
                new Vector3(0.06f, 0.18f, 0.08f), shackleGlowMat);
        }

        private static TextMesh CreateGateSignText(string name, Transform parent, Vector3 localPos, Quaternion localRot, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = Vector3.one * 0.11f;

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 52;
            tm.characterSize = 0.20f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1.0f, 0.92f, 0.38f);
            tm.fontStyle = FontStyle.Bold;
            go.AddComponent<FrontOnlySignText>();
            return tm;
        }

        private static string WrapHintText(string hint, int maxLineChars)
        {
            if (string.IsNullOrWhiteSpace(hint) || hint.Length <= maxLineChars)
            {
                return hint ?? string.Empty;
            }

            string[] words = hint.Split(' ');
            string line = "";
            string result = "";
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];
                if (line.Length + w.Length + 1 > maxLineChars && line.Length > 0)
                {
                    result = string.IsNullOrEmpty(result) ? line : result + "\n" + line;
                    line = w;
                }
                else
                {
                    line = string.IsNullOrEmpty(line) ? w : line + " " + w;
                }
            }
            if (!string.IsNullOrEmpty(line))
            {
                result = string.IsNullOrEmpty(result) ? line : result + "\n" + line;
            }
            return result;
        }

        private static string ResolveDefaultUnlockHint(BarangayAreaId area)
        {
            if (PlayerInventory.Instance != null)
            {
                return PlayerInventory.Instance.GetAreaUnlockRequirementText(area);
            }

            switch (area)
            {
                case BarangayAreaId.BasketballCourt:
                    return "Defeat Dodong & Mark (or win 2 Starter Area matches)";
                case BarangayAreaId.OldNeighborhood:
                    return "Defeat Carlo (or win 2 Basketball Court matches)";
                case BarangayAreaId.FiestaArena:
                    return "Defeat Joshua (or win 2 Old Neighborhood matches)";
                default:
                    return "Unlocked Area";
            }
        }

        private static GameObject CreateVisualBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            SafeDestroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject CreateVisualCylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            SafeDestroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }

    /// <summary>
    /// Ensures Unity built-in TextMesh signage is strictly visible from the front (-transform.forward)
    /// and hidden whenever the camera views it from behind.
    /// </summary>
    public class FrontOnlySignText : MonoBehaviour
    {
        private Renderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        private void LateUpdate()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            if (_renderer == null || Camera.main == null) return;

            Vector3 toCam = Camera.main.transform.position - transform.position;
            bool visibleFromFront = Vector3.Dot(-transform.forward, toCam) > 0.02f;
            if (_renderer.enabled != visibleFromFront)
            {
                _renderer.enabled = visibleFromFront;
            }
        }
    }
}
