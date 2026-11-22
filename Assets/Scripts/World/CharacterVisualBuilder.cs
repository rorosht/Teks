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
        public Transform RightArmPivot;
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
        private int _reactionType = ReactionNone;
        private float _reactionTimer;
        private float _reactionDuration;

        public void SetPose(CharacterPoseMode pose)
        {
            CurrentPose = pose;
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
            _isPlayingFlick = true;
            _flickAnimTimer = 0f;
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
            Quaternion targetRightArm = Quaternion.Euler(0f, 0f, -8f);

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
                targetPelvisY = 0.34f;
                targetTorsoRot = Quaternion.Euler(32f, 0f, 0f);
                targetLeftLeg = Quaternion.Euler(-64f, -12f, 0f);
                targetRightLeg = Quaternion.Euler(-54f, 12f, 0f);
                targetLeftArm = Quaternion.Euler(-28f, 0f, 16f);

                if (_isPlayingFlick)
                {
                    _flickAnimTimer += dt;
                    float t = Mathf.Clamp01(_flickAnimTimer / 0.25f);
                    float snap = Mathf.Sin(t * Mathf.PI);
                    targetRightArm = Quaternion.Euler(-75f - snap * 35f, -10f, -6f);
                    if (_flickAnimTimer >= 0.35f)
                    {
                        _isPlayingFlick = false;
                    }
                }
                else
                {
                    float readyBob = Mathf.Sin(Time.time * 3.5f) * 2.5f;
                    targetRightArm = Quaternion.Euler(-68f + readyBob, -12f, -6f);
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
                        float hop = Mathf.Abs(Mathf.Sin(elapsed * 9.0f));
                        float cheerWave = Mathf.Sin(elapsed * 12.0f) * 12f;
                        if (!isSeatedPose)
                        {
                            targetPelvisY += hop * 0.16f;
                            targetLeftLeg = Quaternion.Euler(-hop * 14f, 0f, 0f);
                            targetRightLeg = Quaternion.Euler(-hop * 14f, 0f, 0f);
                            targetLeftKnee = Quaternion.Euler(hop * 20f, 0f, 0f);
                            targetRightKnee = Quaternion.Euler(hop * 20f, 0f, 0f);
                        }
                        else
                        {
                            targetPelvisY += hop * 0.04f;
                        }

                        targetTorsoRot = Quaternion.Euler(-8f, 0f, Mathf.Sin(elapsed * 6.0f) * 5f);
                        targetHeadRot = Quaternion.Euler(-10f + Mathf.Sin(elapsed * 9.0f) * 5f, 0f, 0f);
                        targetLeftArm = Quaternion.Euler(-152f + cheerWave, 10f, 22f);
                        targetRightArm = Quaternion.Euler(-152f - cheerWave, -10f, -22f);
                    }
                    else if (_reactionType == ReactionLose)
                    {
                        float shake = Mathf.Sin(elapsed * 7.5f) * 18f;
                        if (!isSeatedPose)
                        {
                            targetPelvisY = Mathf.Max(0.46f, targetPelvisY - 0.07f);
                        }

                        targetTorsoRot = Quaternion.Euler(22f, 0f, 0f);
                        targetHeadRot = Quaternion.Euler(18f, shake, 0f);
                        targetLeftArm = Quaternion.Euler(14f, 0f, 12f);
                        targetRightArm = Quaternion.Euler(14f, 0f, -12f);
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

            float lerpSpeed = Mathf.Clamp01(dt * 12f);
            Pelvis.localPosition = Vector3.Lerp(Pelvis.localPosition, new Vector3(0f, targetPelvisY, 0f), lerpSpeed);
            if (Torso != null) Torso.localRotation = Quaternion.Slerp(Torso.localRotation, targetTorsoRot, lerpSpeed);
            if (HeadGroup != null) HeadGroup.localRotation = Quaternion.Slerp(HeadGroup.localRotation, targetHeadRot, lerpSpeed);
            if (LeftLegPivot != null) LeftLegPivot.localRotation = Quaternion.Slerp(LeftLegPivot.localRotation, targetLeftLeg, lerpSpeed);
            if (RightLegPivot != null) RightLegPivot.localRotation = Quaternion.Slerp(RightLegPivot.localRotation, targetRightLeg, lerpSpeed);
            if (LeftKneePivot != null) LeftKneePivot.localRotation = Quaternion.Slerp(LeftKneePivot.localRotation, targetLeftKnee, lerpSpeed);
            if (RightKneePivot != null) RightKneePivot.localRotation = Quaternion.Slerp(RightKneePivot.localRotation, targetRightKnee, lerpSpeed);
            if (LeftArmPivot != null) LeftArmPivot.localRotation = Quaternion.Slerp(LeftArmPivot.localRotation, targetLeftArm, lerpSpeed);
            if (RightArmPivot != null) RightArmPivot.localRotation = Quaternion.Slerp(RightArmPivot.localRotation, targetRightArm, lerpSpeed);
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

            int jerseyStyle = Mathf.Abs(host.name.GetHashCode()) % 3;
            Texture2D jerseyTex = CharacterResourceGenerator.GetStreetwearShirtTexture(shirtColor, slipperColor, jerseyStyle);
            Texture2D faceTex = CharacterResourceGenerator.GetCharacterFaceTexture(skinTone);

            Material skinMat = CardArtGenerator.CreateLitMaterial(skinTone, 0.25f);
            Material headSkinMat = CardArtGenerator.CreateLitMaterial(Color.white, 0.22f, 0f, faceTex);
            Material shirtMat = CardArtGenerator.CreateLitMaterial(Color.white, 0.22f, 0f, jerseyTex);
            Material sleeveMat = CardArtGenerator.CreateLitMaterial(shirtColor, 0.2f);
            Material shortsMat = CardArtGenerator.CreateLitMaterial(shortsColor, 0.15f);
            Material hairMat = CardArtGenerator.CreateLitMaterial(new Color(0.11f, 0.09f, 0.08f), 0.3f);
            Material slipperMat = CardArtGenerator.CreateLitMaterial(slipperColor, 0.2f);
            Material whiteMat = CardArtGenerator.CreateLitMaterial(Color.white, 0.3f);
            Material eyeMat = CardArtGenerator.CreateLitMaterial(new Color(0.08f, 0.08f, 0.08f), 0.6f);

            // Pelvis root
            var pelvis = new GameObject("Pelvis").transform;
            pelvis.SetParent(visualRoot.transform, false);
            pelvis.localPosition = new Vector3(0f, 0.62f, 0f);
            actor.Pelvis = pelvis;

            // Hip block (shorts waist)
            CreatePart("Hips", PrimitiveType.Cube, pelvis, new Vector3(0f, 0.02f, 0f), new Vector3(0.30f, 0.14f, 0.20f), shortsMat);

            // Torso
            var torsoPivot = new GameObject("Torso").transform;
            torsoPivot.SetParent(pelvis, false);
            torsoPivot.localPosition = new Vector3(0f, 0.08f, 0f);
            actor.Torso = torsoPivot;

            CreatePart("ShirtBody", PrimitiveType.Cube, torsoPivot, new Vector3(0f, 0.22f, 0f), new Vector3(0.32f, 0.42f, 0.21f), shirtMat);
            CreatePart("Neck", PrimitiveType.Cylinder, torsoPivot, new Vector3(0f, 0.45f, 0f), new Vector3(0.11f, 0.06f, 0.11f), skinMat);

            if (role == CharacterOutfitRole.Tindera)
            {
                Material apronMat = CardArtGenerator.CreateLitMaterial(new Color(0.95f, 0.90f, 0.78f), 0.15f);
                CreatePart("Apron", PrimitiveType.Cube, torsoPivot, new Vector3(0f, 0.14f, 0.11f), new Vector3(0.28f, 0.42f, 0.03f), apronMat);
            }

            // Head
            var headGroup = new GameObject("HeadGroup").transform;
            headGroup.SetParent(torsoPivot, false);
            headGroup.localPosition = new Vector3(0f, 0.60f, 0f);
            actor.HeadGroup = headGroup;

            CreatePart("Head", PrimitiveType.Sphere, headGroup, Vector3.zero, new Vector3(0.27f, 0.29f, 0.27f), headSkinMat);
            CreatePart("HairCap", PrimitiveType.Sphere, headGroup, new Vector3(0f, 0.045f, -0.02f), new Vector3(0.28f, 0.25f, 0.28f), hairMat);

            // Eyes
            CreatePart("LeftEyeWhite", PrimitiveType.Sphere, headGroup, new Vector3(-0.055f, 0.01f, 0.118f), new Vector3(0.042f, 0.042f, 0.02f), whiteMat);
            CreatePart("RightEyeWhite", PrimitiveType.Sphere, headGroup, new Vector3(0.055f, 0.01f, 0.118f), new Vector3(0.042f, 0.042f, 0.02f), whiteMat);
            CreatePart("LeftPupil", PrimitiveType.Sphere, headGroup, new Vector3(-0.055f, 0.01f, 0.128f), new Vector3(0.024f, 0.024f, 0.02f), eyeMat);
            CreatePart("RightPupil", PrimitiveType.Sphere, headGroup, new Vector3(0.055f, 0.01f, 0.128f), new Vector3(0.024f, 0.024f, 0.02f), eyeMat);

            if (role == CharacterOutfitRole.FemaleKid)
            {
                Material bandMat = CardArtGenerator.CreateLitMaterial(new Color(0.95f, 0.38f, 0.72f), 0.3f);
                CreatePart("HairBun", PrimitiveType.Sphere, headGroup, new Vector3(0f, 0.14f, -0.06f), new Vector3(0.14f, 0.14f, 0.14f), hairMat);
                CreatePart("Headband", PrimitiveType.Cylinder, headGroup, new Vector3(0f, 0.08f, 0f), new Vector3(0.285f, 0.022f, 0.285f), bandMat);
            }
            else if (role == CharacterOutfitRole.Tindera)
            {
                Material scarfMat = CardArtGenerator.CreateLitMaterial(new Color(0.88f, 0.28f, 0.22f), 0.2f);
                CreatePart("Headscarf", PrimitiveType.Sphere, headGroup, new Vector3(0f, 0.06f, -0.03f), new Vector3(0.29f, 0.23f, 0.29f), scarfMat);
            }
            else
            {
                // Spiky/swept boy hair tuft
                CreatePart("FrontHairTuft", PrimitiveType.Cube, headGroup, new Vector3(0f, 0.13f, 0.05f), new Vector3(0.18f, 0.07f, 0.14f), hairMat);
            }

            // Arms
            actor.LeftArmPivot = CreateArm("LeftArmPivot", torsoPivot, new Vector3(-0.21f, 0.39f, 0f), sleeveMat, skinMat, isRight: false, out _, out _);
            actor.RightArmPivot = CreateArm("RightArmPivot", torsoPivot, new Vector3(0.21f, 0.39f, 0f), sleeveMat, skinMat, isRight: true, out GameObject handCard, out Renderer handCardRenderer);
            actor.HandCardObject = handCard;
            actor.HandCardRenderer = handCardRenderer;
            actor.SetHandCardVisible(false);

            // Legs + Basketball Shorts + Tsinelas Slippers
            actor.LeftLegPivot = CreateLeg("LeftLegPivot", "LeftKneePivot", pelvis, new Vector3(-0.085f, -0.02f, 0f), shortsMat, skinMat, slipperMat, whiteMat, out Transform leftKneePivot);
            actor.RightLegPivot = CreateLeg("RightLegPivot", "RightKneePivot", pelvis, new Vector3(0.085f, -0.02f, 0f), shortsMat, skinMat, slipperMat, whiteMat, out Transform rightKneePivot);
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
            out GameObject handCardObj,
            out Renderer handCardRenderer)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPos;

            CreatePart("Sleeve", PrimitiveType.Cube, pivot, new Vector3(0f, -0.07f, 0f), new Vector3(0.10f, 0.16f, 0.10f), sleeveMat);
            CreatePart("Forearm", PrimitiveType.Capsule, pivot, new Vector3(0f, -0.22f, 0f), new Vector3(0.075f, 0.14f, 0.075f), skinMat);
            CreatePart("Hand", PrimitiveType.Sphere, pivot, new Vector3(0f, -0.35f, 0f), new Vector3(0.08f, 0.08f, 0.08f), skinMat);

            handCardObj = null;
            handCardRenderer = null;

            if (isRight)
            {
                var cardGo = CreatePart(
                    "HeldTeksCard",
                    PrimitiveType.Cube,
                    pivot,
                    new Vector3(0f, -0.37f, 0.08f),
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
            Material stripeMat,
            out Transform kneePivot)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPos;

            // Basketball shorts leg + sporty side stripe
            CreatePart("ShortsLeg", PrimitiveType.Cube, pivot, new Vector3(0f, -0.12f, 0f), new Vector3(0.13f, 0.26f, 0.14f), shortsMat);
            float stripeX = localPos.x < 0f ? -0.068f : 0.068f;
            CreatePart("ShortsStripe", PrimitiveType.Cube, pivot, new Vector3(stripeX, -0.12f, 0f), new Vector3(0.01f, 0.24f, 0.04f), stripeMat);

            // Knee pivot placed at bottom of shorts (-0.25f) so standing world offsets remain identical when identity
            kneePivot = new GameObject(kneeName).transform;
            kneePivot.SetParent(pivot, false);
            kneePivot.localPosition = new Vector3(0f, -0.25f, 0f);

            // Calf (-0.25f + -0.11f = -0.36f)
            CreatePart("LowerLeg", PrimitiveType.Capsule, kneePivot, new Vector3(0f, -0.11f, 0f), new Vector3(0.08f, 0.16f, 0.08f), skinMat);

            // Tsinelas (flip-flop slipper) (-0.25f + -0.32f = -0.57f; -0.25f + -0.295f = -0.545f)
            CreatePart("TsinelasSole", PrimitiveType.Cube, kneePivot, new Vector3(0f, -0.32f, 0.03f), new Vector3(0.10f, 0.03f, 0.19f), slipperMat);
            CreatePart("TsinelasStrap", PrimitiveType.Cube, kneePivot, new Vector3(0f, -0.295f, 0.04f), new Vector3(0.09f, 0.02f, 0.04f), stripeMat);

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
