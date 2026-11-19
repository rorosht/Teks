using UnityEngine;
using TeksMaster.Core;
using TeksMaster.World;

namespace TeksMaster.Characters
{
    public class NpcOpponent : MonoBehaviour
    {
        public string NpcName = "AI Gelo";
        public AIDifficulty Difficulty = AIDifficulty.Medium;
        public string FavoriteCardId = "bakunawa";
        public int CardStarRank = 3;
        public int BaseCoinWager = 100;

        public bool IsWandering = false;
        public bool IsSeated = false;
        public Vector3 WanderCenter;
        public float WanderRadius = 2.5f;
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

        private CharacterActor _actor;
        private Vector3 _targetPoint;
        private float _waitTimer;
        private Transform _playerTransform;
        private float _speechSeed;

        private void Start()
        {
            _speechSeed = Random.Range(0f, 12f);
            _actor = GetComponent<CharacterActor>();
            if (WanderCenter == Vector3.zero)
            {
                WanderCenter = transform.position;
            }
            PickNextWaypoint();

            if (_actor != null && IsSeated)
            {
                _actor.SetPose(CharacterPoseMode.SittingOnCourt);
            }
        }

        public void SetPlayerTransform(Transform player)
        {
            _playerTransform = player;
        }

        public string GetDisplayTag()
        {
            string diffLabel = Difficulty switch
            {
                AIDifficulty.Easy => "Easy",
                AIDifficulty.Medium => "Med",
                AIDifficulty.Hard => "Hard",
                AIDifficulty.Boss => "Boss",
                _ => "Easy"
            };
            return $"{NpcName} ({diffLabel})";
        }

        public Color GetDifficultyColor()
        {
            return Difficulty switch
            {
                AIDifficulty.Easy => new Color(0.20f, 0.85f, 0.42f),
                AIDifficulty.Medium => new Color(0.98f, 0.72f, 0.18f),
                AIDifficulty.Hard => new Color(0.95f, 0.34f, 0.25f),
                AIDifficulty.Boss => new Color(0.76f, 0.28f, 0.98f),
                _ => Color.white
            };
        }

        public float GetAiChubProbability()
        {
            return Difficulty switch
            {
                AIDifficulty.Easy => 0.34f,
                AIDifficulty.Medium => 0.44f,
                AIDifficulty.Hard => 0.54f,
                AIDifficulty.Boss => 0.62f,
                _ => 0.40f
            };
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
            if (AmbientCallouts == null || AmbientCallouts.Length == 0)
                return ChallengeQuote;
            int idx = Mathf.FloorToInt((Time.time + _speechSeed) / 4.5f) % AmbientCallouts.Length;
            return AmbientCallouts[Mathf.Abs(idx) % AmbientCallouts.Length];
        }

        private void Update()
        {
            if (_actor == null) _actor = GetComponent<CharacterActor>();
            if (InMatch || IsSeated)
            {
                if (_actor != null) _actor.SetNormalizedSpeed(0f);
                return;
            }

            if (_playerTransform != null && Vector3.Distance(transform.position, _playerTransform.position) < 3.2f)
            {
                if (_actor != null) _actor.SetNormalizedSpeed(0f);
                LookAtFlat(_playerTransform.position);
                return;
            }

            if (!IsWandering)
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
                transform.position += dir * (1.55f * Time.deltaTime);
                LookAtFlat(transform.position + dir);
                if (_actor != null) _actor.SetNormalizedSpeed(0.55f);
            }
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
            Vector2 rnd = Random.insideUnitCircle * WanderRadius;
            _targetPoint = WanderCenter + new Vector3(rnd.x, 0f, rnd.y);
            _targetPoint.x = Mathf.Clamp(_targetPoint.x, -29f, 29f);
            _targetPoint.z = Mathf.Clamp(_targetPoint.z, -21f, 16f);
        }
    }
}
