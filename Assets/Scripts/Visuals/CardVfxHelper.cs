using UnityEngine;
using UnityEngine.Rendering;
using TeksMaster.Core;

namespace TeksMaster.Visuals
{
    public static class CardVfxHelper
    {
        private static Mesh _sparkDiamondMesh;
        private static Mesh _ringAnnulusMesh;
        private static Mesh _lightPillarMesh;

        private static Material _rareSparkMat;
        private static Material _epicSparkMat;
        private static Material _legendarySparkMat;

        private static Material _rareRingMat;
        private static Material _epicRingMat;
        private static Material _legendaryRingMat;
        private static Material _legendaryPillarMat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches()
        {
            _sparkDiamondMesh = null;
            _ringAnnulusMesh = null;
            _lightPillarMesh = null;

            _rareSparkMat = null;
            _epicSparkMat = null;
            _legendarySparkMat = null;

            _rareRingMat = null;
            _epicRingMat = null;
            _legendaryRingMat = null;
            _legendaryPillarMat = null;
        }

        public static GameObject SpawnLandingVfx(Transform cardTransform, CardRarity rarity)
        {
            if (!Application.isPlaying || cardTransform == null) return null;
            if (rarity != CardRarity.Rare && rarity != CardRarity.Epic && rarity != CardRarity.Legendary)
            {
                return null;
            }

            var vfxRoot = new GameObject($"CardLandingVfx_{rarity}");
            vfxRoot.transform.SetParent(cardTransform, true);

            // Force world-upright placement & rotation so Face-Down cards (roll = 180 deg) still emit upward (+Y)
            Vector3 cardPos = cardTransform.position;
            vfxRoot.transform.position = new Vector3(cardPos.x, 0.016f, cardPos.z);
            vfxRoot.transform.rotation = Quaternion.Euler(0f, cardTransform.eulerAngles.y, 0f);

            // Neutralize any non-unit parent scale safely
            Vector3 parentScale = cardTransform.lossyScale;
            float invX = Mathf.Abs(parentScale.x) > 0.001f ? 1f / parentScale.x : 1f;
            float invY = Mathf.Abs(parentScale.y) > 0.001f ? 1f / parentScale.y : 1f;
            float invZ = Mathf.Abs(parentScale.z) > 0.001f ? 1f / parentScale.z : 1f;
            vfxRoot.transform.localScale = new Vector3(invX, invY, invZ);

            Color primaryColor;
            float hdrIntensity;
            int burstCount;
            float minSpeed;
            float maxSpeed;
            float minLife;
            float maxLife;
            float coneAngle;
            float totalCleanupDelay;

            switch (rarity)
            {
                case CardRarity.Rare:
                    primaryColor = new Color(0.18f, 0.88f, 1.0f);
                    hdrIntensity = 1.80f;
                    burstCount = 22;
                    minSpeed = 1.15f;
                    maxSpeed = 2.40f;
                    minLife = 0.55f;
                    maxLife = 0.85f;
                    coneAngle = 38f;
                    totalCleanupDelay = 1.35f;
                    break;
                case CardRarity.Epic:
                    primaryColor = new Color(0.76f, 0.30f, 1.0f);
                    hdrIntensity = 2.25f;
                    burstCount = 34;
                    minSpeed = 1.40f;
                    maxSpeed = 3.10f;
                    minLife = 0.65f;
                    maxLife = 1.05f;
                    coneAngle = 46f;
                    totalCleanupDelay = 1.65f;
                    break;
                default: // Legendary
                    primaryColor = new Color(1.0f, 0.84f, 0.18f);
                    hdrIntensity = 2.80f;
                    burstCount = 50;
                    minSpeed = 1.65f;
                    maxSpeed = 3.85f;
                    minLife = 0.80f;
                    maxLife = 1.30f;
                    coneAngle = 52f;
                    totalCleanupDelay = 2.15f;
                    break;
            }

            CreateSparkBurst(
                vfxRoot.transform,
                rarity,
                burstCount,
                minSpeed,
                maxSpeed,
                minLife,
                maxLife,
                coneAngle);

            if (rarity == CardRarity.Rare)
            {
                SpawnExpandingRing(vfxRoot.transform, "RareGlowRing", GetRingMaterial(rarity), primaryColor, hdrIntensity, 0.18f, 0.72f, 0.65f, 0f, 0.004f);
            }
            else if (rarity == CardRarity.Epic)
            {
                Color accentColor = new Color(0.95f, 0.50f, 1.0f);
                SpawnExpandingRing(vfxRoot.transform, "EpicShockRing_Outer", GetRingMaterial(rarity), primaryColor, hdrIntensity, 0.18f, 0.94f, 0.78f, 0f, 0.004f);
                SpawnExpandingRing(vfxRoot.transform, "EpicShockRing_Inner", GetRingMaterial(rarity), accentColor, hdrIntensity * 0.9f, 0.14f, 0.70f, 0.64f, 0.12f, 0.009f);
            }
            else
            {
                Color brightGold = new Color(1.0f, 0.95f, 0.55f);
                SpawnExpandingRing(vfxRoot.transform, "LegendaryRing_Outer", GetRingMaterial(rarity), primaryColor, hdrIntensity, 0.20f, 1.18f, 0.92f, 0f, 0.004f);
                SpawnExpandingRing(vfxRoot.transform, "LegendaryRing_Inner", GetRingMaterial(rarity), brightGold, hdrIntensity * 1.05f, 0.16f, 0.88f, 0.76f, 0.14f, 0.010f);
                SpawnLegendaryLightPillar(vfxRoot.transform, primaryColor, hdrIntensity);
            }

            Object.Destroy(vfxRoot, totalCleanupDelay);
            return vfxRoot;
        }

