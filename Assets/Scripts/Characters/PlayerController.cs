using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TeksMaster.Core;
using TeksMaster.World;

namespace TeksMaster.Characters
{
    public class CameraFollowController : MonoBehaviour
    {
        public Transform Target;
        public bool IsMatchCameraActive { get; private set; }
        public bool IsTitleCameraActive { get; set; }

        private Vector3 _matchCameraPos;
        private Quaternion _matchCameraRot;
        private float _orbitYaw = 18f;
        private float _orbitPitch = 28f;
        private float _orbitDistance = 11.2f;
        private float _titleAngle = -28f;

        public void SetMatchCamera(Vector3 worldPos, Vector3 lookTarget)
        {
            IsMatchCameraActive = true;
            _matchCameraPos = worldPos;
            _matchCameraRot = Quaternion.LookRotation((lookTarget - worldPos).normalized, Vector3.up);
        }

        public void ClearMatchCamera()
        {
            IsMatchCameraActive = false;
        }

        private void LateUpdate()
        {
            if (IsTitleCameraActive)
            {
                _titleAngle += Time.deltaTime * 6.5f;
                float rad = _titleAngle * Mathf.Deg2Rad;
                Vector3 courtCenter = new Vector3(1.2f, 1.4f, 0.5f);
                Vector3 orbitPos = courtCenter + new Vector3(Mathf.Sin(rad) * 14.5f, 5.2f, -Mathf.Cos(rad) * 12.2f);
                transform.position = Vector3.Lerp(transform.position, orbitPos, Time.deltaTime * 3.5f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation((courtCenter - transform.position).normalized, Vector3.up), Time.deltaTime * 4f);
                return;
            }

            if (IsMatchCameraActive)
            {
                transform.position = Vector3.Lerp(transform.position, _matchCameraPos, Time.deltaTime * 6.5f);
                transform.rotation = Quaternion.Slerp(transform.rotation, _matchCameraRot, Time.deltaTime * 7.5f);
                return;
            }

            if (Target == null) return;

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _orbitYaw += delta.x * 0.25f;
                _orbitPitch = Mathf.Clamp(_orbitPitch - delta.y * 0.18f, 14f, 52f);
            }

