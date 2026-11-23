using UnityEngine;
using TeksMaster.Core;
using TeksMaster.Visuals;

namespace TeksMaster.World
{
    public enum CharacterPoseMode
    {
        StandingOrWalking,
        SittingOnCourt,
        CrouchingFlick,
        SittingOnBench,
        Talking
    }

    public enum CharacterOutfitRole
    {
        MaleKid,
        FemaleKid,
        Tindera
    }

    public class CharacterActor : MonoBehaviour
    {
        public const int ReactionNone = 0;
        public const int ReactionWin = 1;
        public const int ReactionLose = 2;
        public const int ReactionTabla = 3;

        public Transform VisualRoot;
        public Transform Pelvis;
        public Transform Torso;
        public Transform HeadGroup;
        public Transform LeftArmPivot;
        public Transform LeftForearmPivot;
        public Transform RightArmPivot;
        public Transform RightForearmPivot;
        public Transform LeftLegPivot;
        public Transform RightLegPivot;
        public Transform LeftKneePivot;
        public Transform RightKneePivot;
        public GameObject HandCardObject;
        public Renderer HandCardRenderer;

        public CharacterPoseMode CurrentPose { get; private set; } = CharacterPoseMode.StandingOrWalking;
        public bool IsPlayingReaction => _reactionType != ReactionNone && _reactionTimer > 0f;
        public int CurrentReactionType => IsPlayingReaction ? _reactionType : ReactionNone;

        private float _normalizedSpeed;
        private float _walkCycle;
        private float _flickAnimTimer;
        private bool _isPlayingFlick;
        private float _skyWatchTimer;
        private int _reactionType = ReactionNone;
        private float _reactionTimer;
        private float _reactionDuration;

        public void SetPose(CharacterPoseMode pose)
        {
            CurrentPose = pose;
            if (pose != CharacterPoseMode.CrouchingFlick)
            {
                _isPlayingFlick = false;
                _skyWatchTimer = 0f;
            }
        }

        public void SetNormalizedSpeed(float speed01)
        {
            _normalizedSpeed = Mathf.Clamp01(speed01);
        }

        public void SetHandCardVisible(bool visible, Texture2D frontTex = null)
        {
            if (HandCardObject == null) return;
            HandCardObject.SetActive(visible);
            if (visible && frontTex != null && HandCardRenderer != null)
            {
                if (HandCardRenderer.sharedMaterial.HasProperty("_BaseMap"))
                    HandCardRenderer.sharedMaterial.SetTexture("_BaseMap", frontTex);
                if (HandCardRenderer.sharedMaterial.HasProperty("_MainTex"))
                    HandCardRenderer.sharedMaterial.SetTexture("_MainTex", frontTex);
            }
        }

        public void PlayReactionPose(int reactionType, float duration = 2.2f)
        {
            if (reactionType == ReactionNone || duration <= 0f)
            {
                ClearReactionPose();
                return;
            }

            _skyWatchTimer = 0f;
            _isPlayingFlick = false;
            _reactionType = reactionType;
            _reactionDuration = Mathf.Max(0.1f, duration);
            _reactionTimer = _reactionDuration;
        }

        public void ClearReactionPose()
        {
            _reactionType = ReactionNone;
            _reactionTimer = 0f;
            _reactionDuration = 0f;
        }

        public void TriggerFlickAnimation()
        {
            ClearReactionPose();
            _skyWatchTimer = 0f;
            _isPlayingFlick = true;
            _flickAnimTimer = 0f;
        }

        public void TriggerSkyWatchAnimation(float duration = 2.0f)
        {
            ClearReactionPose();
            _skyWatchTimer = Mathf.Max(0f, duration);
        }

        public Vector3 GetHeadTagWorldPosition()
        {
            float height = CurrentPose switch
            {
                CharacterPoseMode.SittingOnCourt => 1.08f,
                CharacterPoseMode.CrouchingFlick => 1.18f,
                CharacterPoseMode.SittingOnBench => 1.44f,
                CharacterPoseMode.Talking => 1.72f,
                _ => 1.72f
            };
            return transform.position + Vector3.up * height;
        }

