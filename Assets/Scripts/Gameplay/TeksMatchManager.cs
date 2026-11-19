using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TeksMaster.Core;
using TeksMaster.Characters;
using TeksMaster.Visuals;
using TeksMaster.World;

namespace TeksMaster.Gameplay
{
    public enum MatchState
    {
        Idle,
        WagerScreen,
        ReadyToFlick,
        FlickInAir,
        RoundComplete
    }

    public class TeksMatchManager : MonoBehaviour
    {
        public static TeksMatchManager Instance { get; private set; }

        public MatchState State { get; private set; } = MatchState.Idle;
        public NpcOpponent ActiveOpponent { get; private set; }
        public int CurrentCoinWager { get; private set; } = 100;

        // Needle runs -1f .. +1f
        public float NeedleValue { get; private set; }
        public float SweetSpotHalfWidth { get; private set; } = 0.16f;
        public float GoodZoneHalfWidth => 0.44f;

        public string LastFlickRating { get; private set; } = string.Empty;
        public string RoundResultTitle { get; private set; } = string.Empty;
        public string RoundResultSubtitle { get; private set; } = string.Empty;
        public bool PlayerWonLastRound { get; private set; }
        public bool WasTablaLastRound { get; private set; }

        public bool PlayerCardFaceUp { get; private set; }
        public bool OpponentCardFaceUp { get; private set; }
        public float FlickAirElapsed { get; private set; }

        public TeksCardPhysics PlayerPhysicalCard => _playerCard;
        public TeksCardPhysics OpponentPhysicalCard => _opponentCard;

        public event Action OnMatchStateChanged;

        private readonly List<TeksCardPhysics> _spawnedCards = new List<TeksCardPhysics>();
        private TeksCardPhysics _playerCard;
        private TeksCardPhysics _opponentCard;

        private Vector3 _savedPlayerPos;
        private Quaternion _savedPlayerRot;
        private Vector3 _savedOpponentPos;
        private Quaternion _savedOpponentRot;
        private bool _savedOpponentSeated;

        private Vector3 _matchOrigin = new Vector3(-2.4f, 0f, -3.2f);
        private Vector3 _matchForward = Vector3.forward;
        private Vector3 _matchRight = Vector3.right;
        private float _needlePhase;
        private float _needleSpeed = 2.65f;
        private float _tablaRearmTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void StartChallenge(NpcOpponent npc)
        {
            if (npc == null || State != MatchState.Idle) return;

            ActiveOpponent = npc;
            int playerCoins = PlayerInventory.Instance != null ? PlayerInventory.Instance.Coins : 100;
            CurrentCoinWager = Mathf.Clamp(npc.BaseCoinWager, 25, Mathf.Max(25, playerCoins));
            LastFlickRating = string.Empty;
            RoundResultTitle = string.Empty;
            RoundResultSubtitle = string.Empty;
            WasTablaLastRound = false;

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.MovementLocked = true;
            }

            State = MatchState.WagerScreen;
            OnMatchStateChanged?.Invoke();
        }

        public void CancelChallenge()
        {
            CleanupMatchCards();
            RestoreCharactersAfterMatch();

            State = MatchState.Idle;
            ActiveOpponent = null;
            LastFlickRating = string.Empty;

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.MovementLocked = false;
            }

            if (Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<CameraFollowController>();
                camFollow?.ClearMatchCamera();
            }

