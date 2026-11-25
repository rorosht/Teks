using System.Text;
using UnityEngine;
using TeksMaster.Core;
using TeksMaster.World;

namespace TeksMaster.Characters
{
    public class NpcOpponent : MonoBehaviour
    {
        public string NpcId = "";
        public string NpcName = "AI Gelo";
        public int CharacterPrefabNumber = 7;
        public AIDifficulty Difficulty = AIDifficulty.Medium;
        public BarangayAreaId AreaId = BarangayAreaId.BasketballCourt;
        public bool IsStoryOpponent = false;
        public bool IsChampion = false;
        public int StoryOrder = 0;
        public string FavoriteCardId = "bakunawa";
        public int CardStarRank = 3;
        public int BaseCoinWager = 100;

        public bool IsWandering = false;
        public bool IsSeated = false;
        public bool IsBenchSitter = false;
        public Vector3 WanderCenter;
        public float WanderRadius = 2.5f;
        public Vector3[] PatrolWaypoints;
        public Bounds PatrolBounds;
        public bool InMatch = false;

        public string ChallengeQuote = "Hoy! Teks tayo! Kaya mo ba Pamato ko?";
        public string[] AmbientCallouts = {
            "Hoy! Teks tayo!",
            "Tara 1v1! Pusta na!",
            "Chub 'to siguradong panalo!"
        };
        public string WinBanter = "Haha! Chub akin! Akin na barya mo!";
        public string LoseBanter = "Grabe ang lakas ng Pamato mo! Rematch!";
        public string TablaBanter = "Tabla! Isa pa, walang atrasan!";

        private const float TalkingProximityRadius = 3.2f;
        private const float BanterOverrideDuration = 5.0f;

        private CharacterActor _actor;
        private Vector3 _targetPoint;
        private float _waitTimer;
        private Transform _playerTransform;
        private float _speechSeed;
        private int _patrolWaypointIndex;
        private string _banterOverride;
        private float _banterOverrideTimer;

        private void Start()
        {
            _speechSeed = Random.Range(0f, 12f);
            _actor = GetComponent<CharacterActor>();
            if (WanderCenter == Vector3.zero)
            {
                WanderCenter = transform.position;
            }

            EnsureZoneBoundsInitialized();
            PickNextWaypoint();

            if (_actor != null && (IsSeated || IsBenchSitter))
            {
                _actor.SetPose(GetRestingPose());
            }
        }

        public void SetPlayerTransform(Transform player)
        {
            _playerTransform = player;
        }

        public string GetEffectiveNpcId()
        {
            if (!string.IsNullOrWhiteSpace(NpcId))
            {
                return NpcId.Trim().ToLowerInvariant();
            }

            if (string.IsNullOrWhiteSpace(NpcName))
            {
                return "npc_unknown";
            }

            string raw = NpcName.Trim().ToLowerInvariant();
            var sb = new StringBuilder(raw.Length);
            bool lastUnderscore = false;
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lastUnderscore = false;
                }
                else if (!lastUnderscore && sb.Length > 0)
                {
                    sb.Append('_');
                    lastUnderscore = true;
                }
            }

