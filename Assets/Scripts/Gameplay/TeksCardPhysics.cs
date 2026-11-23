using UnityEngine;
using TeksMaster.Audio;
using TeksMaster.Core;
using TeksMaster.Visuals;

namespace TeksMaster.Gameplay
{
    public class TeksCardPhysics : MonoBehaviour
    {
        public string CardId { get; private set; }
        public int StarRank { get; private set; }
        public CardRarity Rarity => _rarity;
        public bool IsPlayerPamato { get; private set; }
        public bool IsOpponentPamato { get; private set; }
        public bool HasSettled { get; private set; }
        public bool TargetFaceUp => _targetFaceUp;
        public float FlightElapsed => _flightElapsed;
        public string RoleLabel { get; set; } = "TEKS";
        public bool IsFaceUp => Vector3.Dot(transform.up, Vector3.up) >= 0f;

        private Rigidbody _rb;
        private Transform _visualRoot;
        private GameObject _landingRing;
        private Transform _billboardText;
        private CardRarity _rarity = CardRarity.Common;
        private bool _hasSpawnRarity;
        private float _flightElapsed;
        private float _settledTimer;
        private float _targetFaceUpProb = 0.5f;
        private bool _decidedOutcome;
        private bool _targetFaceUp;
        private bool _hasTouchedGround;
        private float _groundSlideTimer;
        private float _popTimer = -1f;

        private static readonly Vector3 VisualBaseScale = Vector3.one * 1.25f;

        public static TeksCardPhysics SpawnPhysicalCard(
            string cardId,
            int starRank,
            Vector3 position,
            Quaternion rotation,
            bool isPlayerPamato,
            bool isOpponentPamato)
        {
            var def = TeksDatabase.GetCard(cardId);
            var root = new GameObject($"TeksCard_{(def != null ? def.Name : cardId)}");
            root.transform.position = position;
            root.transform.rotation = rotation;

            var visualRoot = new GameObject("CardVisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            visualRoot.transform.localScale = VisualBaseScale;

            // Core cardboard thickness & rarity edge border
            Color borderColor = def != null ? TeksDatabase.GetRarityColor(def.Rarity) : new Color(0.8f, 0.7f, 0.5f);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "CardBody";
            body.transform.SetParent(visualRoot.transform, false);
            body.transform.localScale = new Vector3(0.22f, 0.010f, 0.31f);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = CardArtGenerator.CreateLitMaterial(borderColor, 0.35f, starRank >= 4 ? 0.35f : 0.05f);

            // Top face (+Y): Character Illustration (Chub / Face-Up)
            var topFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
            topFace.name = "FrontFace_Chub";
            topFace.transform.SetParent(visualRoot.transform, false);
            topFace.transform.localPosition = new Vector3(0f, 0.0056f, 0f);
            topFace.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            topFace.transform.localScale = new Vector3(0.202f, 0.292f, 1f);
            Destroy(topFace.GetComponent<Collider>());
            Texture2D frontTex = def != null ? CardArtGenerator.GetCardPortraitTexture(def, starRank) : Texture2D.whiteTexture;
            topFace.GetComponent<Renderer>().sharedMaterial = CardArtGenerator.CreateLitMaterial(Color.white, 0.28f, 0f, frontTex);

            // Bottom face (-Y): Vintage Cardboard Back (Cha / Face-Down)
            var backFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backFace.name = "BackFace_Cha";
            backFace.transform.SetParent(visualRoot.transform, false);
            backFace.transform.localPosition = new Vector3(0f, -0.0056f, 0f);
            backFace.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            backFace.transform.localScale = new Vector3(0.204f, 0.294f, 1f);
            Destroy(backFace.GetComponent<Collider>());
            backFace.GetComponent<Renderer>().sharedMaterial = CardArtGenerator.CreateLitMaterial(Color.white, 0.12f, 0f, CardArtGenerator.GetCardBackTexture());

            // Physics setup — zero bounce so teks cards never rebound off the concrete
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.275f, 0.020f, 0.388f);
            var physMat = new PhysicsMaterial("TeksCardNoBounceMat")
            {
                bounciness = 0f,
                dynamicFriction = 0.92f,
                staticFriction = 0.95f,
                bounceCombine = PhysicsMaterialCombine.Minimum,
                frictionCombine = PhysicsMaterialCombine.Maximum
            };
            col.material = physMat;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.08f;
            rb.linearDamping = 0.36f;
            rb.angularDamping = 0.42f;
            rb.maxAngularVelocity = 55f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var comp = root.AddComponent<TeksCardPhysics>();
            comp.CardId = cardId;
            comp.StarRank = Mathf.Clamp(starRank, 1, 5);
            comp._rarity = def != null ? def.Rarity : CardRarity.Common;
            comp._hasSpawnRarity = def != null;
            comp.IsPlayerPamato = isPlayerPamato;
            comp.IsOpponentPamato = isOpponentPamato;
            comp._rb = rb;
            comp._visualRoot = visualRoot.transform;
            return comp;
        }