        private void Update()
        {
            if (Pelvis == null) return;

            float dt = Time.deltaTime;
            float targetPelvisY = 0.62f;
            Quaternion targetTorsoRot = Quaternion.identity;
            Quaternion targetHeadRot = Quaternion.identity;
            Quaternion targetLeftLeg = Quaternion.identity;
            Quaternion targetRightLeg = Quaternion.identity;
            Quaternion targetLeftKnee = Quaternion.identity;
            Quaternion targetRightKnee = Quaternion.identity;
            Quaternion targetLeftArm = Quaternion.Euler(0f, 0f, 8f);
            Quaternion targetLeftForearm = Quaternion.identity;
            Quaternion targetRightArm = Quaternion.Euler(0f, 0f, -8f);
            Quaternion targetRightForearm = Quaternion.identity;

            if (CurrentPose == CharacterPoseMode.SittingOnCourt)
            {
                targetPelvisY = 0.14f;
                targetTorsoRot = Quaternion.Euler(12f, 0f, 0f);
                targetLeftLeg = Quaternion.Euler(-78f, -18f, 0f);
                targetRightLeg = Quaternion.Euler(-78f, 18f, 0f);
                targetLeftArm = Quaternion.Euler(-38f, 10f, 14f);
                targetRightArm = Quaternion.Euler(-38f, -10f, -14f);
            }
            else if (CurrentPose == CharacterPoseMode.SittingOnBench)
            {
                float sitIdle = Mathf.Sin(Time.time * 1.8f);
                targetPelvisY = 0.44f;
                targetTorsoRot = Quaternion.Euler(4f + sitIdle * 1.2f, 0f, 0f);
                targetHeadRot = Quaternion.Euler(sitIdle * 2.0f, Mathf.Sin(Time.time * 0.9f) * 4.5f, 0f);
                targetLeftLeg = Quaternion.Euler(-82f, -4f, 0f);
                targetRightLeg = Quaternion.Euler(-82f, 4f, 0f);
                targetLeftKnee = Quaternion.Euler(82f, 0f, 0f);
                targetRightKnee = Quaternion.Euler(82f, 0f, 0f);
                targetLeftArm = Quaternion.Euler(-24f + sitIdle * 1.5f, 8f, 10f);
                targetRightArm = Quaternion.Euler(-24f - sitIdle * 1.5f, -8f, -10f);
            }
            else if (CurrentPose == CharacterPoseMode.CrouchingFlick)
            {
                // Athletic slight crouch holding the teks cards out in front with both hands together
                float readyBob = Mathf.Sin(Time.time * 3.5f) * 2.5f;
                targetPelvisY = 0.50f;
                targetTorsoRot = Quaternion.Euler(18f, 0f, 0f);
                targetHeadRot = Quaternion.Euler(-10f, 0f, 0f);
                targetLeftLeg = Quaternion.Euler(-28f, -8f, 0f);
                targetRightLeg = Quaternion.Euler(-24f, 8f, 0f);
                targetLeftKnee = Quaternion.Euler(48f, 0f, 0f);
                targetRightKnee = Quaternion.Euler(44f, 0f, 0f);
                targetLeftArm = Quaternion.Euler(-46f + readyBob, 24f, 18f);
                targetRightArm = Quaternion.Euler(-48f + readyBob, -24f, -18f);
                targetLeftForearm = Quaternion.Euler(-52f, 12f, 0f);
                targetRightForearm = Quaternion.Euler(-52f, -12f, 0f);

                if (_isPlayingFlick)
                {
                    _flickAnimTimer += dt;

                    if (_flickAnimTimer < 0.12f)
                    {
                        // Stage 1 (0.00-0.12s): Quick dip crouch & pull hands slightly down
                        float s1 = Mathf.Clamp01(_flickAnimTimer / 0.12f);
                        float w = 1f - (1f - s1) * (1f - s1);
                        targetPelvisY = Mathf.Lerp(0.50f, 0.42f, w);
                        targetTorsoRot = Quaternion.Euler(Mathf.Lerp(18f, 28f, w), Mathf.Lerp(0f, 8f, w), 0f);
                        targetHeadRot = Quaternion.Euler(Mathf.Lerp(-10f, 4f, w), 0f, 0f);
                        targetLeftLeg = Quaternion.Euler(Mathf.Lerp(-28f, -40f, w), -8f, 0f);
                        targetRightLeg = Quaternion.Euler(Mathf.Lerp(-24f, -36f, w), 8f, 0f);
                        targetLeftKnee = Quaternion.Euler(Mathf.Lerp(48f, 64f, w), 0f, 0f);
                        targetRightKnee = Quaternion.Euler(Mathf.Lerp(44f, 58f, w), 0f, 0f);
                        targetLeftArm = Quaternion.Euler(Mathf.Lerp(-46f, -26f, w), Mathf.Lerp(24f, 12f, w), Mathf.Lerp(18f, 22f, w));
                        targetRightArm = Quaternion.Euler(Mathf.Lerp(-48f, -18f, w), Mathf.Lerp(-24f, -10f, w), Mathf.Lerp(-18f, -14f, w));
                        targetLeftForearm = Quaternion.Euler(Mathf.Lerp(-52f, -38f, w), 8f, 0f);
                        targetRightForearm = Quaternion.Euler(Mathf.Lerp(-52f, -68f, w), -8f, 0f);
                    }
                    else if (_flickAnimTimer < 0.36f)
                    {
                        // Stage 2 (0.12-0.36s): Explosive high upward toss — right arm sweeps straight up, head tilts way up into the sky
                        float s2 = Mathf.Clamp01((_flickAnimTimer - 0.12f) / 0.24f);
                        float t = 1f - Mathf.Pow(1f - s2, 3f);
                        targetPelvisY = Mathf.Lerp(0.42f, 0.62f, t);
                        targetTorsoRot = Quaternion.Euler(Mathf.Lerp(28f, -6f, t), Mathf.Lerp(8f, -10f, t), Mathf.Lerp(0f, 4f, t));
                        targetHeadRot = Quaternion.Euler(Mathf.Lerp(4f, -44f, t), 0f, 0f);
                        targetLeftLeg = Quaternion.Euler(Mathf.Lerp(-40f, -6f, t), -6f, 0f);
                        targetRightLeg = Quaternion.Euler(Mathf.Lerp(-36f, -4f, t), 6f, 0f);
                        targetLeftKnee = Quaternion.Euler(Mathf.Lerp(64f, 10f, t), 0f, 0f);
                        targetRightKnee = Quaternion.Euler(Mathf.Lerp(58f, 8f, t), 0f, 0f);
                        targetLeftArm = Quaternion.Euler(Mathf.Lerp(-26f, 16f, t), Mathf.Lerp(12f, -12f, t), Mathf.Lerp(22f, 28f, t));
                        targetRightArm = Quaternion.Euler(Mathf.Lerp(-18f, -162f, t), Mathf.Lerp(-10f, -6f, t), Mathf.Lerp(-14f, -10f, t));
                        targetLeftForearm = Quaternion.Euler(Mathf.Lerp(-38f, -18f, t), 0f, 0f);
                        targetRightForearm = Quaternion.Euler(Mathf.Lerp(-68f, -8f, t), 0f, 0f);
                    }
                    else
                    {
                        // Stage 3 (0.36-1.10s): Hold sky-watching gaze while right arm gradually lowers to excited open hand
                        float s3 = Mathf.Clamp01((_flickAnimTimer - 0.36f) / 0.74f);
                        float r = s3 * s3 * (3f - 2f * s3);
                        targetPelvisY = Mathf.Lerp(0.62f, 0.58f, r);
                        targetTorsoRot = Quaternion.Euler(Mathf.Lerp(-6f, -2f, r), Mathf.Lerp(-10f, 0f, r), Mathf.Lerp(4f, 0f, r));
                        targetHeadRot = Quaternion.Euler(Mathf.Lerp(-44f, -40f, r), 0f, 0f);
                        targetLeftLeg = Quaternion.Euler(-6f, -6f, 0f);
                        targetRightLeg = Quaternion.Euler(-4f, 6f, 0f);
                        targetLeftKnee = Quaternion.Euler(10f, 0f, 0f);
                        targetRightKnee = Quaternion.Euler(8f, 0f, 0f);
                        targetLeftArm = Quaternion.Euler(Mathf.Lerp(16f, -22f, r), 8f, Mathf.Lerp(28f, 18f, r));
                        targetRightArm = Quaternion.Euler(Mathf.Lerp(-162f, -68f, r), -8f, Mathf.Lerp(-10f, -18f, r));
                        targetLeftForearm = Quaternion.Euler(-24f, 0f, 0f);
                        targetRightForearm = Quaternion.Euler(Mathf.Lerp(-8f, -28f, r), 0f, 0f);

                        if (_flickAnimTimer >= 1.10f)
                        {
                            _isPlayingFlick = false;
                            _skyWatchTimer = Mathf.Max(_skyWatchTimer, 1.1f);
                        }
                    }
                }
            }
            else if (CurrentPose == CharacterPoseMode.Talking)
            {
                float t = Time.time;
                float sway = Mathf.Sin(t * 2.2f);
                float nod = Mathf.Sin(t * 5.2f) * 6.5f;
                float headYaw = Mathf.Sin(t * 1.9f) * 7.5f;
                float gestureL = Mathf.Sin(t * 4.2f);
                float gestureR = Mathf.Sin(t * 4.2f + 1.4f);

                targetPelvisY = 0.62f + Mathf.Abs(sway) * 0.012f;
                targetTorsoRot = Quaternion.Euler(3f + sway * 2.0f, sway * 4.0f, sway * 1.8f);
                targetHeadRot = Quaternion.Euler(nod, headYaw, Mathf.Sin(t * 2.6f) * 3.0f);
                targetLeftArm = Quaternion.Euler(-28f - gestureL * 18f, 12f + gestureL * 8f, 14f + gestureL * 6f);
                targetRightArm = Quaternion.Euler(-34f - gestureR * 22f, -12f - gestureR * 8f, -14f - gestureR * 6f);
            }
            else
            {
                if (_normalizedSpeed > 0.05f)
                {
                    _walkCycle += dt * (4.5f + _normalizedSpeed * 6.5f);
                    float swing = Mathf.Sin(_walkCycle) * 32f * _normalizedSpeed;
                    targetPelvisY = 0.62f + Mathf.Abs(Mathf.Cos(_walkCycle)) * 0.03f * _normalizedSpeed;
                    targetTorsoRot = Quaternion.Euler(6f * _normalizedSpeed, 0f, 0f);
                    targetLeftLeg = Quaternion.Euler(swing, 0f, 0f);
                    targetRightLeg = Quaternion.Euler(-swing, 0f, 0f);
                    targetLeftArm = Quaternion.Euler(-swing * 0.8f, 0f, 8f);
                    targetRightArm = Quaternion.Euler(swing * 0.8f, 0f, -8f);
                }
                else
                {
                    float idle = Mathf.Sin(Time.time * 2.0f) * 1.5f;
                    targetPelvisY = 0.62f + Mathf.Sin(Time.time * 2.0f) * 0.01f;
                    targetTorsoRot = Quaternion.Euler(idle * 0.3f, 0f, 0f);
                    targetLeftArm = Quaternion.Euler(idle, 0f, 7f);
                    targetRightArm = Quaternion.Euler(-idle, 0f, -7f);
                }
            }

            if (_skyWatchTimer > 0f && !_isPlayingFlick)
            {
                _skyWatchTimer -= dt;
                if (_skyWatchTimer < 0f) _skyWatchTimer = 0f;

                targetPelvisY = Mathf.Max(targetPelvisY, 0.58f);
                targetTorsoRot = Quaternion.Euler(-4f, 0f, 0f);
                targetHeadRot = Quaternion.Euler(-42f, Mathf.Sin(Time.time * 2.5f) * 3f, 0f);
                targetLeftLeg = Quaternion.Euler(-6f, -6f, 0f);
                targetRightLeg = Quaternion.Euler(-4f, 6f, 0f);
                targetLeftKnee = Quaternion.Euler(10f, 0f, 0f);
                targetRightKnee = Quaternion.Euler(8f, 0f, 0f);
                targetLeftArm = Quaternion.Euler(-20f, 10f, 18f);
                targetRightArm = Quaternion.Euler(-24f, -10f, -18f);
                targetLeftForearm = Quaternion.Euler(-28f, 0f, 0f);
                targetRightForearm = Quaternion.Euler(-28f, 0f, 0f);
            }

            if (_reactionType != ReactionNone && _reactionTimer > 0f)
            {
                _reactionTimer -= dt;
                if (_reactionTimer <= 0f)
                {
                    ClearReactionPose();
                }
                else
                {
                    float elapsed = _reactionDuration - _reactionTimer;
                    bool isSeatedPose = CurrentPose == CharacterPoseMode.SittingOnCourt
                        || CurrentPose == CharacterPoseMode.SittingOnBench;

                    if (_reactionType == ReactionWin)
                    {
                        // Excitedly leans forward looking down at the winning card and points right hand downward with happy bounce
                        float bounce = Mathf.Abs(Mathf.Sin(elapsed * 8.5f));
                        if (!isSeatedPose)
                        {
                            targetPelvisY = 0.58f + bounce * 0.08f;
                            targetLeftLeg = Quaternion.Euler(-14f - bounce * 8f, -6f, 0f);
                            targetRightLeg = Quaternion.Euler(-10f - bounce * 8f, 6f, 0f);
                            targetLeftKnee = Quaternion.Euler(22f + bounce * 10f, 0f, 0f);
                            targetRightKnee = Quaternion.Euler(18f + bounce * 10f, 0f, 0f);
                        }
                        else
                        {
                            targetPelvisY += bounce * 0.03f;
                        }

                        targetTorsoRot = Quaternion.Euler(24f, -8f, Mathf.Sin(elapsed * 5.0f) * 3f);
                        targetHeadRot = Quaternion.Euler(34f, 4f, 0f);
                        targetLeftArm = Quaternion.Euler(-24f, 14f, 34f + bounce * 10f);
                        targetRightArm = Quaternion.Euler(-42f + Mathf.Sin(elapsed * 10.0f) * 6f, -10f, -10f);
                        targetLeftForearm = Quaternion.Euler(-30f, 0f, 0f);
                        targetRightForearm = Quaternion.Euler(-6f, 0f, 0f);
                    }
                    else if (_reactionType == ReactionLose)
                    {
                        // Looks down at the cards and shrugs with both arms spread out to the sides ("Ay sayang!")
                        float shrug = 0.5f + 0.5f * Mathf.Sin(elapsed * 5.0f);
                        float shake = Mathf.Sin(elapsed * 6.0f) * 10f;
                        if (!isSeatedPose)
                        {
                            targetPelvisY = 0.58f;
                            targetLeftLeg = Quaternion.Euler(-6f, -5f, 0f);
                            targetRightLeg = Quaternion.Euler(-6f, 5f, 0f);
                            targetLeftKnee = Quaternion.Euler(10f, 0f, 0f);
                            targetRightKnee = Quaternion.Euler(10f, 0f, 0f);
                        }

                        targetTorsoRot = Quaternion.Euler(10f, 0f, 0f);
                        targetHeadRot = Quaternion.Euler(24f, shake, Mathf.Sin(elapsed * 4.0f) * 6f);
                        targetLeftArm = Quaternion.Euler(-28f - shrug * 10f, 24f, 54f + shrug * 12f);
                        targetRightArm = Quaternion.Euler(-28f - shrug * 10f, -24f, -54f - shrug * 12f);
                        targetLeftForearm = Quaternion.Euler(-48f, -16f, 0f);
                        targetRightForearm = Quaternion.Euler(-48f, 16f, 0f);
                    }
                    else if (_reactionType == ReactionTabla)
                    {
                        float shrugPulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 6.0f);
                        float tilt = Mathf.Sin(elapsed * 4.5f) * 14f;
                        targetTorsoRot = Quaternion.Euler(-4f, 0f, tilt * 0.25f);
                        targetHeadRot = Quaternion.Euler(-4f, tilt * 0.5f, tilt);
                        targetLeftArm = Quaternion.Euler(-48f - shrugPulse * 14f, 26f, 44f + shrugPulse * 12f);
                        targetRightArm = Quaternion.Euler(-48f - shrugPulse * 14f, -26f, -44f - shrugPulse * 12f);
                    }
                }
            }