            Quaternion orbitRot = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
            Vector3 focusPoint = Target.position + new Vector3(1.1f, 1.35f, 0.8f);
            Vector3 desiredPos = focusPoint - (orbitRot * Vector3.forward * _orbitDistance);
            if (desiredPos.y < 1.2f) desiredPos.y = 1.2f;

            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * 7f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation((focusPoint - transform.position).normalized, Vector3.up), Time.deltaTime * 9f);
        }
    }

    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [SerializeField] private float walkSpeed = 4.6f;
        [SerializeField] private float runSpeed = 7.0f;
        [SerializeField] private float jumpHeight = 1.15f;
        [SerializeField] private float gravity = -18f;

        public CharacterActor Actor { get; private set; }
        public NpcOpponent NearestOpponent { get; private set; }
        public bool IsNearSariSariStore { get; private set; }
        public bool IsNearTournamentBooth { get; private set; }
        public HiddenCardCollectible NearestHiddenCard { get; private set; }
        public AreaGateBarrier NearestLockedGate { get; private set; }
        public bool MovementLocked { get; set; }
        public bool ExternalInteractHandlerActive { get; set; }

        public event Action<AreaGateBarrier> OnLockedGateInteracted;

        private const float OpponentProximityRadius = 3.1f;
        private const float StoreProximityRadius = 3.6f;
        private const float TournamentBoothProximityRadius = 3.6f;
        private const float HiddenCardProximityRadius = 2.6f;
        private const float LockedGateProximityRadius = 3.2f;

        private CharacterController _cc;
        private Vector3 _velocity;
        private Vector2 _virtualMoveInput;
        private int _lastInteractFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
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

        public void InitializePlayer(Vector3 spawnPos, Quaternion spawnRot)
        {
            _cc = GetComponent<CharacterController>();
            if (_cc == null)
            {
                _cc = gameObject.AddComponent<CharacterController>();
                _cc.center = new Vector3(0f, 0.85f, 0f);
                _cc.radius = 0.34f;
                _cc.height = 1.7f;
            }

            _cc.enabled = false;
            transform.position = spawnPos;
            transform.rotation = spawnRot;
            _cc.enabled = true;

            var inv = PlayerInventory.Instance;
            if (inv != null)
            {
                inv.OnGenderChanged -= HandleGenderChanged;
                inv.OnGenderChanged += HandleGenderChanged;
                RebuildAvatarMesh(inv.SelectedGender);
            }
            else
            {
                RebuildAvatarMesh(PlayerGender.Male);
            }

            if (Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<CameraFollowController>();
                if (camFollow == null) camFollow = Camera.main.gameObject.AddComponent<CameraFollowController>();
                camFollow.Target = transform;
            }
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnGenderChanged -= HandleGenderChanged;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void HandleGenderChanged(PlayerGender gender)
        {
            RebuildAvatarMesh(gender);
        }

        public void RebuildAvatarMesh(PlayerGender gender)
        {
            CharacterOutfitRole role = gender == PlayerGender.Male
                ? CharacterOutfitRole.MaleKid
                : CharacterOutfitRole.FemaleKid;

            Color shirt = gender == PlayerGender.Male
                ? new Color(0.95f, 0.95f, 0.96f)   // White Pinoy street tee (like foreground player in image_0)
                : new Color(0.98f, 0.45f, 0.62f);  // Bright coral/pink tee

            Color shorts = gender == PlayerGender.Male
                ? new Color(0.14f, 0.16f, 0.24f)   // Dark basketball shorts with white stripe
                : new Color(0.18f, 0.42f, 0.78f);

            Color slippers = gender == PlayerGender.Male
                ? new Color(0.18f, 0.56f, 0.86f)
                : new Color(0.96f, 0.32f, 0.58f);

            var currentPose = Actor != null ? Actor.CurrentPose : CharacterPoseMode.StandingOrWalking;
            Actor = CharacterVisualBuilder.BuildCharacterVisual(
                gameObject,
                role,
                shirt,
                shorts,
                new Color(0.75f, 0.51f, 0.37f),
                slippers
            );
            Actor.SetPose(currentPose);
        }

        public void SetVirtualMoveInput(Vector2 move)
        {
            _virtualMoveInput = Vector2.ClampMagnitude(move, 1f);
        }

        public void TeleportForMatch(Vector3 pos, Quaternion rot)
        {
            if (_cc != null) _cc.enabled = false;
            transform.position = pos;
            transform.rotation = rot;
            _velocity = Vector3.zero;
            if (_cc != null) _cc.enabled = true;
        }

        public bool TryInteractNearestHiddenCard()
        {
            if (_lastInteractFrame == Time.frameCount) return false;
            if (NearestHiddenCard == null || !NearestHiddenCard.CanCollect) return false;

            _lastInteractFrame = Time.frameCount;
            bool collected = NearestHiddenCard.TryCollect();
            if (collected)
            {
                NearestHiddenCard = null;
            }
            return collected;
        }

        public bool TryInteractNearestLockedGate()
        {
            if (_lastInteractFrame == Time.frameCount) return false;
            if (NearestLockedGate == null || !NearestLockedGate.IsLocked) return false;

            _lastInteractFrame = Time.frameCount;
            OnLockedGateInteracted?.Invoke(NearestLockedGate);
            return true;
        }

        private void Update()
        {
            UpdateProximityTargets();

            var kb = Keyboard.current;
            if (!MovementLocked && kb != null && kb.cKey.wasPressedThisFrame)
            {
                PlayerInventory.Instance?.ToggleGender();
            }

            if (!MovementLocked && !ExternalInteractHandlerActive && kb != null && kb.eKey.wasPressedThisFrame)
            {
                if (NearestOpponent == null && !IsNearSariSariStore && !IsNearTournamentBooth)
                {
                    if (NearestHiddenCard != null)
                    {
                        TryInteractNearestHiddenCard();
                    }
                    else if (NearestLockedGate != null)
                    {
                        TryInteractNearestLockedGate();
                    }
                }
            }

            if (MovementLocked)
            {
                if (Actor != null) Actor.SetNormalizedSpeed(0f);
                return;
            }

            Vector2 move = _virtualMoveInput;
            bool sprint = false;
            bool jumpPressed = false;

            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                sprint = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                jumpPressed = kb.spaceKey.wasPressedThisFrame;
            }

            if (move.sqrMagnitude > 1f) move.Normalize();

            Vector3 camForward = Vector3.forward;
            Vector3 camRight = Vector3.right;
            if (Camera.main != null)
            {
                camForward = Camera.main.transform.forward;
                camForward.y = 0f;
                camForward.Normalize();
                camRight = Camera.main.transform.right;
                camRight.y = 0f;
                camRight.Normalize();
            }

            Vector3 moveDir = camForward * move.y + camRight * move.x;
            float speed = sprint ? runSpeed : walkSpeed;

            if (_cc != null)
            {
                if (_cc.isGrounded && _velocity.y < 0f)
                {
                    _velocity.y = -2f;
                }

                if (_cc.isGrounded && jumpPressed)
                {
                    _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }

                _velocity.y += gravity * Time.deltaTime;

                Vector3 motion = moveDir * speed + Vector3.up * _velocity.y;
                _cc.Move(motion * Time.deltaTime);
            }

            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 12f);
            }

            if (Actor != null)
            {
                Actor.SetPose(CharacterPoseMode.StandingOrWalking);
                Actor.SetNormalizedSpeed(move.magnitude * (sprint ? 1f : 0.72f));
            }
        }

        private void UpdateProximityTargets()
        {
            NearestOpponent = null;
            NearestHiddenCard = null;
            NearestLockedGate = null;
            IsNearSariSariStore = false;
            IsNearTournamentBooth = false;

            float bestOppDist = OpponentProximityRadius;
            Vector3 playerPos = transform.position;

            var court = BarangayCourtBuilder.Instance;
            if (court != null)
            {
                for (int i = 0; i < court.SpawnedOpponents.Count; i++)
                {
                    var npc = court.SpawnedOpponents[i];
                    if (npc == null) continue;
                    npc.SetPlayerTransform(transform);
                    float d = Vector3.Distance(playerPos, npc.transform.position);
                    if (d < bestOppDist)
                    {
                        bestOppDist = d;
                        NearestOpponent = npc;
                    }
                }

                var storePoints = court.StoreInteractionPoints;
                if (storePoints != null && storePoints.Count > 0)
                {
                    for (int i = 0; i < storePoints.Count; i++)
                    {
                        if (Vector3.Distance(playerPos, storePoints[i]) < StoreProximityRadius)
                        {
                            IsNearSariSariStore = true;
                            break;
                        }
                    }
                }
                else
                {
                    IsNearSariSariStore = Vector3.Distance(playerPos, court.StoreInteractionPoint) < StoreProximityRadius;
                }

                IsNearTournamentBooth = Vector3.Distance(playerPos, court.TournamentInteractionPoint) < TournamentBoothProximityRadius;
            }

            // Detect nearest uncollected hidden card (horizontal XZ distance < 2.6f)
            float bestCardDist = HiddenCardProximityRadius;
            var hiddenCards = HiddenCardCollectible.AllInstances;
            for (int i = 0; i < hiddenCards.Count; i++)
            {
                var card = hiddenCards[i];
                if (card == null || !card.CanCollect) continue;
                Vector3 cPos = card.transform.position;
                float dx = playerPos.x - cPos.x;
                float dz = playerPos.z - cPos.z;
                float distXZ = Mathf.Sqrt(dx * dx + dz * dz);
                if (distXZ < bestCardDist)
                {
                    bestCardDist = distXZ;
                    NearestHiddenCard = card;
                }
            }

            // Detect nearest locked area gate barrier (< 3.2f via BoxCollider.ClosestPoint horizontal distance)
            float bestGateDist = LockedGateProximityRadius;
            var gates = AreaGateBarrier.AllBarriers;
            for (int i = 0; i < gates.Count; i++)
            {
                var gate = gates[i];
                if (gate == null || !gate.IsLocked) continue;
                float dist = gate.GetDistanceTo(playerPos);
                if (dist < bestGateDist)
                {
                    bestGateDist = dist;
                    NearestLockedGate = gate;
                }
            }
        }
    }
}