        public void LaunchFlick(
            Vector3 launchVelocity,
            Vector3 angularVelocity,
            float targetFaceUpProbability,
            CardRarity rarity = CardRarity.Common)
        {
            HasSettled = false;
            _flightElapsed = 0f;
            _settledTimer = 0f;
            _hasTouchedGround = false;
            _groundSlideTimer = 0f;
            _popTimer = -1f;
            if (!_hasSpawnRarity || rarity != CardRarity.Common)
            {
                _rarity = rarity;
            }
            if (_visualRoot != null) _visualRoot.localScale = VisualBaseScale;

            // Strictly driven by targetFaceUpProbability
            _targetFaceUpProb = Mathf.Clamp01(targetFaceUpProbability);
            _decidedOutcome = true;
            _targetFaceUp = Random.value < _targetFaceUpProb;

            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = launchVelocity;
                _rb.angularVelocity = angularVelocity;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (HasSettled || !_decidedOutcome || _rb == null) return;

            bool isGroundHit = collision.gameObject.name == "CourtPhysicsSurface" || transform.position.y < 0.15f;
            if (_flightElapsed > 0.12f && isGroundHit)
            {
                HandleGroundContact();
            }
        }

        private void HandleGroundContact()
        {
            if (_rb == null || HasSettled) return;

            _hasTouchedGround = true;

            // Immediately kill any vertical rebound so the card never bounces off the floor
            Vector3 v = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(v.x * 0.35f, 0f, v.z * 0.35f);
            _rb.angularVelocity *= 0.12f;

            // Begin flattening onto the target side without lifting off the ground
            Quaternion targetRot = GetTargetFlatRotation();
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, 0.55f));
        }

        private Quaternion GetTargetFlatRotation()
        {
            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = Vector3.Cross(transform.right, Vector3.up);
                flatForward.y = 0f;
            }
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = Vector3.forward;
            }

            Quaternion yawRot = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            return _targetFaceUp ? yawRot : yawRot * Quaternion.Euler(0f, 0f, 180f);
        }

        private void FixedUpdate()
        {
            if (HasSettled || _rb == null) return;

            _flightElapsed += Time.fixedDeltaTime;

            if (transform.position.y < 0.008f)
            {
                Vector3 p = transform.position;
                p.y = 0.011f;
                transform.position = p;
            }

            if (_decidedOutcome && !_hasTouchedGround)
            {
                // Safety ground-contact catch if card descends onto any non-named flat surface
                if (_flightElapsed > 0.18f && _rb.linearVelocity.y <= 0f && transform.position.y <= 0.024f)
                {
                    HandleGroundContact();
                }
                else if (_flightElapsed > 0.25f && _rb.linearVelocity.y < 0f)
                {
                    // Mid-air descent alignment: preserve natural spin high up, smoothly orient to target side before touchdown
                    float height = Mathf.Max(0f, transform.position.y - 0.011f);
                    float proximity = Mathf.Clamp01(1f - height / 1.15f);
                    Quaternion targetRot = GetTargetFlatRotation();
                    float slerpRate = Mathf.Clamp01((2.4f + proximity * 14.5f) * Time.fixedDeltaTime);
                    _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, slerpRate));

                    Vector3 angVel = _rb.angularVelocity;
                    float pitchRollDamp = Mathf.Lerp(0.95f, 0.68f, proximity);
                    _rb.angularVelocity = new Vector3(
                        angVel.x * pitchRollDamp,
                        angVel.y * 0.94f,
                        angVel.z * pitchRollDamp);
                }
            }

            if (_hasTouchedGround)
            {
                _groundSlideTimer += Time.fixedDeltaTime;

                // Strictly prevent any upward lift or rebound after ground contact
                Vector3 v = _rb.linearVelocity;
                if (v.y > 0f) v.y = 0f;
                v.x *= 0.80f;
                v.z *= 0.80f;
                _rb.linearVelocity = v;
                _rb.angularVelocity *= 0.55f;

                Quaternion targetRot = GetTargetFlatRotation();
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, Mathf.Clamp01(24f * Time.fixedDeltaTime)));
            }

            float linSpeed = _rb.linearVelocity.magnitude;
            float angSpeed = _rb.angularVelocity.magnitude;

            if (_hasTouchedGround)
            {
                if ((linSpeed < 0.16f && angSpeed < 0.85f) || _groundSlideTimer >= 0.32f)
                {
                    _settledTimer += Time.fixedDeltaTime;
                    if (_settledTimer >= 0.10f || _groundSlideTimer >= 0.35f)
                    {
                        SnapFlatAndSettle();
                    }
                }
                else
                {
                    _settledTimer = 0f;
                }
            }
            else if (_flightElapsed > 3.2f)
            {
                SnapFlatAndSettle();
            }
        }

        private void Update()
        {
            if (_popTimer >= 0f && _visualRoot != null)
            {
                _popTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_popTimer / 0.20f);
                float punch = Mathf.Lerp(1.30f, 1.0f, t);
                _visualRoot.localScale = VisualBaseScale * punch;
                if (t >= 1f) _popTimer = -1f;
            }

            if (_billboardText != null && Camera.main != null)
            {
                _billboardText.position = transform.position + Vector3.up * 0.26f;
                Vector3 toCam = _billboardText.position - Camera.main.transform.position;
                if (toCam.sqrMagnitude > 0.001f)
                {
                    _billboardText.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
                }
            }

            if (_landingRing != null)
            {
                _landingRing.transform.position = new Vector3(transform.position.x, 0.005f, transform.position.z);
                _landingRing.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            }
        }

        private void SnapFlatAndSettle()
        {
            if (HasSettled) return;

            HasSettled = true;
            _popTimer = 0f;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            float yaw = transform.eulerAngles.y;
            float finalRoll = _targetFaceUp ? 0f : 180f;
            transform.rotation = Quaternion.Euler(0f, yaw, finalRoll);
            Vector3 pos = transform.position;
            pos.y = 0.011f;
            transform.position = pos;

            SpawnSettledMarkers();

            // Rare/Epic/Legendary visual + audio flourishes fire strictly AFTER outcome & final pose lock
            CardVfxHelper.SpawnLandingVfx(transform, _rarity);
            if (TeksAudioManager.Instance != null)
            {
                TeksAudioManager.Instance.PlayCardLand(_rarity);
            }
        }

        private void SpawnSettledMarkers()
        {
            Color statusCol = _targetFaceUp
                ? new Color(0.22f, 0.96f, 0.45f)
                : new Color(0.96f, 0.34f, 0.28f);

            // Glowing landing pad flat on the concrete beneath the settled card
            if (_landingRing == null)
            {
                _landingRing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _landingRing.name = "CardLandingPad";
                _landingRing.transform.SetParent(transform, false);
                Destroy(_landingRing.GetComponent<Collider>());
                _landingRing.transform.localScale = new Vector3(0.36f, 0.004f, 0.48f);
                _landingRing.GetComponent<Renderer>().sharedMaterial =
                    CardArtGenerator.CreateUnlitEmissiveMaterial(statusCol, 0.55f);
            }

            // Floating 3D status text above card
            if (_billboardText == null)
            {
                var labelGo = new GameObject("SettledStatusBillboard");
                labelGo.transform.SetParent(transform, false);
                labelGo.transform.localScale = Vector3.one * 0.032f;

                var tm = labelGo.AddComponent<TextMesh>();
                tm.fontSize = 36;
                tm.characterSize = 0.72f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.fontStyle = FontStyle.Bold;
                tm.color = statusCol;
                string sideStr = _targetFaceUp ? "CHUB! (FACE-UP)" : "CHA! (FACE-DOWN)";
                tm.text = $"{RoleLabel}\n{sideStr}";

                _billboardText = labelGo.transform;
            }
        }
    }
}
