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

        // Expressive face controls (Option 1 stylized character faces)
        public Transform LeftEyebrow;
        public Transform RightEyebrow;
        public GameObject SmileMouth;
        public GameObject HappyOpenMouth;
        public GameObject SadMouth;

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
                CharacterPoseMode.SittingOnCourt => 1.14f,
                CharacterPoseMode.CrouchingFlick => 1.24f,
                CharacterPoseMode.SittingOnBench => 1.50f,
                CharacterPoseMode.Talking => 1.78f,
                _ => 1.78f
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
            Quaternion targetLeftArm = Quaternion.Euler(0f, 0f, 10f);
            Quaternion targetLeftForearm = Quaternion.identity;
            Quaternion targetRightArm = Quaternion.Euler(0f, 0f, -10f);
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
                    targetLeftArm = Quaternion.Euler(-swing * 0.8f, 0f, 10f);
                    targetRightArm = Quaternion.Euler(swing * 0.8f, 0f, -10f);
                }
                else
                {
                    float idle = Mathf.Sin(Time.time * 2.0f) * 1.5f;
                    targetPelvisY = 0.62f + Mathf.Sin(Time.time * 2.0f) * 0.01f;
                    targetTorsoRot = Quaternion.Euler(idle * 0.3f, 0f, 0f);
                    targetLeftArm = Quaternion.Euler(idle, 0f, 9f);
                    targetRightArm = Quaternion.Euler(-idle, 0f, -9f);
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

            UpdateFacialExpression(lerpSpeed);
        }

        private void UpdateFacialExpression(float lerpSpeed)
        {
            bool isWin = _reactionType == ReactionWin && _reactionTimer > 0f;
            bool isLose = (_reactionType == ReactionLose || _reactionType == ReactionTabla) && _reactionTimer > 0f;
            bool isFocused = CurrentPose == CharacterPoseMode.CrouchingFlick && !isWin && !isLose;

            if (SmileMouth != null) SmileMouth.SetActive(!isWin && !isLose);
            if (HappyOpenMouth != null) HappyOpenMouth.SetActive(isWin);
            if (SadMouth != null) SadMouth.SetActive(isLose);

            float targetBrowZ = 0f;
            float targetBrowYOffset = 0f;
            if (isWin)
            {
                targetBrowZ = 8f;
                targetBrowYOffset = 0.010f;
            }
            else if (isLose)
            {
                targetBrowZ = 16f;
                targetBrowYOffset = 0.004f;
            }
            else if (isFocused)
            {
                targetBrowZ = -12f;
                targetBrowYOffset = -0.006f;
            }

            if (LeftEyebrow != null)
            {
                LeftEyebrow.localRotation = Quaternion.Slerp(
                    LeftEyebrow.localRotation,
                    Quaternion.Euler(0f, -8f, targetBrowZ),
                    lerpSpeed);
                Vector3 lp = LeftEyebrow.localPosition;
                lp.y = Mathf.Lerp(lp.y, 0.068f + targetBrowYOffset, lerpSpeed);
                LeftEyebrow.localPosition = lp;
            }

            if (RightEyebrow != null)
            {
                RightEyebrow.localRotation = Quaternion.Slerp(
                    RightEyebrow.localRotation,
                    Quaternion.Euler(0f, 8f, -targetBrowZ),
                    lerpSpeed);
                Vector3 rp = RightEyebrow.localPosition;
                rp.y = Mathf.Lerp(rp.y, 0.068f + targetBrowYOffset, lerpSpeed);
                RightEyebrow.localPosition = rp;
            }
        }
    }

    public static class CharacterVisualBuilder
    {
        private static readonly Color[] HairColorPalette =
        {
            new Color(0.24f, 0.15f, 0.09f), // Warm Option-1 brown
            new Color(0.11f, 0.09f, 0.08f), // Classic Pinoy jet black
            new Color(0.18f, 0.12f, 0.08f), // Dark espresso brown
            new Color(0.30f, 0.19f, 0.11f), // Warm chestnut brown
        };

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

            uint seed = ComputeStableHash(host.name);
            int hairStyle = (int)(seed % 4u);
            Color hairColor = HairColorPalette[(seed >> 3) % (uint)HairColorPalette.Length];
            bool wearLongCuffedPants = role == CharacterOutfitRole.Tindera || ((seed >> 6) % 10u) < 6u;
            bool wearSneakers = role != CharacterOutfitRole.Tindera && (wearLongCuffedPants || ((seed >> 9) % 10u) < 7u);
            bool usePatternedJersey = role != CharacterOutfitRole.Tindera && ((seed >> 12) % 3u) == 0u;

            // Soft toy-like matte URP materials matching Option 1 aesthetic
            Material skinMat = CardArtGenerator.CreateLitMaterial(skinTone, 0.18f);
            Material earInnerMat = CardArtGenerator.CreateLitMaterial(skinTone * 0.86f, 0.14f);
            Material noseMat = CardArtGenerator.CreateLitMaterial(Color.Lerp(skinTone, new Color(0.88f, 0.56f, 0.44f), 0.24f), 0.22f);
            Material cheekMat = CardArtGenerator.CreateLitMaterial(Color.Lerp(skinTone, new Color(0.95f, 0.54f, 0.50f), 0.36f), 0.12f);

            Material shirtMat;
            if (usePatternedJersey)
            {
                int jerseyStyle = (int)((seed >> 14) % 3u);
                Texture2D jerseyTex = CharacterResourceGenerator.GetStreetwearShirtTexture(shirtColor, slipperColor, jerseyStyle);
                shirtMat = CardArtGenerator.CreateLitMaterial(Color.white, 0.16f, 0f, jerseyTex);
            }
            else
            {
                shirtMat = CardArtGenerator.CreateLitMaterial(shirtColor, 0.16f);
            }

            Material sleeveMat = CardArtGenerator.CreateLitMaterial(shirtColor, 0.16f);
            Material collarMat = CardArtGenerator.CreateLitMaterial(Color.Lerp(shirtColor, Color.white, 0.18f), 0.14f);
            Material shortsMat = CardArtGenerator.CreateLitMaterial(shortsColor, 0.14f);
            Material cuffMat = CardArtGenerator.CreateLitMaterial(Color.Lerp(shortsColor, Color.white, 0.20f), 0.14f);
            Material hairMat = CardArtGenerator.CreateLitMaterial(hairColor, 0.24f);
            Material shoeMat = CardArtGenerator.CreateLitMaterial(slipperColor, 0.22f);
            Material whiteMat = CardArtGenerator.CreateLitMaterial(new Color(0.96f, 0.96f, 0.95f), 0.22f);
            Material eyeMat = CardArtGenerator.CreateLitMaterial(new Color(0.11f, 0.08f, 0.07f), 0.85f);
            Material mouthMat = CardArtGenerator.CreateLitMaterial(new Color(0.32f, 0.16f, 0.12f), 0.25f);
            Material openMouthMat = CardArtGenerator.CreateLitMaterial(new Color(0.58f, 0.20f, 0.22f), 0.20f);

            Mesh headMesh = StylizedCharacterMeshFactory.GetHeadMesh();
            Mesh torsoMesh = StylizedCharacterMeshFactory.GetTorsoMesh();
            Mesh hipsMesh = StylizedCharacterMeshFactory.GetHipsMesh();
            Mesh sleeveMesh = StylizedCharacterMeshFactory.GetSleeveMesh();
            Mesh limbMesh = StylizedCharacterMeshFactory.GetTaperedLimbMesh();
            Mesh cuffMesh = StylizedCharacterMeshFactory.GetPantCuffMesh();
            Mesh soleMesh = StylizedCharacterMeshFactory.GetSneakerSoleMesh();
            Mesh upperMesh = StylizedCharacterMeshFactory.GetSneakerUpperMesh();
            Mesh hairCapMesh = StylizedCharacterMeshFactory.GetHairCapMesh();
            Mesh hairTuftMesh = StylizedCharacterMeshFactory.GetHairTuftMesh();
            Mesh arcMesh = StylizedCharacterMeshFactory.GetCurvedArcMesh();
            Mesh sphereMesh = StylizedCharacterMeshFactory.GetSmoothSphereMesh();

            // Pelvis root (exact 0.62f standing height for seamless pose/ground compatibility)
            var pelvis = new GameObject("Pelvis").transform;
            pelvis.SetParent(visualRoot.transform, false);
            pelvis.localPosition = new Vector3(0f, 0.62f, 0f);
            actor.Pelvis = pelvis;

            // Smooth rounded hips
            CreateMeshPart("Hips", hipsMesh, pelvis, new Vector3(0f, 0.02f, 0f), Quaternion.identity, new Vector3(0.32f, 0.17f, 0.28f), shortsMat);

            // Torso
            var torsoPivot = new GameObject("Torso").transform;
            torsoPivot.SetParent(pelvis, false);
            torsoPivot.localPosition = new Vector3(0f, 0.07f, 0f);
            actor.Torso = torsoPivot;

            // Rounded t-shirt body + soft collar ring + smooth neck
            CreateMeshPart("ShirtBody", torsoMesh, torsoPivot, new Vector3(0f, 0.01f, 0f), Quaternion.identity, new Vector3(0.36f, 0.42f, 0.30f), shirtMat);
            CreateMeshPart("CollarTrim", cuffMesh, torsoPivot, new Vector3(0f, 0.405f, 0f), Quaternion.identity, new Vector3(0.15f, 0.032f, 0.14f), collarMat);
            CreateMeshPart("Neck", limbMesh, torsoPivot, new Vector3(0f, 0.445f, 0f), Quaternion.identity, new Vector3(0.115f, 0.095f, 0.115f), skinMat);

            if (role == CharacterOutfitRole.Tindera)
            {
                Material apronMat = CardArtGenerator.CreateLitMaterial(new Color(0.96f, 0.92f, 0.82f), 0.14f);
                Material pocketMat = CardArtGenerator.CreateLitMaterial(new Color(0.88f, 0.34f, 0.26f), 0.14f);
                CreateMeshPart("Apron", torsoMesh, torsoPivot, new Vector3(0f, -0.05f, 0.065f), Quaternion.identity, new Vector3(0.28f, 0.38f, 0.16f), apronMat);
                CreateMeshPart("ApronPocket", hipsMesh, torsoPivot, new Vector3(0f, 0.09f, 0.125f), Quaternion.identity, new Vector3(0.14f, 0.09f, 0.05f), pocketMat);
            }

            // Head (Option 1 big sculpted chibi head)
            var headGroup = new GameObject("HeadGroup").transform;
            headGroup.SetParent(torsoPivot, false);
            headGroup.localPosition = new Vector3(0f, 0.615f, 0f);
            actor.HeadGroup = headGroup;

            CreateMeshPart("Head", headMesh, headGroup, Vector3.zero, Quaternion.identity, new Vector3(0.395f, 0.375f, 0.375f), skinMat);

            // Cute rounded ears with subtle inner ear tint
            CreateMeshPart("LeftEar", sphereMesh, headGroup, new Vector3(-0.190f, -0.015f, -0.012f), Quaternion.Euler(0f, 15f, 6f), new Vector3(0.068f, 0.084f, 0.044f), skinMat);
            CreateMeshPart("LeftEarInner", sphereMesh, headGroup, new Vector3(-0.194f, -0.015f, -0.002f), Quaternion.Euler(0f, 15f, 6f), new Vector3(0.038f, 0.050f, 0.024f), earInnerMat);
            CreateMeshPart("RightEar", sphereMesh, headGroup, new Vector3(0.190f, -0.015f, -0.012f), Quaternion.Euler(0f, -15f, -6f), new Vector3(0.068f, 0.084f, 0.044f), skinMat);
            CreateMeshPart("RightEarInner", sphereMesh, headGroup, new Vector3(0.194f, -0.015f, -0.002f), Quaternion.Euler(0f, -15f, -6f), new Vector3(0.038f, 0.050f, 0.024f), earInnerMat);

            // Button nose
            CreateMeshPart("Nose", sphereMesh, headGroup, new Vector3(0f, -0.020f, 0.174f), Quaternion.identity, new Vector3(0.034f, 0.044f, 0.038f), noseMat);

            // Soft rosy cheeks
            CreateMeshPart("LeftCheek", sphereMesh, headGroup, new Vector3(-0.095f, -0.036f, 0.142f), Quaternion.Euler(0f, -24f, 0f), new Vector3(0.056f, 0.038f, 0.018f), cheekMat);
            CreateMeshPart("RightCheek", sphereMesh, headGroup, new Vector3(0.095f, -0.036f, 0.142f), Quaternion.Euler(0f, 24f, 0f), new Vector3(0.056f, 0.038f, 0.018f), cheekMat);

            // Option-1 glossy vertical oval eyes + crisp white specular catchlight dots
            CreateMeshPart("LeftEye", sphereMesh, headGroup, new Vector3(-0.062f, 0.014f, 0.160f), Quaternion.Euler(0f, -10f, 0f), new Vector3(0.034f, 0.049f, 0.026f), eyeMat);
            CreateMeshPart("RightEye", sphereMesh, headGroup, new Vector3(0.062f, 0.014f, 0.160f), Quaternion.Euler(0f, 10f, 0f), new Vector3(0.034f, 0.049f, 0.026f), eyeMat);
            CreateMeshPart("LeftEyeCatchlight", sphereMesh, headGroup, new Vector3(-0.054f, 0.025f, 0.171f), Quaternion.identity, new Vector3(0.010f, 0.010f, 0.008f), whiteMat);
            CreateMeshPart("RightEyeCatchlight", sphereMesh, headGroup, new Vector3(0.070f, 0.025f, 0.171f), Quaternion.identity, new Vector3(0.010f, 0.010f, 0.008f), whiteMat);

            // Curved 3D eyebrows (animated dynamically in CharacterActor)
            actor.LeftEyebrow = CreateMeshPart("LeftEyebrow", arcMesh, headGroup, new Vector3(-0.064f, 0.068f, 0.158f), Quaternion.Euler(0f, -8f, 0f), new Vector3(0.058f, 0.042f, 0.042f), hairMat).transform;
            actor.RightEyebrow = CreateMeshPart("RightEyebrow", arcMesh, headGroup, new Vector3(0.064f, 0.068f, 0.158f), Quaternion.Euler(0f, 8f, 0f), new Vector3(0.058f, 0.042f, 0.042f), hairMat).transform;

            // Swappable 3D expressive mouths (Smile, Happy Open, Sad/Shocked)
            actor.SmileMouth = CreateMeshPart("SmileMouth", arcMesh, headGroup, new Vector3(0f, -0.062f, 0.160f), Quaternion.identity, new Vector3(0.088f, -0.055f, 0.042f), mouthMat);
            actor.HappyOpenMouth = CreateMeshPart("HappyOpenMouth", sphereMesh, headGroup, new Vector3(0f, -0.070f, 0.158f), Quaternion.identity, new Vector3(0.068f, 0.048f, 0.026f), openMouthMat);
            actor.SadMouth = CreateMeshPart("SadMouth", arcMesh, headGroup, new Vector3(0f, -0.074f, 0.160f), Quaternion.identity, new Vector3(0.072f, 0.048f, 0.042f), mouthMat);
            actor.HappyOpenMouth.SetActive(false);
            actor.SadMouth.SetActive(false);

            // Stylized sculpted hair (varies per role + character name hash)
            BuildHairstyle(headGroup, role, hairStyle, hairCapMesh, hairTuftMesh, sphereMesh, cuffMesh, hairMat, slipperColor);

            // Arms with flared sleeves + mitten hands with thumbs
            actor.LeftArmPivot = CreateArm(
                "LeftArmPivot", torsoPivot, new Vector3(-0.205f, 0.375f, 0f),
                sleeveMesh, limbMesh, sphereMesh, sleeveMat, skinMat,
                isRight: false, out Transform leftForearmPivot, out _, out _);
            actor.LeftForearmPivot = leftForearmPivot;

            actor.RightArmPivot = CreateArm(
                "RightArmPivot", torsoPivot, new Vector3(0.205f, 0.375f, 0f),
                sleeveMesh, limbMesh, sphereMesh, sleeveMat, skinMat,
                isRight: true, out Transform rightForearmPivot, out GameObject handCard, out Renderer handCardRenderer);
            actor.RightForearmPivot = rightForearmPivot;
            actor.HandCardObject = handCard;
            actor.HandCardRenderer = handCardRenderer;
            actor.SetHandCardVisible(false);

            // Legs (Cuffed Option-1 Jeans or Barangay Shorts) + Footwear (Chunky Sneakers or Tsinelas)
            actor.LeftLegPivot = CreateLeg(
                "LeftLegPivot", "LeftKneePivot", pelvis, new Vector3(-0.088f, -0.02f, 0f),
                wearLongCuffedPants, wearSneakers,
                limbMesh, cuffMesh, soleMesh, upperMesh, sphereMesh,
                shortsMat, cuffMat, skinMat, shoeMat, whiteMat, out Transform leftKneePivot);

            actor.RightLegPivot = CreateLeg(
                "RightLegPivot", "RightKneePivot", pelvis, new Vector3(0.088f, -0.02f, 0f),
                wearLongCuffedPants, wearSneakers,
                limbMesh, cuffMesh, soleMesh, upperMesh, sphereMesh,
                shortsMat, cuffMat, skinMat, shoeMat, whiteMat, out Transform rightKneePivot);

            actor.LeftKneePivot = leftKneePivot;
            actor.RightKneePivot = rightKneePivot;

            return actor;
        }

        private static void BuildHairstyle(
            Transform headGroup,
            CharacterOutfitRole role,
            int hairStyle,
            Mesh hairCapMesh,
            Mesh hairTuftMesh,
            Mesh sphereMesh,
            Mesh cuffMesh,
            Material hairMat,
            Color accentColor)
        {
            // Base sculpted hair cap hugging scalp
            CreateMeshPart("HairCap", hairCapMesh, headGroup, new Vector3(0f, 0.015f, -0.008f), Quaternion.identity, new Vector3(0.415f, 0.395f, 0.395f), hairMat);

            // Soft sideburns
            CreateMeshPart("SideburnL", hairTuftMesh, headGroup, new Vector3(-0.175f, 0.055f, 0.035f), Quaternion.Euler(78f, -20f, 10f), new Vector3(0.085f, 0.075f, 0.115f), hairMat);
            CreateMeshPart("SideburnR", hairTuftMesh, headGroup, new Vector3(0.175f, 0.055f, 0.035f), Quaternion.Euler(78f, 20f, -10f), new Vector3(0.085f, 0.075f, 0.115f), hairMat);

            if (role == CharacterOutfitRole.Tindera)
            {
                Material scarfMat = CardArtGenerator.CreateLitMaterial(new Color(0.88f, 0.28f, 0.22f), 0.16f);
                CreateMeshPart("Headscarf", hairCapMesh, headGroup, new Vector3(0f, 0.026f, -0.016f), Quaternion.Euler(-8f, 0f, 0f), new Vector3(0.428f, 0.385f, 0.410f), scarfMat);
                CreateMeshPart("TinderaBun", sphereMesh, headGroup, new Vector3(0f, 0.06f, -0.195f), Quaternion.identity, new Vector3(0.145f, 0.145f, 0.135f), hairMat);
                CreateMeshPart("FrontWaveL", hairTuftMesh, headGroup, new Vector3(-0.04f, 0.145f, 0.07f), Quaternion.Euler(18f, -38f, 0f), new Vector3(0.14f, 0.09f, 0.16f), hairMat);
                CreateMeshPart("FrontWaveR", hairTuftMesh, headGroup, new Vector3(0.04f, 0.145f, 0.07f), Quaternion.Euler(18f, 38f, 0f), new Vector3(0.14f, 0.09f, 0.16f), hairMat);
                return;
            }

            if (role == CharacterOutfitRole.FemaleKid)
            {
                Color tieColor = Color.Lerp(accentColor, new Color(0.96f, 0.38f, 0.66f), 0.45f);
                Material bandMat = CardArtGenerator.CreateLitMaterial(tieColor, 0.22f);

                // Cute swept bangs across forehead
                CreateMeshPart("BangCenter", hairTuftMesh, headGroup, new Vector3(-0.03f, 0.148f, 0.065f), Quaternion.Euler(26f, 18f, 0f), new Vector3(0.16f, 0.10f, 0.18f), hairMat);
                CreateMeshPart("BangLeft", hairTuftMesh, headGroup, new Vector3(-0.09f, 0.135f, 0.055f), Quaternion.Euler(34f, -30f, 0f), new Vector3(0.13f, 0.09f, 0.17f), hairMat);
                CreateMeshPart("BangRight", hairTuftMesh, headGroup, new Vector3(0.08f, 0.138f, 0.055f), Quaternion.Euler(32f, 34f, 0f), new Vector3(0.13f, 0.09f, 0.17f), hairMat);

                int femaleStyle = hairStyle % 3;
                if (femaleStyle == 0)
                {
                    // High bun + colorful headband
                    CreateMeshPart("HairBun", sphereMesh, headGroup, new Vector3(0f, 0.225f, -0.065f), Quaternion.identity, new Vector3(0.165f, 0.155f, 0.165f), hairMat);
                    CreateMeshPart("BunScrunchie", cuffMesh, headGroup, new Vector3(0f, 0.175f, -0.050f), Quaternion.Euler(-22f, 0f, 0f), new Vector3(0.125f, 0.042f, 0.125f), bandMat);
                    CreateMeshPart("Headband", cuffMesh, headGroup, new Vector3(0f, 0.105f, 0.005f), Quaternion.Euler(-16f, 0f, 0f), new Vector3(0.395f, 0.034f, 0.375f), bandMat);
                }
                else if (femaleStyle == 1)
                {
                    // Twin playful pigtails
                    CreateMeshPart("PigtailL", hairTuftMesh, headGroup, new Vector3(-0.185f, 0.085f, -0.065f), Quaternion.Euler(65f, -72f, 0f), new Vector3(0.16f, 0.15f, 0.24f), hairMat);
                    CreateMeshPart("PigtailR", hairTuftMesh, headGroup, new Vector3(0.185f, 0.085f, -0.065f), Quaternion.Euler(65f, 72f, 0f), new Vector3(0.16f, 0.15f, 0.24f), hairMat);
                    CreateMeshPart("TieL", sphereMesh, headGroup, new Vector3(-0.185f, 0.095f, -0.055f), Quaternion.identity, new Vector3(0.065f, 0.065f, 0.065f), bandMat);
                    CreateMeshPart("TieR", sphereMesh, headGroup, new Vector3(0.185f, 0.095f, -0.055f), Quaternion.identity, new Vector3(0.065f, 0.065f, 0.065f), bandMat);
                }
                else
                {
                    // Rounded cute bob + headband
                    CreateMeshPart("BobBack", sphereMesh, headGroup, new Vector3(0f, -0.025f, -0.065f), Quaternion.identity, new Vector3(0.415f, 0.335f, 0.335f), hairMat);
                    CreateMeshPart("Headband", cuffMesh, headGroup, new Vector3(0f, 0.108f, 0.008f), Quaternion.Euler(-14f, 0f, 0f), new Vector3(0.398f, 0.036f, 0.378f), bandMat);
                }
                return;
            }

            // MaleKid: 4 distinct sculpted Option-1 styles
            if (hairStyle == 0)
            {
                // Exact Option 1: Swept layered chunky tufts on top & front
                CreateMeshPart("TuftMain1", hairTuftMesh, headGroup, new Vector3(-0.07f, 0.155f, 0.02f), Quaternion.Euler(-12f, 26f, -6f), new Vector3(0.20f, 0.15f, 0.24f), hairMat);
                CreateMeshPart("TuftMain2", hairTuftMesh, headGroup, new Vector3(0.03f, 0.165f, 0.00f), Quaternion.Euler(-18f, 16f, 4f), new Vector3(0.19f, 0.15f, 0.23f), hairMat);
                CreateMeshPart("TuftSide", hairTuftMesh, headGroup, new Vector3(-0.12f, 0.135f, 0.03f), Quaternion.Euler(14f, -22f, 12f), new Vector3(0.14f, 0.12f, 0.19f), hairMat);
                CreateMeshPart("TuftRight", hairTuftMesh, headGroup, new Vector3(0.11f, 0.135f, 0.02f), Quaternion.Euler(12f, 38f, -10f), new Vector3(0.14f, 0.11f, 0.19f), hairMat);
                CreateMeshPart("TuftCrown", hairTuftMesh, headGroup, new Vector3(-0.02f, 0.175f, -0.06f), Quaternion.Euler(-28f, 8f, 0f), new Vector3(0.21f, 0.14f, 0.22f), hairMat);
                CreateMeshPart("TuftBack", hairTuftMesh, headGroup, new Vector3(0f, 0.12f, -0.11f), Quaternion.Euler(-10f, 180f, 0f), new Vector3(0.19f, 0.12f, 0.16f), hairMat);
            }
            else if (hairStyle == 1)
            {
                // Energetic spiky barangay cut
                CreateMeshPart("SpikeCenter", hairTuftMesh, headGroup, new Vector3(0f, 0.165f, 0.04f), Quaternion.Euler(-36f, 0f, 0f), new Vector3(0.17f, 0.15f, 0.23f), hairMat);
                CreateMeshPart("SpikeLeft", hairTuftMesh, headGroup, new Vector3(-0.09f, 0.150f, 0.03f), Quaternion.Euler(-28f, -26f, 10f), new Vector3(0.15f, 0.13f, 0.20f), hairMat);
                CreateMeshPart("SpikeRight", hairTuftMesh, headGroup, new Vector3(0.09f, 0.150f, 0.03f), Quaternion.Euler(-28f, 26f, -10f), new Vector3(0.15f, 0.13f, 0.20f), hairMat);
                CreateMeshPart("SpikeTop", hairTuftMesh, headGroup, new Vector3(0f, 0.175f, -0.05f), Quaternion.Euler(-48f, 0f, 0f), new Vector3(0.18f, 0.14f, 0.21f), hairMat);
            }
            else if (hairStyle == 2)
            {
                // Clean side-parted rounded bangs
                CreateMeshPart("SweepLarge", hairTuftMesh, headGroup, new Vector3(-0.08f, 0.148f, 0.05f), Quaternion.Euler(18f, 36f, 0f), new Vector3(0.21f, 0.13f, 0.22f), hairMat);
                CreateMeshPart("SweepMid", hairTuftMesh, headGroup, new Vector3(0.02f, 0.155f, 0.03f), Quaternion.Euler(12f, 24f, 0f), new Vector3(0.18f, 0.12f, 0.20f), hairMat);
                CreateMeshPart("SweepEdge", hairTuftMesh, headGroup, new Vector3(0.11f, 0.130f, 0.04f), Quaternion.Euler(26f, 44f, 0f), new Vector3(0.13f, 0.10f, 0.17f), hairMat);
                CreateMeshPart("SweepTop", hairTuftMesh, headGroup, new Vector3(-0.02f, 0.168f, -0.04f), Quaternion.Euler(-12f, 14f, 0f), new Vector3(0.22f, 0.13f, 0.20f), hairMat);
            }
            else
            {
                // Street-kid headband + messy top tufts
                Material bandMat = CardArtGenerator.CreateLitMaterial(accentColor, 0.18f);
                CreateMeshPart("StreetHeadband", cuffMesh, headGroup, new Vector3(0f, 0.098f, 0.004f), Quaternion.Euler(-10f, 0f, 0f), new Vector3(0.405f, 0.044f, 0.388f), bandMat);
                CreateMeshPart("Messy1", hairTuftMesh, headGroup, new Vector3(-0.05f, 0.165f, 0.02f), Quaternion.Euler(-24f, 22f, 0f), new Vector3(0.18f, 0.14f, 0.22f), hairMat);
                CreateMeshPart("Messy2", hairTuftMesh, headGroup, new Vector3(0.06f, 0.162f, 0.01f), Quaternion.Euler(-20f, -18f, 0f), new Vector3(0.17f, 0.14f, 0.21f), hairMat);
                CreateMeshPart("Messy3", hairTuftMesh, headGroup, new Vector3(0f, 0.172f, -0.06f), Quaternion.Euler(-34f, 0f, 0f), new Vector3(0.19f, 0.13f, 0.20f), hairMat);
            }
        }

        private static Transform CreateArm(
            string name,
            Transform parent,
            Vector3 localPos,
            Mesh sleeveMesh,
            Mesh limbMesh,
            Mesh sphereMesh,
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

            // Flared rounded short sleeve
            CreateMeshPart("Sleeve", sleeveMesh, pivot, new Vector3(0f, 0.01f, 0f), Quaternion.identity, new Vector3(0.125f, 0.165f, 0.125f), sleeveMat);

            forearmPivot = new GameObject(isRight ? "RightForearmPivot" : "LeftForearmPivot").transform;
            forearmPivot.SetParent(pivot, false);
            forearmPivot.localPosition = new Vector3(0f, -0.15f, 0f);

            // Smooth tapered forearm
            CreateMeshPart("Forearm", limbMesh, forearmPivot, new Vector3(0f, -0.085f, 0f), Quaternion.identity, new Vector3(0.082f, 0.21f, 0.082f), skinMat);

            // Option-1 rounded mitten hand with sculpted thumb
            CreateMeshPart("Hand", sphereMesh, forearmPivot, new Vector3(0f, -0.205f, 0f), Quaternion.identity, new Vector3(0.095f, 0.098f, 0.088f), skinMat);
            float thumbX = isRight ? -0.042f : 0.042f;
            float thumbRotZ = isRight ? 38f : -38f;
            CreateMeshPart("Thumb", limbMesh, forearmPivot, new Vector3(thumbX, -0.188f, 0.022f), Quaternion.Euler(14f, 0f, thumbRotZ), new Vector3(0.034f, 0.058f, 0.034f), skinMat);

            handCardObj = null;
            handCardRenderer = null;

            if (isRight)
            {
                var cardGo = CreatePrimitivePart(
                    "HeldTeksCard",
                    PrimitiveType.Cube,
                    forearmPivot,
                    new Vector3(0f, -0.22f, 0.08f),
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
            bool wearLongCuffedPants,
            bool wearSneakers,
            Mesh limbMesh,
            Mesh cuffMesh,
            Mesh soleMesh,
            Mesh upperMesh,
            Mesh sphereMesh,
            Material pantsMat,
            Material cuffMat,
            Material skinMat,
            Material shoeMat,
            Material whiteMat,
            out Transform kneePivot)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPos;

            // Upper leg (smooth rounded pant thigh or shorts leg)
            CreateMeshPart("UpperLeg", limbMesh, pivot, new Vector3(0f, -0.125f, 0f), Quaternion.identity, new Vector3(0.135f, 0.28f, 0.138f), pantsMat);

            // Knee pivot placed at -0.25f so standing world offsets stay identical to existing animation poses
            kneePivot = new GameObject(kneeName).transform;
            kneePivot.SetParent(pivot, false);
            kneePivot.localPosition = new Vector3(0f, -0.25f, 0f);

            if (wearLongCuffedPants)
            {
                // Option 1 cuffed jeans/pants leg + chunky rolled cuff at ankle
                CreateMeshPart("LowerPantLeg", limbMesh, kneePivot, new Vector3(0f, -0.125f, 0f), Quaternion.identity, new Vector3(0.126f, 0.27f, 0.128f), pantsMat);
                CreateMeshPart("PantCuff", cuffMesh, kneePivot, new Vector3(0f, -0.245f, 0f), Quaternion.identity, new Vector3(0.142f, 0.062f, 0.144f), cuffMat);
            }
            else
            {
                // Sporty shorts hem + smooth calf
                CreateMeshPart("ShortsHem", cuffMesh, pivot, new Vector3(0f, -0.235f, 0f), Quaternion.identity, new Vector3(0.138f, 0.032f, 0.140f), cuffMat);
                CreateMeshPart("LowerLeg", limbMesh, kneePivot, new Vector3(0f, -0.135f, 0f), Quaternion.identity, new Vector3(0.088f, 0.27f, 0.088f), skinMat);
            }

            if (wearSneakers)
            {
                // Option 1 chunky rounded toy sneaker (white rubber sole + colored upper + white toe cap + laces)
                // Sole bottom sits at y = -0.35f below kneePivot -> 0.62 - 0.02 - 0.25 - 0.35 = 0.00f ground contact!
                CreateMeshPart("SneakerSole", soleMesh, kneePivot, new Vector3(0f, -0.332f, 0.032f), Quaternion.identity, new Vector3(0.132f, 0.036f, 0.215f), whiteMat);
                CreateMeshPart("SneakerUpper", upperMesh, kneePivot, new Vector3(0f, -0.318f, 0.028f), Quaternion.identity, new Vector3(0.124f, 0.098f, 0.198f), shoeMat);
                CreateMeshPart("SneakerToeCap", sphereMesh, kneePivot, new Vector3(0f, -0.304f, 0.102f), Quaternion.identity, new Vector3(0.104f, 0.044f, 0.064f), whiteMat);
                CreateMeshPart("SneakerLace1", cuffMesh, kneePivot, new Vector3(0f, -0.278f, 0.052f), Quaternion.Euler(24f, 0f, 0f), new Vector3(0.064f, 0.016f, 0.022f), whiteMat);
                CreateMeshPart("SneakerLace2", cuffMesh, kneePivot, new Vector3(0f, -0.264f, 0.034f), Quaternion.Euler(28f, 0f, 0f), new Vector3(0.060f, 0.016f, 0.022f), whiteMat);
            }
            else
            {
                // Stylized rounded tsinelas slipper (ground contact at y = -0.35f)
                CreateMeshPart("TsinelasSole", soleMesh, kneePivot, new Vector3(0f, -0.334f, 0.028f), Quaternion.identity, new Vector3(0.118f, 0.032f, 0.205f), shoeMat);
                CreateMeshPart("Foot", sphereMesh, kneePivot, new Vector3(0f, -0.300f, 0.022f), Quaternion.identity, new Vector3(0.095f, 0.056f, 0.155f), skinMat);
                CreateMeshPart("TsinelasStrap", cuffMesh, kneePivot, new Vector3(0f, -0.285f, 0.045f), Quaternion.Euler(18f, 0f, 0f), new Vector3(0.102f, 0.024f, 0.046f), whiteMat);
            }

            return pivot;
        }

        private static GameObject CreateMeshPart(
            string name,
            Mesh mesh,
            Transform parent,
            Vector3 localPos,
            Quaternion localRot,
            Vector3 localScale,
            Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;

            return go;
        }

        private static GameObject CreatePrimitivePart(
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

        private static uint ComputeStableHash(string input)
        {
            string s = string.IsNullOrEmpty(input) ? "teks_kid" : input;
            uint hash = 2166136261u;
            for (int i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= 16777619u;
            }
            return hash;
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