        private static void CreateSparkBurst(
            Transform parent,
            CardRarity rarity,
            int burstCount,
            float minSpeed,
            float maxSpeed,
            float minLife,
            float maxLife,
            float coneAngle)
        {
            var psGo = new GameObject("SparkBurst");
            psGo.transform.SetParent(parent, false);
            psGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            // ParticleSystem cone emits along local +Z; rotate -90 deg on X so it emits straight +Y
            psGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var ps = psGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = maxLife + 0.15f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startSize = rarity == CardRarity.Legendary
                ? new ParticleSystem.MinMaxCurve(0.036f, 0.072f)
                : new ParticleSystem.MinMaxCurve(0.028f, 0.056f);
            main.gravityModifier = rarity == CardRarity.Legendary ? 1.35f : 1.75f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = burstCount + 8;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, (short)burstCount)
            });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = coneAngle;
            shape.radius = 0.15f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.65f, 0.65f),
                new Keyframe(1f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var rotOverLifetime = ps.rotationOverLifetime;
            rotOverLifetime.enabled = true;
            rotOverLifetime.separateAxes = true;
            rotOverLifetime.x = new ParticleSystem.MinMaxCurve(-5f, 5f);
            rotOverLifetime.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
            rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

            var renderer = psGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = GetSparkDiamondMesh();
            renderer.sharedMaterial = GetSparkMaterial(rarity);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Play();
        }

        private static void SpawnExpandingRing(
            Transform parent,
            string ringName,
            Material ringMat,
            Color tintColor,
            float hdrIntensity,
            float startRadius,
            float endRadius,
            float duration,
            float delay,
            float localY)
        {
            var ringGo = new GameObject(ringName);
            ringGo.transform.SetParent(parent, false);
            ringGo.transform.localPosition = new Vector3(0f, localY, 0f);
            ringGo.transform.localRotation = Quaternion.identity;

            var mf = ringGo.AddComponent<MeshFilter>();
            mf.sharedMesh = GetRingAnnulusMesh();

            var mr = ringGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ringMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var anim = ringGo.AddComponent<CardVfxRingAnimator>();
            anim.InitializeRing(mr, tintColor, hdrIntensity, startRadius, endRadius, duration, delay);
        }

        private static void SpawnLegendaryLightPillar(Transform parent, Color goldColor, float hdrIntensity)
        {
            var pillarGo = new GameObject("LegendaryLightPillar");
            pillarGo.transform.SetParent(parent, false);
            pillarGo.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            pillarGo.transform.localRotation = Quaternion.identity;

            var mf = pillarGo.AddComponent<MeshFilter>();
            mf.sharedMesh = GetLightPillarMesh();

            var mr = pillarGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GetLegendaryPillarMaterial();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var anim = pillarGo.AddComponent<CardVfxRingAnimator>();
            anim.InitializePillar(mr, goldColor, hdrIntensity, maxHeight: 2.15f, maxRadius: 0.24f, duration: 1.15f);
        }

        // --- URP-Safe Bundled Emissive Materials (via TeksEmissiveTemplate) ---

        private static Material GetSparkMaterial(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Rare:
                    if (_rareSparkMat == null)
                        _rareSparkMat = CardArtGenerator.CreateUnlitEmissiveMaterial(new Color(0.24f, 0.92f, 1.0f), 1.90f);
                    return _rareSparkMat;
                case CardRarity.Epic:
                    if (_epicSparkMat == null)
                        _epicSparkMat = CardArtGenerator.CreateUnlitEmissiveMaterial(new Color(0.82f, 0.36f, 1.0f), 2.30f);
                    return _epicSparkMat;
                default:
                    if (_legendarySparkMat == null)
                        _legendarySparkMat = CardArtGenerator.CreateUnlitEmissiveMaterial(new Color(1.0f, 0.88f, 0.24f), 2.80f);
                    return _legendarySparkMat;
            }
        }

        private static Material GetRingMaterial(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Rare:
                    if (_rareRingMat == null)
                        _rareRingMat = CardArtGenerator.CreateUnlitEmissiveMaterial(new Color(0.18f, 0.88f, 1.0f), 1.80f);
                    return _rareRingMat;
                case CardRarity.Epic:
                    if (_epicRingMat == null)
                        _epicRingMat = CardArtGenerator.CreateUnlitEmissiveMaterial(new Color(0.76f, 0.30f, 1.0f), 2.25f);
                    return _epicRingMat;
                default:
                    if (_legendaryRingMat == null)
                        _legendaryRingMat = CardArtGenerator.CreateUnlitEmissiveMaterial(new Color(1.0f, 0.84f, 0.18f), 2.80f);
                    return _legendaryRingMat;
            }
        }

        private static Material GetLegendaryPillarMaterial()
        {
            if (_legendaryPillarMat == null)
            {
                _legendaryPillarMat = CardArtGenerator.CreateUnlitEmissiveMaterial(new Color(1.0f, 0.88f, 0.30f), 2.75f);
            }
            return _legendaryPillarMat;
        }

        // --- Cached Procedural Meshes ---

        private static Mesh GetSparkDiamondMesh()
        {
            if (_sparkDiamondMesh != null) return _sparkDiamondMesh;

            var mesh = new Mesh { name = "TeksVfx_SparkDiamond" };
            Vector3[] verts = new Vector3[]
            {
                new Vector3(0f, 1.15f, 0f),   // 0 top
                new Vector3(0f, -1.15f, 0f),  // 1 bottom
                new Vector3(0.65f, 0f, 0f),   // 2 +X
                new Vector3(0f, 0f, 0.65f),   // 3 +Z
                new Vector3(-0.65f, 0f, 0f),  // 4 -X
                new Vector3(0f, 0f, -0.65f)   // 5 -Z
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
            _sparkDiamondMesh = mesh;
            return _sparkDiamondMesh;
        }

        private static Mesh GetRingAnnulusMesh()
        {
            if (_ringAnnulusMesh != null) return _ringAnnulusMesh;

            const int segments = 28;
            const float innerR = 0.78f;
            const float outerR = 1.00f;

            var mesh = new Mesh { name = "TeksVfx_RingAnnulus" };
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
            _ringAnnulusMesh = mesh;
            return _ringAnnulusMesh;
        }

        private static Mesh GetLightPillarMesh()
        {
            if (_lightPillarMesh != null) return _lightPillarMesh;

            const int segments = 18;
            const float bottomR = 0.85f;
            const float topR = 1.15f;

            var mesh = new Mesh { name = "TeksVfx_LightPillar" };
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
                verts[vi] = new Vector3(cos * bottomR, 0f, sin * bottomR);
                verts[vi + 1] = new Vector3(cos * topR, 1f, sin * topR);
                Vector3 outward = new Vector3(cos, 0f, sin);
                normals[vi] = outward;
                normals[vi + 1] = outward;
                uvs[vi] = new Vector2(i / (float)segments, 0f);
                uvs[vi + 1] = new Vector2(i / (float)segments, 1f);

                int next = ((i + 1) % segments) * 2;
                int ti = i * 6;
                tris[ti] = vi;
                tris[ti + 1] = vi + 1;
                tris[ti + 2] = next + 1;

                tris[ti + 3] = vi;
                tris[ti + 4] = next + 1;
                tris[ti + 5] = next;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            _lightPillarMesh = mesh;
            return _lightPillarMesh;
        }
    }

    public class CardVfxRingAnimator : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private Color _baseColor = Color.white;
        private float _hdrIntensity = 2f;
        private float _startRadius = 0.18f;
        private float _endRadius = 0.80f;
        private float _maxHeight = 2.0f;
        private float _maxRadius = 0.24f;
        private float _duration = 0.7f;
        private float _delay;
        private float _elapsed;
        private bool _isPillar;
        private bool _configured;

        public void InitializeRing(
            MeshRenderer meshRenderer,
            Color color,
            float hdrIntensity,
            float startRadius,
            float endRadius,
            float duration,
            float delay)
        {
            _renderer = meshRenderer;
            _baseColor = color;
            _hdrIntensity = hdrIntensity;
            _startRadius = startRadius;
            _endRadius = endRadius;
            _duration = Mathf.Max(0.1f, duration);
            _delay = Mathf.Max(0f, delay);
            _elapsed = 0f;
            _isPillar = false;
            _configured = true;
            _propBlock = new MaterialPropertyBlock();

            if (_renderer != null)
            {
                _renderer.enabled = _delay <= 0.0001f;
            }
            transform.localScale = new Vector3(_startRadius, 1f, _startRadius);
            ApplyBlock(1f);
        }

        public void InitializePillar(
            MeshRenderer meshRenderer,
            Color color,
            float hdrIntensity,
            float maxHeight,
            float maxRadius,
            float duration)
        {
            _renderer = meshRenderer;
            _baseColor = color;
            _hdrIntensity = hdrIntensity;
            _maxHeight = maxHeight;
            _maxRadius = maxRadius;
            _duration = Mathf.Max(0.2f, duration);
            _delay = 0f;
            _elapsed = 0f;
            _isPillar = true;
            _configured = true;
            _propBlock = new MaterialPropertyBlock();

            if (_renderer != null) _renderer.enabled = true;
            transform.localScale = new Vector3(0.04f, 0.15f, 0.04f);
            ApplyBlock(1f);
        }

        private void Update()
        {
            if (!_configured || _renderer == null) return;

            _elapsed += Time.deltaTime;
            if (_elapsed < _delay)
            {
                if (_renderer.enabled) _renderer.enabled = false;
                return;
            }

            float activeTime = _elapsed - _delay;
            float t = Mathf.Clamp01(activeTime / _duration);

            if (t >= 1f)
            {
                _renderer.enabled = false;
                enabled = false;
                return;
            }

            if (!_renderer.enabled) _renderer.enabled = true;

            if (_isPillar)
            {
                UpdatePillar(t);
            }
            else
            {
                UpdateRing(t);
            }
        }

        private void UpdateRing(float t)
        {
            // Ease-out radial expansion, then smooth dissolve taper near the end
            float easeOut = 1f - (1f - t) * (1f - t);
            float radius = Mathf.Lerp(_startRadius, _endRadius, easeOut);
            float fade = t < 0.60f ? 1f : 1f - Mathf.Clamp01((t - 0.60f) / 0.40f);
            float finalScale = radius * (t > 0.82f ? Mathf.Lerp(1f, 0.01f, (t - 0.82f) / 0.18f) : 1f);

            transform.localScale = new Vector3(finalScale, 1f, finalScale);
            ApplyBlock(fade * fade);
        }

        private void UpdatePillar(float t)
        {
            float rise = Mathf.Clamp01(t / 0.18f);
            float height = Mathf.Lerp(0.15f, _maxHeight, 1f - (1f - rise) * (1f - rise));
            float radiusFactor;
            float glow;

            if (t < 0.18f)
            {
                radiusFactor = Mathf.Lerp(0.25f, 1.0f, rise);
                glow = Mathf.Lerp(0.65f, 1.15f, rise);
            }
            else if (t < 0.50f)
            {
                float hold = (t - 0.18f) / 0.32f;
                radiusFactor = Mathf.Lerp(1.0f, 0.72f, hold);
                glow = Mathf.Lerp(1.15f, 0.85f, hold);
            }
            else
            {
                float fadeT = (t - 0.50f) / 0.50f;
                radiusFactor = Mathf.Lerp(0.72f, 0.01f, fadeT * fadeT);
                glow = Mathf.Lerp(0.85f, 0.0f, fadeT);
            }

            float r = _maxRadius * radiusFactor;
            transform.localScale = new Vector3(r, height, r);
            transform.Rotate(0f, 65f * Time.deltaTime, 0f, Space.Self);
            ApplyBlock(glow);
        }

        private void ApplyBlock(float intensityFactor)
        {
            if (_renderer == null) return;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            float clamped = Mathf.Max(0f, intensityFactor);
            Color litCol = Color.Lerp(Color.black, _baseColor, Mathf.Clamp01(clamped));
            litCol.a = Mathf.Clamp01(clamped);

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorId, litCol);
            _propBlock.SetColor(ColorId, litCol);
            _propBlock.SetColor(EmissionColorId, _baseColor * (_hdrIntensity * clamped));
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}