            float lerpRate = (_isPlayingFlick && CurrentPose == CharacterPoseMode.CrouchingFlick) ? 24f : 12f;
            float lerpSpeed = Mathf.Clamp01(dt * lerpRate);
            Pelvis.localPosition = Vector3.Lerp(Pelvis.localPosition, new Vector3(0f, targetPelvisY, 0f), lerpSpeed);
            if (Torso != null) Torso.localRotation = Quaternion.Slerp(Torso.localRotation, targetTorsoRot, lerpSpeed);
            if (HeadGroup != null) HeadGroup.localRotation = Quaternion.Slerp(HeadGroup.localRotation, targetHeadRot, lerpSpeed);
            if (LeftLegPivot != null) LeftLegPivot.localRotation = Quaternion.Slerp(LeftLegPivot.localRotation, targetLeftLeg, lerpSpeed);
            if (RightLegPivot != null) RightLegPivot.localRotation = Quaternion.Slerp(RightLegPivot.localRotation, targetRightLeg, lerpSpeed);
            if (LeftKneePivot != null) LeftKneePivot.localRotation = Quaternion.Slerp(LeftKneePivot.localRotation, targetLeftKnee, lerpSpeed);
            if (RightKneePivot != null) RightKneePivot.localRotation = Quaternion.Slerp(RightKneePivot.localRotation, targetRightKnee, lerpSpeed);
            if (LeftArmPivot != null) LeftArmPivot.localRotation = Quaternion.Slerp(LeftArmPivot.localRotation, targetLeftArm, lerpSpeed);
            if (LeftForearmPivot != null) LeftForearmPivot.localRotation = Quaternion.Slerp(LeftForearmPivot.localRotation, targetLeftForearm, lerpSpeed);
            if (RightArmPivot != null) RightArmPivot.localRotation = Quaternion.Slerp(RightArmPivot.localRotation, targetRightArm, lerpSpeed);
            if (RightForearmPivot != null) RightForearmPivot.localRotation = Quaternion.Slerp(RightForearmPivot.localRotation, targetRightForearm, lerpSpeed);
        }
    }

    public static class CharacterVisualBuilder
    {
        public static CharacterActor BuildCharacterVisual(
            GameObject host,
            CharacterOutfitRole role,
            Color shirtColor,
            Color shortsColor,
            Color skinTone,
            Color slipperColor)
        {
            var existingVisual = host.transform.Find("VisualRoot");
            if (existingVisual != null)
            {
                SafeDestroy(existingVisual.gameObject);
            }

            var actor = host.GetComponent<CharacterActor>();
            if (actor == null) actor = host.AddComponent<CharacterActor>();

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(host.transform, false);
            actor.VisualRoot = visualRoot.transform;

            int nameHash = Mathf.Abs(host.name.GetHashCode());
            float hairBlend = (nameHash % 5) / 4f;
            Color hairColor = Color.Lerp(new Color(0.08f, 0.08f, 0.09f), new Color(0.24f, 0.14f, 0.08f), hairBlend);
            bool wearJeans = role != CharacterOutfitRole.Tindera && (nameHash % 2 == 0);
            bool isPolo = role != CharacterOutfitRole.Tindera && ((nameHash / 2) % 2 == 0);
            bool useSneakers = role != CharacterOutfitRole.Tindera;

            Material skinMat = CardArtGenerator.CreateLitMaterial(skinTone, 0.24f);
            Material shirtMat = CardArtGenerator.CreateLitMaterial(shirtColor, 0.20f);
            Material collarMat = CardArtGenerator.CreateLitMaterial(Color.Lerp(shirtColor, Color.white, isPolo ? 0.18f : 0.25f), 0.22f);
            Material sleeveMat = CardArtGenerator.CreateLitMaterial(shirtColor, 0.20f);
            Material shortsMat = CardArtGenerator.CreateLitMaterial(shortsColor, 0.16f);
            Material hairMat = CardArtGenerator.CreateLitMaterial(hairColor, 0.28f);
            Material slipperMat = CardArtGenerator.CreateLitMaterial(slipperColor, 0.22f);
            Material whiteMat = CardArtGenerator.CreateLitMaterial(Color.white, 0.30f);
            Material eyeMat = CardArtGenerator.CreateLitMaterial(new Color(0.07f, 0.07f, 0.08f), 0.72f);
            Material mouthMat = CardArtGenerator.CreateLitMaterial(new Color(0.46f, 0.20f, 0.18f), 0.20f);

            // Pelvis root
            var pelvis = new GameObject("Pelvis").transform;
            pelvis.SetParent(visualRoot.transform, false);
            pelvis.localPosition = new Vector3(0f, 0.62f, 0f);
            actor.Pelvis = pelvis;

            // Smooth rounded hips
            CreatePart("Hips", PrimitiveType.Sphere, pelvis, new Vector3(0f, 0.01f, 0f), new Vector3(0.28f, 0.16f, 0.20f), shortsMat);

            // Torso
            var torsoPivot = new GameObject("Torso").transform;
            torsoPivot.SetParent(pelvis, false);
            torsoPivot.localPosition = new Vector3(0f, 0.08f, 0f);
            actor.Torso = torsoPivot;

            // Smooth rounded crew-neck T-shirt or collared polo torso
            CreatePart("ShirtBody", PrimitiveType.Capsule, torsoPivot, new Vector3(0f, 0.21f, 0f), new Vector3(0.29f, 0.22f, 0.21f), shirtMat);
            CreatePart("CollarRing", PrimitiveType.Cylinder, torsoPivot, new Vector3(0f, 0.405f, 0f), new Vector3(0.145f, 0.018f, 0.135f), collarMat);
            if (isPolo)
            {
                var leftFlap = CreatePart("PoloCollarLeft", PrimitiveType.Cube, torsoPivot, new Vector3(-0.052f, 0.398f, 0.068f), new Vector3(0.068f, 0.030f, 0.045f), collarMat);
                leftFlap.transform.localRotation = Quaternion.Euler(18f, -24f, 18f);
                var rightFlap = CreatePart("PoloCollarRight", PrimitiveType.Cube, torsoPivot, new Vector3(0.052f, 0.398f, 0.068f), new Vector3(0.068f, 0.030f, 0.045f), collarMat);
                rightFlap.transform.localRotation = Quaternion.Euler(18f, 24f, -18f);
                CreatePart("PoloPlacket", PrimitiveType.Cube, torsoPivot, new Vector3(0f, 0.345f, 0.102f), new Vector3(0.032f, 0.095f, 0.012f), collarMat);
            }

            // Slim cartoon neck
            CreatePart("Neck", PrimitiveType.Cylinder, torsoPivot, new Vector3(0f, 0.435f, 0f), new Vector3(0.092f, 0.055f, 0.092f), skinMat);

            if (role == CharacterOutfitRole.Tindera)
            {
                Material apronMat = CardArtGenerator.CreateLitMaterial(new Color(0.95f, 0.90f, 0.78f), 0.15f);
                CreatePart("Apron", PrimitiveType.Cube, torsoPivot, new Vector3(0f, 0.14f, 0.108f), new Vector3(0.24f, 0.36f, 0.025f), apronMat);
            }

            // Stylized Cartoon Kid Head & Face
            var headGroup = new GameObject("HeadGroup").transform;
            headGroup.SetParent(torsoPivot, false);
            headGroup.localPosition = new Vector3(0f, 0.54f, 0f);
            actor.HeadGroup = headGroup;

            CreatePart("Head", PrimitiveType.Sphere, headGroup, Vector3.zero, new Vector3(0.36f, 0.38f, 0.34f), skinMat);
            CreatePart("LeftEar", PrimitiveType.Sphere, headGroup, new Vector3(-0.175f, 0.0f, -0.01f), new Vector3(0.06f, 0.085f, 0.05f), skinMat);
            CreatePart("RightEar", PrimitiveType.Sphere, headGroup, new Vector3(0.175f, 0.0f, -0.01f), new Vector3(0.06f, 0.085f, 0.05f), skinMat);
            CreatePart("Nose", PrimitiveType.Sphere, headGroup, new Vector3(0f, -0.015f, 0.168f), new Vector3(0.042f, 0.038f, 0.036f), skinMat);

            // Large expressive cartoon eyes with specular catchlights
            CreatePart("LeftEyeWhite", PrimitiveType.Sphere, headGroup, new Vector3(-0.068f, 0.025f, 0.150f), new Vector3(0.064f, 0.072f, 0.028f), whiteMat);
            CreatePart("RightEyeWhite", PrimitiveType.Sphere, headGroup, new Vector3(0.068f, 0.025f, 0.150f), new Vector3(0.064f, 0.072f, 0.028f), whiteMat);
            CreatePart("LeftPupil", PrimitiveType.Sphere, headGroup, new Vector3(-0.068f, 0.022f, 0.160f), new Vector3(0.038f, 0.044f, 0.024f), eyeMat);
            CreatePart("RightPupil", PrimitiveType.Sphere, headGroup, new Vector3(0.068f, 0.022f, 0.160f), new Vector3(0.038f, 0.044f, 0.024f), eyeMat);
            CreatePart("LeftCatchlight", PrimitiveType.Sphere, headGroup, new Vector3(-0.058f, 0.036f, 0.170f), new Vector3(0.014f, 0.014f, 0.012f), whiteMat);
            CreatePart("RightCatchlight", PrimitiveType.Sphere, headGroup, new Vector3(0.058f, 0.036f, 0.170f), new Vector3(0.014f, 0.014f, 0.012f), whiteMat);

            // Sculpted eyebrows & warm smile mouth
            var leftBrow = CreatePart("LeftBrow", PrimitiveType.Cube, headGroup, new Vector3(-0.068f, 0.082f, 0.158f), new Vector3(0.062f, 0.018f, 0.018f), hairMat);
            leftBrow.transform.localRotation = Quaternion.Euler(0f, -10f, 8f);
            var rightBrow = CreatePart("RightBrow", PrimitiveType.Cube, headGroup, new Vector3(0.068f, 0.082f, 0.158f), new Vector3(0.062f, 0.018f, 0.018f), hairMat);
            rightBrow.transform.localRotation = Quaternion.Euler(0f, 10f, -8f);
            CreatePart("Mouth", PrimitiveType.Sphere, headGroup, new Vector3(0f, -0.078f, 0.152f), new Vector3(0.065f, 0.018f, 0.018f), mouthMat);

            // Sculpted 3D Hair
            CreatePart("HairCap", PrimitiveType.Sphere, headGroup, new Vector3(0f, 0.075f, -0.025f), new Vector3(0.375f, 0.31f, 0.36f), hairMat);
            CreatePart("HairBackNape", PrimitiveType.Sphere, headGroup, new Vector3(0f, -0.02f, -0.095f), new Vector3(0.34f, 0.22f, 0.22f), hairMat);
            CreatePart("LeftSideburn", PrimitiveType.Sphere, headGroup, new Vector3(-0.165f, 0.02f, 0.02f), new Vector3(0.055f, 0.11f, 0.08f), hairMat);
            CreatePart("RightSideburn", PrimitiveType.Sphere, headGroup, new Vector3(0.165f, 0.02f, 0.02f), new Vector3(0.055f, 0.11f, 0.08f), hairMat);

            if (role == CharacterOutfitRole.FemaleKid)
            {
                Material bandMat = CardArtGenerator.CreateLitMaterial(new Color(0.95f, 0.38f, 0.72f), 0.3f);
                var leftBang = CreatePart("LeftBang", PrimitiveType.Capsule, headGroup, new Vector3(-0.115f, 0.11f, 0.125f), new Vector3(0.08f, 0.085f, 0.07f), hairMat);
                leftBang.transform.localRotation = Quaternion.Euler(15f, -20f, 24f);
                var rightBang = CreatePart("RightBang", PrimitiveType.Capsule, headGroup, new Vector3(0.115f, 0.11f, 0.125f), new Vector3(0.08f, 0.085f, 0.07f), hairMat);
                rightBang.transform.localRotation = Quaternion.Euler(15f, 20f, -24f);
                CreatePart("HairBun", PrimitiveType.Sphere, headGroup, new Vector3(0f, 0.08f, -0.19f), new Vector3(0.16f, 0.16f, 0.16f), hairMat);
                var ponyTip = CreatePart("PonytailTip", PrimitiveType.Capsule, headGroup, new Vector3(0f, -0.02f, -0.22f), new Vector3(0.10f, 0.11f, 0.10f), hairMat);
                ponyTip.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
                CreatePart("Headband", PrimitiveType.Cylinder, headGroup, new Vector3(0f, 0.115f, 0.0f), new Vector3(0.378f, 0.024f, 0.365f), bandMat);
            }
            else if (role == CharacterOutfitRole.Tindera)
            {
                Material scarfMat = CardArtGenerator.CreateLitMaterial(new Color(0.88f, 0.28f, 0.22f), 0.2f);
                CreatePart("Headscarf", PrimitiveType.Sphere, headGroup, new Vector3(0f, 0.08f, -0.028f), new Vector3(0.385f, 0.28f, 0.37f), scarfMat);
                CreatePart("HairBun", PrimitiveType.Sphere, headGroup, new Vector3(0f, 0.02f, -0.19f), new Vector3(0.14f, 0.14f, 0.14f), hairMat);
            }
            else
            {
                // Stylized swept-up front quiff/spikes
                var quiffCenter = CreatePart("QuiffCenter", PrimitiveType.Capsule, headGroup, new Vector3(0.01f, 0.195f, 0.085f), new Vector3(0.11f, 0.10f, 0.10f), hairMat);
                quiffCenter.transform.localRotation = Quaternion.Euler(38f, 8f, -14f);
                var quiffLeft = CreatePart("QuiffLeft", PrimitiveType.Capsule, headGroup, new Vector3(-0.085f, 0.175f, 0.075f), new Vector3(0.085f, 0.085f, 0.085f), hairMat);
                quiffLeft.transform.localRotation = Quaternion.Euler(32f, -18f, 16f);
                var quiffRight = CreatePart("QuiffRight", PrimitiveType.Capsule, headGroup, new Vector3(0.095f, 0.165f, 0.065f), new Vector3(0.085f, 0.085f, 0.085f), hairMat);
                quiffRight.transform.localRotation = Quaternion.Euler(28f, 22f, -24f);
            }

            // Smooth capsule arms with both forearm pivots wired
            actor.LeftArmPivot = CreateArm("LeftArmPivot", torsoPivot, new Vector3(-0.185f, 0.36f, 0f), sleeveMat, skinMat, isRight: false, out Transform leftForearmPivot, out _, out _);
            actor.LeftForearmPivot = leftForearmPivot;
            actor.RightArmPivot = CreateArm("RightArmPivot", torsoPivot, new Vector3(0.185f, 0.36f, 0f), sleeveMat, skinMat, isRight: true, out Transform rightForearmPivot, out GameObject handCard, out Renderer handCardRenderer);
            actor.RightForearmPivot = rightForearmPivot;
            actor.HandCardObject = handCard;
            actor.HandCardRenderer = handCardRenderer;
            actor.SetHandCardVisible(false);

            // Legs (Fitted Jeans OR Knee-Length Shorts) + White-Toe-Cap Canvas Sneakers (or Tsinelas for Tindera)
            actor.LeftLegPivot = CreateLeg("LeftLegPivot", "LeftKneePivot", pelvis, new Vector3(-0.08f, -0.02f, 0f), shortsMat, skinMat, slipperMat, whiteMat, wearJeans, useSneakers, out Transform leftKneePivot);
            actor.RightLegPivot = CreateLeg("RightLegPivot", "RightKneePivot", pelvis, new Vector3(0.08f, -0.02f, 0f), shortsMat, skinMat, slipperMat, whiteMat, wearJeans, useSneakers, out Transform rightKneePivot);
            actor.LeftKneePivot = leftKneePivot;
            actor.RightKneePivot = rightKneePivot;

            return actor;
        }

        private static Transform CreateArm(
            string name,
            Transform parent,
            Vector3 localPos,
            Material sleeveMat,
            Material skinMat,
            bool isRight,
            out Transform forearmPivot,
            out GameObject handCardObj,
            out Renderer handCardRenderer)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPos;

            CreatePart("ShoulderCap", PrimitiveType.Sphere, pivot, Vector3.zero, new Vector3(0.098f, 0.098f, 0.098f), sleeveMat);
            CreatePart("Sleeve", PrimitiveType.Capsule, pivot, new Vector3(0f, -0.065f, 0f), new Vector3(0.095f, 0.085f, 0.095f), sleeveMat);

            forearmPivot = new GameObject(isRight ? "RightForearmPivot" : "LeftForearmPivot").transform;
            forearmPivot.SetParent(pivot, false);
            forearmPivot.localPosition = new Vector3(0f, -0.14f, 0f);

            CreatePart("Forearm", PrimitiveType.Capsule, forearmPivot, new Vector3(0f, -0.07f, 0f), new Vector3(0.072f, 0.11f, 0.072f), skinMat);
            CreatePart("Hand", PrimitiveType.Sphere, forearmPivot, new Vector3(0f, -0.195f, 0f), new Vector3(0.078f, 0.078f, 0.078f), skinMat);

            handCardObj = null;
            handCardRenderer = null;

            if (isRight)
            {
                var cardGo = CreatePart(
                    "HeldTeksCard",
                    PrimitiveType.Cube,
                    forearmPivot,
                    new Vector3(0f, -0.215f, 0.08f),
                    new Vector3(0.09f, 0.008f, 0.13f),
                    CardArtGenerator.CreateLitMaterial(Color.white, 0.3f)
                );
                handCardObj = cardGo;
                handCardRenderer = cardGo.GetComponent<Renderer>();
            }

            return pivot;
        }

        private static Transform CreateLeg(
            string name,
            string kneeName,
            Transform parent,
            Vector3 localPos,
            Material shortsMat,
            Material skinMat,
            Material slipperMat,
            Material whiteMat,
            bool wearJeans,
            bool useSneakers,
            out Transform kneePivot)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPos;

            // Smooth rounded upper leg (jeans or knee-length shorts)
            CreatePart("ShortsLeg", PrimitiveType.Capsule, pivot, new Vector3(0f, -0.125f, 0f), new Vector3(0.125f, 0.145f, 0.130f), shortsMat);

            // Knee pivot placed at bottom of thigh (-0.25f) so standing world offsets remain identical when identity
            kneePivot = new GameObject(kneeName).transform;
            kneePivot.SetParent(pivot, false);
            kneePivot.localPosition = new Vector3(0f, -0.25f, 0f);

            // Lower leg: full-length fitted denim for jeans, or bare calf for knee-length shorts
            if (wearJeans)
            {
                CreatePart("LowerLeg", PrimitiveType.Capsule, kneePivot, new Vector3(0f, -0.115f, 0f), new Vector3(0.098f, 0.165f, 0.100f), shortsMat);
            }
            else
            {
                CreatePart("LowerLeg", PrimitiveType.Capsule, kneePivot, new Vector3(0f, -0.11f, 0f), new Vector3(0.078f, 0.16f, 0.078f), skinMat);
            }

            // White-toe-cap canvas sneakers for kids, or traditional tsinelas for Tindera
            if (useSneakers)
            {
                CreatePart("SneakerSole", PrimitiveType.Cube, kneePivot, new Vector3(0f, -0.328f, 0.035f), new Vector3(0.106f, 0.032f, 0.205f), whiteMat);
                CreatePart("SneakerBody", PrimitiveType.Cube, kneePivot, new Vector3(0f, -0.285f, 0.015f), new Vector3(0.098f, 0.068f, 0.155f), slipperMat);
                CreatePart("SneakerToeCap", PrimitiveType.Sphere, kneePivot, new Vector3(0f, -0.296f, 0.092f), new Vector3(0.100f, 0.064f, 0.095f), whiteMat);
                CreatePart("SneakerLaceStrip", PrimitiveType.Cube, kneePivot, new Vector3(0f, -0.252f, 0.055f), new Vector3(0.052f, 0.014f, 0.068f), whiteMat);
            }
            else
            {
                CreatePart("TsinelasSole", PrimitiveType.Cube, kneePivot, new Vector3(0f, -0.32f, 0.03f), new Vector3(0.10f, 0.03f, 0.19f), slipperMat);
                CreatePart("TsinelasStrap", PrimitiveType.Cube, kneePivot, new Vector3(0f, -0.295f, 0.04f), new Vector3(0.09f, 0.02f, 0.04f), whiteMat);
            }

            return pivot;
        }

        private static GameObject CreatePart(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 localPos,
            Vector3 localScale,
            Material mat)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            var col = go.GetComponent<Collider>();
            if (col != null) SafeDestroy(col);

            var rend = go.GetComponent<Renderer>();
            if (rend != null && mat != null) rend.sharedMaterial = mat;

            return go;
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