            OnMatchStateChanged?.Invoke();
        }

        public void BeginFlickMatch(int coinWager)
        {
            if (ActiveOpponent == null) return;

            int playerCoins = PlayerInventory.Instance != null ? PlayerInventory.Instance.Coins : 100;
            CurrentCoinWager = Mathf.Clamp(coinWager, 25, Mathf.Max(25, playerCoins));

            SaveAndPositionCharacters();
            ArmFlickMeter();
        }

        private void SaveAndPositionCharacters()
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                _savedPlayerPos = player.transform.position;
                _savedPlayerRot = player.transform.rotation;
                player.MovementLocked = true;
            }

            // Keep the battle right in the exact area where the player challenged the AI NPC
            Vector3 oppAnchor = ActiveOpponent != null ? ActiveOpponent.transform.position : _savedPlayerPos + Vector3.forward * 2.2f;
            oppAnchor.y = 0f;

            Vector3 playerStart = player != null ? player.transform.position : oppAnchor - Vector3.forward * 2.2f;
            playerStart.y = 0f;

            Vector3 dirToOpp = oppAnchor - playerStart;
            dirToOpp.y = 0f;
            if (dirToOpp.sqrMagnitude < 0.05f)
            {
                dirToOpp = player != null ? player.transform.forward : Vector3.forward;
                dirToOpp.y = 0f;
            }
            _matchForward = dirToOpp.sqrMagnitude > 0.001f ? dirToOpp.normalized : Vector3.forward;
            _matchRight = Vector3.Cross(Vector3.up, _matchForward).normalized;
            _matchOrigin = oppAnchor - _matchForward * 2.15f;
            _matchOrigin.y = 0f;

            if (player != null)
            {
                Quaternion playerRot = Quaternion.LookRotation(_matchForward, Vector3.up);
                player.TeleportForMatch(_matchOrigin, playerRot);

                if (player.Actor != null)
                {
                    player.Actor.SetPose(CharacterPoseMode.CrouchingFlick);
                    TeksCardDefinition def = null;
                    var owned = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetEquippedPamato(out def) : null;
                    var tex = def != null ? CardArtGenerator.GetCardPortraitTexture(def, owned != null ? owned.StarRank : 1) : null;
                    player.Actor.SetHandCardVisible(true, tex);
                }
            }

            if (ActiveOpponent != null)
            {
                _savedOpponentPos = ActiveOpponent.transform.position;
                _savedOpponentRot = ActiveOpponent.transform.rotation;
                _savedOpponentSeated = ActiveOpponent.IsSeated;

                ActiveOpponent.InMatch = true;
                ActiveOpponent.transform.position = oppAnchor;
                ActiveOpponent.transform.rotation = Quaternion.LookRotation(-_matchForward, Vector3.up);

                var oppActor = ActiveOpponent.GetComponent<CharacterActor>();
                if (oppActor != null)
                {
                    oppActor.SetPose(CharacterPoseMode.CrouchingFlick);
                    oppActor.SetHandCardVisible(false);
                }
            }

            SetMatchPreparationCamera();
        }

        private void SetMatchPreparationCamera()
        {
            if (Camera.main == null) return;
            var camFollow = Camera.main.GetComponent<CameraFollowController>();
            if (camFollow == null) return;

            // Close-up over-the-shoulder angle focused on the player's flick hand & landing spot in the current area
            Vector3 camPos = _matchOrigin - _matchForward * 1.45f - _matchRight * 0.85f + Vector3.up * 1.55f;
            Vector3 lookTarget = _matchOrigin + _matchForward * 1.10f + Vector3.up * 0.18f;
            camFollow.SetMatchCamera(camPos, lookTarget);
        }

        private void RestoreCharactersAfterMatch()
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                if (player.Actor != null)
                {
                    player.Actor.SetHandCardVisible(false);
                    player.Actor.SetPose(CharacterPoseMode.StandingOrWalking);
                }
                if (_savedPlayerPos != Vector3.zero)
                {
                    player.TeleportForMatch(_savedPlayerPos, _savedPlayerRot);
                }
            }

            if (ActiveOpponent != null)
            {
                ActiveOpponent.InMatch = false;
                if (_savedOpponentPos != Vector3.zero)
                {
                    ActiveOpponent.transform.position = _savedOpponentPos;
                    ActiveOpponent.transform.rotation = _savedOpponentRot;
                }
                var oppActor = ActiveOpponent.GetComponent<CharacterActor>();
                if (oppActor != null)
                {
                    oppActor.SetPose(_savedOpponentSeated ? CharacterPoseMode.SittingOnCourt : CharacterPoseMode.StandingOrWalking);
                }
            }

            _savedPlayerPos = Vector3.zero;
            _savedOpponentPos = Vector3.zero;
        }

        private void ArmFlickMeter()
        {
            CleanupMatchCards();
            WasTablaLastRound = false;
            LastFlickRating = string.Empty;

            var player = PlayerController.Instance;
            TeksCardDefinition def = null;
            var owned = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetEquippedPamato(out def) : null;
            float streakBonus = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetStreakSweetSpotBonus() : 0f;
            SweetSpotHalfWidth = 0.15f + (owned != null ? owned.GetFlickSweetSpotBonus() : 0f) + streakBonus;

            float diffFactor = ActiveOpponent != null ? ActiveOpponent.Difficulty switch
            {
                AIDifficulty.Easy => 2.25f,
                AIDifficulty.Medium => 2.75f,
                AIDifficulty.Hard => 3.25f,
                AIDifficulty.Boss => 3.70f,
                _ => 2.65f
            } : 2.65f;
            _needleSpeed = diffFactor;
            _needlePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            if (player != null && player.Actor != null)
            {
                player.Actor.SetPose(CharacterPoseMode.CrouchingFlick);
                var tex = def != null ? CardArtGenerator.GetCardPortraitTexture(def, owned != null ? owned.StarRank : 1) : null;
                player.Actor.SetHandCardVisible(true, tex);
            }

            SetMatchPreparationCamera();

            State = MatchState.ReadyToFlick;
            OnMatchStateChanged?.Invoke();
        }

        public void ExecuteFlick()
        {
            if (State != MatchState.ReadyToFlick) return;

            float absVal = Mathf.Abs(NeedleValue);
            float powerFactor;
            float accuracyBonus;

            if (absVal <= SweetSpotHalfWidth)
            {
                powerFactor = 1.0f;
                accuracyBonus = 0.28f;
                LastFlickRating = "SWEET SPOT! MAX SPIN & POWER!";
            }
            else if (absVal <= GoodZoneHalfWidth)
            {
                powerFactor = 0.80f;
                accuracyBonus = 0.12f;
                LastFlickRating = "GOOD FLICK!";
            }
            else
            {
                powerFactor = 0.58f;
                accuracyBonus = -0.05f;
                LastFlickRating = "WEAK FLICK!";
            }

            var player = PlayerController.Instance;
            Vector3 handPos = _matchOrigin + new Vector3(0.24f, 0.56f, 0.34f);
            Vector3 forwardDir = new Vector3(0.32f, 0f, 0.95f).normalized;

            if (player != null)
            {
                handPos = player.transform.position + player.transform.right * 0.22f + Vector3.up * 0.56f + player.transform.forward * 0.36f;
                forwardDir = player.transform.forward;
                if (player.Actor != null)
                {
                    player.Actor.SetHandCardVisible(false);
                    player.Actor.TriggerFlickAnimation();
                }
            }

            var ownedPamato = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetEquippedPamato(out _) : null;
            string playerCardId = ownedPamato != null ? ownedPamato.CardId : "hero";
            int playerRank = ownedPamato != null ? ownedPamato.StarRank : 5;
            float playerLuck = ownedPamato != null ? ownedPamato.GetLuckRatingBonus() : 0.15f;

            string oppCardId = ActiveOpponent != null ? ActiveOpponent.FavoriteCardId : "bakunawa";
            int oppRank = ActiveOpponent != null ? ActiveOpponent.CardStarRank : 3;
            float oppChubProb = ActiveOpponent != null ? ActiveOpponent.GetAiChubProbability() : 0.44f;

            float playerChubProb = Mathf.Clamp(0.42f + accuracyBonus + playerLuck, 0.25f, 0.90f);

            CleanupMatchCards();
            FlickAirElapsed = 0f;

            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

            // 1. Player's Pamato Shooter Card (Left trajectory)
            _playerCard = TeksCardPhysics.SpawnPhysicalCard(
                playerCardId, playerRank,
                handPos - rightDir * 0.08f, Quaternion.Euler(15f, player != null ? player.transform.eulerAngles.y : 0f, 0f),
                isPlayerPamato: true, isOpponentPamato: false);
            _playerCard.RoleLabel = "YOUR PAMATO";
            _spawnedCards.Add(_playerCard);
            _playerCard.LaunchFlick(
                forwardDir * (1.80f * powerFactor) + Vector3.up * (3.25f * powerFactor) - rightDir * 0.38f,
                new Vector3(26f * powerFactor, 7f, 14f),
                playerChubProb);

            // 2. Opponent's Pamato Card (Right trajectory)
            _opponentCard = TeksCardPhysics.SpawnPhysicalCard(
                oppCardId, oppRank,
                handPos + rightDir * 0.08f, Quaternion.Euler(-12f, 25f, 0f),
                isPlayerPamato: false, isOpponentPamato: true);
            _opponentCard.RoleLabel = "OPPONENT";
            _spawnedCards.Add(_opponentCard);
            _opponentCard.LaunchFlick(
                forwardDir * (1.90f * powerFactor) + Vector3.up * (3.18f * powerFactor) + rightDir * 0.42f,
                new Vector3(-24f * powerFactor, -9f, 16f),
                oppChubProb);

            // Zoom smoothly toward the landing spot right in front of the player in this same area
            if (Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<CameraFollowController>();
                if (camFollow != null)
                {
                    Vector3 camPos = _matchOrigin - _matchForward * 0.45f - _matchRight * 0.35f + Vector3.up * 1.38f;
                    Vector3 lookTarget = _matchOrigin + _matchForward * 1.25f + Vector3.up * 0.08f;
                    camFollow.SetMatchCamera(camPos, lookTarget);
                }
            }

            State = MatchState.FlickInAir;
            OnMatchStateChanged?.Invoke();
        }

        private void Update()
        {
            if (State == MatchState.ReadyToFlick)
            {
                _needlePhase += Time.deltaTime * _needleSpeed;
                NeedleValue = Mathf.Sin(_needlePhase);

                var kb = Keyboard.current;
                if (kb != null && kb.spaceKey.wasPressedThisFrame)
                {
                    ExecuteFlick();
                }
            }
            else if (State == MatchState.FlickInAir)
            {
                FlickAirElapsed += Time.deltaTime;
                bool allSettled = _spawnedCards.Count > 0;
                for (int i = 0; i < _spawnedCards.Count; i++)
                {
                    if (_spawnedCards[i] != null && !_spawnedCards[i].HasSettled)
                    {
                        allSettled = false;
                        break;
                    }
                }

                if (allSettled)
                {
                    EvaluateRoundOutcome();
                }
            }
            else if (State == MatchState.RoundComplete && WasTablaLastRound)
            {
                _tablaRearmTimer -= Time.deltaTime;
                if (_tablaRearmTimer <= 0f)
                {
                    ArmFlickMeter();
                }
            }
        }

        private void EvaluateRoundOutcome()
        {
            PlayerCardFaceUp = _playerCard != null && _playerCard.IsFaceUp;
            OpponentCardFaceUp = _opponentCard != null && _opponentCard.IsFaceUp;

            var inv = PlayerInventory.Instance;

            if (PlayerCardFaceUp && !OpponentCardFaceUp)
            {
                // Player = Chub, Opponent = Cha -> PLAYER WINS!
                PlayerWonLastRound = true;
                WasTablaLastRound = false;

                string wonCardId = ActiveOpponent != null ? ActiveOpponent.FavoriteCardId : "bantay_kalye";
                var wonDef = TeksDatabase.GetCard(wonCardId);
                string wonName = wonDef != null ? wonDef.Name : wonCardId;

                int payout = CurrentCoinWager;
                string streakNote = string.Empty;
                if (inv != null)
                {
                    inv.RecordMatchResult(true);
                    float mult = inv.GetStreakCoinMultiplier();
                    payout = Mathf.RoundToInt(CurrentCoinWager * mult);
                    if (inv.WinStreak >= 2)
                    {
                        streakNote = $" [HOT HAND x{inv.WinStreak}!]";
                    }
                    inv.AddCoins(payout);
                    inv.AddCard(wonCardId, 1);
                }

                RoundResultTitle = "YOU WIN! (CHUB vs CHA)";
                RoundResultSubtitle = $"Swept +{payout} Coins{streakNote} & won {wonName}!";
            }
            else if (!PlayerCardFaceUp && OpponentCardFaceUp)
            {
                // Player = Cha, Opponent = Chub -> OPPONENT WINS!
                PlayerWonLastRound = false;
                WasTablaLastRound = false;

                if (inv != null)
                {
                    inv.TrySpendCoins(CurrentCoinWager);
                    inv.RecordMatchResult(false);
                }

                string oppName = ActiveOpponent != null ? ActiveOpponent.NpcName : "Opponent";
                RoundResultTitle = $"{oppName.ToUpper()} WINS! (CHA vs CHUB)";
                RoundResultSubtitle = $"Lost {CurrentCoinWager} Barya Coins to {oppName}.";
            }
            else
            {
                // Tabla (both Chub or both Cha) -> automatic re-flick!
                PlayerWonLastRound = false;
                WasTablaLastRound = true;
                _tablaRearmTimer = 2.60f;

                RoundResultTitle = PlayerCardFaceUp ? "TABLA! (BOTH CHUB)" : "TABLA! (BOTH CHA)";
                RoundResultSubtitle = "Tie! Both shooter cards landed the same — re-flicking!";
            }

            State = MatchState.RoundComplete;
            OnMatchStateChanged?.Invoke();
        }

        public void Rematch()
        {
            if (ActiveOpponent == null) return;
            int playerCoins = PlayerInventory.Instance != null ? PlayerInventory.Instance.Coins : 0;
            if (playerCoins < 25)
            {
                PlayerInventory.Instance?.AddCoins(50);
            }
            CurrentCoinWager = Mathf.Min(CurrentCoinWager, PlayerInventory.Instance != null ? PlayerInventory.Instance.Coins : CurrentCoinWager);
            ArmFlickMeter();
        }

        private void CleanupMatchCards()
        {
            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                if (_spawnedCards[i] != null)
                {
                    Destroy(_spawnedCards[i].gameObject);
                }
            }
            _spawnedCards.Clear();
            _playerCard = null;
            _opponentCard = null;
        }
    }
}