            string slug = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(slug) ? "npc_unknown" : slug;
        }

        public CharacterPoseMode GetRestingPose()
        {
            if (IsBenchSitter)
            {
                return CharacterPoseMode.SittingOnBench;
            }
            if (IsSeated)
            {
                return CharacterPoseMode.SittingOnCourt;
            }
            return CharacterPoseMode.StandingOrWalking;
        }

        public void SetPatrolBoundsXZ(float minX, float maxX, float minZ, float maxZ)
        {
            float x0 = Mathf.Min(minX, maxX);
            float x1 = Mathf.Max(minX, maxX);
            float z0 = Mathf.Min(minZ, maxZ);
            float z1 = Mathf.Max(minZ, maxZ);
            Bounds b = new Bounds();
            b.SetMinMax(new Vector3(x0, -2f, z0), new Vector3(x1, 4f, z1));
            PatrolBounds = b;
        }

        public static BarangayAreaId InferAreaFromPosition(Vector3 worldPos)
        {
            if (worldPos.x <= -14.5f && worldPos.z <= -20.8f)
            {
                return BarangayAreaId.TournamentArena;
            }
            if (worldPos.x >= 14.5f && worldPos.z <= -20.8f)
            {
                return BarangayAreaId.FiestaArena;
            }
            if (worldPos.x <= -14.5f && worldPos.z > -8.4f)
            {
                return BarangayAreaId.OldNeighborhood;
            }
            if (worldPos.x >= 14.5f && worldPos.z > -11.5f)
            {
                return BarangayAreaId.PlayersNeighborhood;
            }
            if (worldPos.x > -14.5f && worldPos.x < 14.5f && worldPos.z > -8.4f)
            {
                return BarangayAreaId.BasketballCourt;
            }
            return BarangayAreaId.SariSariAndKalye;
        }

        public static Bounds GetDefaultZoneBounds(BarangayAreaId area)
        {
            Bounds b = new Bounds();
            switch (area)
            {
                case BarangayAreaId.PlayersNeighborhood:
                    b.SetMinMax(new Vector3(16.0f, -2f, -10.5f), new Vector3(27.5f, 4f, 11.5f));
                    break;
                case BarangayAreaId.SariSariAndKalye:
                    b.SetMinMax(new Vector3(-13.0f, -2f, -20.2f), new Vector3(13.0f, 4f, -9.2f));
                    break;
                case BarangayAreaId.BasketballCourt:
                    b.SetMinMax(new Vector3(-13.0f, -2f, -7.0f), new Vector3(13.0f, 4f, 7.8f));
                    break;
                case BarangayAreaId.OldNeighborhood:
                    b.SetMinMax(new Vector3(-27.5f, -2f, -7.6f), new Vector3(-16.0f, 4f, 10.5f));
                    break;
                case BarangayAreaId.FiestaArena:
                    b.SetMinMax(new Vector3(16.0f, -2f, -35.5f), new Vector3(30.5f, 4f, -22.0f));
                    break;
                case BarangayAreaId.TournamentArena:
                    b.SetMinMax(new Vector3(-29.5f, -2f, -35.5f), new Vector3(-16.0f, 4f, -22.0f));
                    break;
                default:
                    b.SetMinMax(new Vector3(-30.0f, -2f, -36.5f), new Vector3(30.0f, 4f, 16.0f));
                    break;
            }
            return b;
        }

        public string GetDisplayTag()
        {
            bool defeated = PlayerInventory.Instance != null
                && PlayerInventory.Instance.HasDefeatedNpc(GetEffectiveNpcId());
            string checkSuffix = defeated ? " \u2713" : string.Empty;

            if (IsChampion)
            {
                return $"[CHAMPION] {NpcName}{checkSuffix}";
            }

            string diffLabel = Difficulty switch
            {
                AIDifficulty.Easy => "Easy",
                AIDifficulty.Medium => "Med",
                AIDifficulty.Hard => "Hard",
                AIDifficulty.Boss => "Boss",
                AIDifficulty.Extreme => "Extreme",
                _ => "Easy"
            };

            if (IsStoryOpponent)
            {
                return $"[STORY] {NpcName} ({diffLabel}){checkSuffix}";
            }

            return $"{NpcName} ({diffLabel}){checkSuffix}";
        }

        public Color GetDifficultyColor()
        {
            if (IsChampion)
            {
                return new Color(1.00f, 0.80f, 0.16f);
            }

            return Difficulty switch
            {
                AIDifficulty.Easy => new Color(0.20f, 0.85f, 0.42f),
                AIDifficulty.Medium => new Color(0.98f, 0.72f, 0.18f),
                AIDifficulty.Hard => new Color(0.95f, 0.34f, 0.25f),
                AIDifficulty.Boss => new Color(0.76f, 0.28f, 0.98f),
                AIDifficulty.Extreme => new Color(0.96f, 0.16f, 0.56f),
                _ => Color.white
            };
        }

        public float GetAiChubProbability()
        {
            return Difficulty switch
            {
                AIDifficulty.Easy => 0.40f,
                AIDifficulty.Medium => 0.50f,
                AIDifficulty.Hard => 0.65f,
                AIDifficulty.Boss => 0.80f,
                AIDifficulty.Extreme => 0.80f,
                _ => 0.40f
            };
        }

        public void PlayBattleReaction(bool npcWon, bool wasTabla)
        {
            int reactionType;
            string banter;
            if (wasTabla)
            {
                reactionType = CharacterActor.ReactionTabla;
                banter = !string.IsNullOrEmpty(TablaBanter)
                    ? TablaBanter
                    : "Tabla! Isa pa, walang atrasan!";
            }
            else if (npcWon)
            {
                reactionType = CharacterActor.ReactionWin;
                banter = !string.IsNullOrEmpty(WinBanter)
                    ? WinBanter
                    : "Haha! Chub akin! Akin na barya mo!";
            }
            else
            {
                reactionType = CharacterActor.ReactionLose;
                banter = !string.IsNullOrEmpty(LoseBanter)
                    ? LoseBanter
                    : "Grabe ang lakas ng Pamato mo! Rematch!";
            }

            _banterOverride = banter;
            _banterOverrideTimer = BanterOverrideDuration;

            if (_actor == null) _actor = GetComponent<CharacterActor>();
            if (_actor != null)
            {
                _actor.PlayReactionPose(reactionType, 2.2f);
            }
        }

        public Vector3 GetTagWorldPosition()
        {
            if (_actor != null) return _actor.GetHeadTagWorldPosition();
            return transform.position + Vector3.up * 1.7f;
        }

        public bool IsPlayerWithin(float maxDist)
        {
            if (_playerTransform == null) return false;
            return Vector3.Distance(transform.position, _playerTransform.position) <= maxDist;
        }

        public string GetActiveCallout()
        {
            if (_banterOverrideTimer > 0f && !string.IsNullOrEmpty(_banterOverride))
            {
                return _banterOverride;
            }

            if (AmbientCallouts == null || AmbientCallouts.Length == 0)
                return ChallengeQuote;
            int idx = Mathf.FloorToInt((Time.time + _speechSeed) / 4.5f) % AmbientCallouts.Length;
            return AmbientCallouts[Mathf.Abs(idx) % AmbientCallouts.Length];
        }

        private void Update()
        {
            if (_actor == null) _actor = GetComponent<CharacterActor>();

            if (!InMatch && _banterOverrideTimer > 0f)
            {
                _banterOverrideTimer -= Time.deltaTime;
                if (_banterOverrideTimer <= 0f)
                {
                    _banterOverrideTimer = 0f;
                    _banterOverride = null;
                }
            }

            if (InMatch)
            {
                if (_actor != null) _actor.SetNormalizedSpeed(0f);
                return;
            }

            if (IsSeated || IsBenchSitter)
            {
                if (_actor != null)
                {
                    _actor.SetNormalizedSpeed(0f);
                    CharacterPoseMode restPose = GetRestingPose();
                    if (_actor.CurrentPose != restPose)
                    {
                        _actor.SetPose(restPose);
                    }
                }
                return;
            }

            if (_playerTransform != null && Vector3.Distance(transform.position, _playerTransform.position) < TalkingProximityRadius)
            {
                if (_actor != null)
                {
                    _actor.SetNormalizedSpeed(0f);
                    if (_actor.CurrentPose != CharacterPoseMode.Talking)
                    {
                        _actor.SetPose(CharacterPoseMode.Talking);
                    }
                }
                LookAtFlat(_playerTransform.position);
                return;
            }

            if (_actor != null && _actor.CurrentPose != CharacterPoseMode.StandingOrWalking)
            {
                _actor.SetPose(CharacterPoseMode.StandingOrWalking);
            }

            bool hasPatrolWaypoints = PatrolWaypoints != null && PatrolWaypoints.Length > 0;
            if (!IsWandering && !hasPatrolWaypoints)
            {
                if (_actor != null) _actor.SetNormalizedSpeed(0f);
                return;
            }

            if (_waitTimer > 0f)
            {
                _waitTimer -= Time.deltaTime;
                if (_actor != null) _actor.SetNormalizedSpeed(0f);
                return;
            }

            Vector3 toTarget = _targetPoint - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist < 0.25f)
            {
                _waitTimer = Random.Range(1.5f, 3.8f);
                PickNextWaypoint();
                if (_actor != null) _actor.SetNormalizedSpeed(0f);
            }
            else
            {
                Vector3 dir = toTarget / dist;
                float step = Mathf.Min(dist, 1.55f * Time.deltaTime);
                Vector3 nextPos = transform.position + dir * step;
                nextPos = ClampPointToBoundsXZ(nextPos, GetEffectivePatrolBounds());
                transform.position = nextPos;
                LookAtFlat(transform.position + dir);
                if (_actor != null) _actor.SetNormalizedSpeed(0.55f);
            }
        }

        private void EnsureZoneBoundsInitialized()
        {
            if (PatrolBounds.size.sqrMagnitude > 0.01f)
            {
                return;
            }

            BarangayAreaId inferredArea = InferAreaFromPosition(WanderCenter);
            Bounds currentAreaBounds = GetDefaultZoneBounds(AreaId);
            if (!IsPointInsideBoundsXZ(WanderCenter, currentAreaBounds))
            {
                AreaId = inferredArea;
            }

            PatrolBounds = GetDefaultZoneBounds(AreaId);
        }

        private Bounds GetEffectivePatrolBounds()
        {
            Bounds b = PatrolBounds.size.sqrMagnitude > 0.01f
                ? PatrolBounds
                : GetDefaultZoneBounds(InferAreaFromPosition(WanderCenter));

            Vector3 flatCenter = new Vector3(WanderCenter.x, b.center.y, WanderCenter.z);
            if (!IsPointInsideBoundsXZ(flatCenter, b))
            {
                b.Encapsulate(flatCenter);
            }
            return b;
        }

        private static bool IsPointInsideBoundsXZ(Vector3 point, Bounds bounds)
        {
            return point.x >= bounds.min.x && point.x <= bounds.max.x
                && point.z >= bounds.min.z && point.z <= bounds.max.z;
        }

        private static Vector3 ClampPointToBoundsXZ(Vector3 point, Bounds bounds)
        {
            float minX = Mathf.Clamp(bounds.min.x, -30f, 30f);
            float maxX = Mathf.Clamp(bounds.max.x, minX, 30f);
            float minZ = Mathf.Clamp(bounds.min.z, -36.5f, 16f);
            float maxZ = Mathf.Clamp(bounds.max.z, minZ, 16f);
            point.x = Mathf.Clamp(point.x, minX, maxX);
            point.z = Mathf.Clamp(point.z, minZ, maxZ);
            return point;
        }

        private void LookAtFlat(Vector3 worldTarget)
        {
            Vector3 flat = worldTarget - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flat.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
            }
        }

        private void PickNextWaypoint()
        {
            Bounds zoneBounds = GetEffectivePatrolBounds();
            if (PatrolWaypoints != null && PatrolWaypoints.Length > 0)
            {
                Vector3 wp = PatrolWaypoints[_patrolWaypointIndex % PatrolWaypoints.Length];
                _patrolWaypointIndex = (_patrolWaypointIndex + 1) % PatrolWaypoints.Length;
                wp.y = transform.position.y;
                _targetPoint = ClampPointToBoundsXZ(wp, zoneBounds);
                return;
            }

            Vector2 rnd = Random.insideUnitCircle * WanderRadius;
            Vector3 candidate = WanderCenter + new Vector3(rnd.x, 0f, rnd.y);
            candidate.y = transform.position.y;
            _targetPoint = ClampPointToBoundsXZ(candidate, zoneBounds);
        }
    }
}
