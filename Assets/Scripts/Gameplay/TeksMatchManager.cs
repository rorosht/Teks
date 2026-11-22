using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TeksMaster.Audio;
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

        public const float PlayerChubProbability = 0.50f;

        public MatchState State { get; private set; } = MatchState.Idle;
        public NpcOpponent ActiveOpponent { get; private set; }
        public int CurrentCoinWager { get; private set; } = 25;
        public int LastCoinReward { get; private set; }
        public IReadOnlyList<BarangayAreaId> LastNewlyUnlockedAreas => _lastNewlyUnlockedAreas;

        // Needle runs -1f .. +1f (drives launch power/spin/juice only; never alters win probability)
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

        // Challenge Mode card restriction state
        public bool HasCardRestriction { get; private set; }
        public CardRarity? MaxAllowedRarity { get; private set; }
        public string CardRestrictionLabel { get; private set; } = string.Empty;

        public event Action OnMatchStateChanged;
        public event Action<NpcOpponent, bool, bool> OnMatchResolved;

        private readonly List<TeksCardPhysics> _spawnedCards = new List<TeksCardPhysics>();
        private readonly List<BarangayAreaId> _lastNewlyUnlockedAreas = new List<BarangayAreaId>();
        private TeksCardPhysics _playerCard;
        private TeksCardPhysics _opponentCard;

        private Vector3 _savedPlayerPos;
        private Quaternion _savedPlayerRot;
        private Vector3 _savedOpponentPos;
        private Quaternion _savedOpponentRot;
        private bool _hasSavedCharacterTransforms;

        private Vector3 _matchOrigin = new Vector3(-2.4f, 0f, -3.2f);
        private Vector3 _matchForward = Vector3.forward;
        private Vector3 _matchRight = Vector3.right;
        private Vector3 _flightCamFocus;
        private bool _flightCamInitialized;
        private float _needlePhase;
        private float _needleSpeed = 2.65f;
        private float _tablaRearmTimer;
        private string _selectedPamatoIdOverride;

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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static int GetBaseCoinReward(NpcOpponent npc)
        {
            if (npc == null) return 25;
            if (npc.IsChampion) return 250;
            return npc.Difficulty switch
            {
                AIDifficulty.Easy => 25,
                AIDifficulty.Medium => 50,
                AIDifficulty.Hard => 80,
                AIDifficulty.Boss => 135,
                AIDifficulty.Extreme => 135,
                _ => 25
            };
        }

        // --- Pre-Battle Pamato Selection & Challenge Card Restriction API ---

        public void SetCardRestriction(CardRarity maxAllowedRarity, string label = null)
        {
            HasCardRestriction = true;
            MaxAllowedRarity = maxAllowedRarity;
            CardRestrictionLabel = string.IsNullOrEmpty(label)
                ? $"{TeksDatabase.GetRarityName(maxAllowedRarity)} Only"
                : label;
            EnsureValidSelectedPamato();
        }

        public void ClearCardRestriction()
        {
            HasCardRestriction = false;
            MaxAllowedRarity = null;
            CardRestrictionLabel = string.Empty;
            _selectedPamatoIdOverride = null;
        }

        public bool IsCardAllowedForMatch(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return false;
            var def = TeksDatabase.GetCard(cardId);
            return IsCardAllowedForMatch(def);
        }

        public bool IsCardAllowedForMatch(TeksCardDefinition def)
        {
            if (def == null) return false;
            if (!HasCardRestriction || !MaxAllowedRarity.HasValue) return true;
            return def.Rarity <= MaxAllowedRarity.Value;
        }

        public List<OwnedTeksCard> GetAllowedPamatoCards()
        {
            var result = new List<OwnedTeksCard>();
            var inv = PlayerInventory.Instance;
            if (inv == null) return result;

            var owned = inv.OwnedCards;
            for (int i = 0; i < owned.Count; i++)
            {
                var card = owned[i];
                if (card != null && IsCardAllowedForMatch(card.CardId))
                {
                    result.Add(card);
                }
            }
            return result;
        }

        public bool HasAnyAllowedPamatoCard()
        {
            return GetAllowedPamatoCards().Count > 0;
        }

        public bool TrySelectPamatoCard(string cardId)
        {
            return TrySelectPamatoCard(cardId, out _);
        }

        public bool TrySelectPamatoCard(string cardId, out string message)
        {
            var inv = PlayerInventory.Instance;
            if (inv == null)
            {
                message = "Inventory unavailable.";
                return false;
            }

            var owned = inv.GetOwnedCard(cardId);
            var def = TeksDatabase.GetCard(cardId);
            if (owned == null || def == null)
            {
                message = "Card not owned.";
                return false;
            }

            if (!IsCardAllowedForMatch(def))
            {
                message = $"Restricted: {CardRestrictionLabel} cards only!";
                return false;
            }

            _selectedPamatoIdOverride = cardId;
            inv.EquipPamato(cardId);

            if (State == MatchState.ReadyToFlick)
            {
                var player = PlayerController.Instance;
                if (player != null && player.Actor != null)
                {
                    var tex = CardArtGenerator.GetCardPortraitTexture(def, owned.StarRank);
                    player.Actor.SetHandCardVisible(true, tex);
                }
            }

            message = $"Selected {def.Name} as Pamato!";
            if (State == MatchState.WagerScreen)
            {
                OnMatchStateChanged?.Invoke();
            }
            return true;
        }

        public OwnedTeksCard GetActivePlayerPamato(out TeksCardDefinition definition)
        {
            definition = null;
            var inv = PlayerInventory.Instance;
            if (inv == null) return null;

            if (!string.IsNullOrEmpty(_selectedPamatoIdOverride))
            {
                var overridden = inv.GetOwnedCard(_selectedPamatoIdOverride);
                if (overridden != null && IsCardAllowedForMatch(overridden.CardId))
                {
                    definition = TeksDatabase.GetCard(overridden.CardId);
                    return overridden;
                }
            }

            var equipped = inv.GetEquippedPamato(out var eqDef);
            if (equipped != null && IsCardAllowedForMatch(eqDef))
            {
                definition = eqDef;
                return equipped;
            }

            var allowed = GetAllowedPamatoCards();
            if (allowed.Count > 0)
            {
                var chosen = allowed[0];
                definition = TeksDatabase.GetCard(chosen.CardId);
                return chosen;
            }

            definition = eqDef;
            return equipped;
        }

        private void EnsureValidSelectedPamato()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null) return;

            var active = GetActivePlayerPamato(out _);
            if (active != null && IsCardAllowedForMatch(active.CardId))
            {
                _selectedPamatoIdOverride = active.CardId;
                if (inv.EquippedPamatoId != active.CardId)
                {
                    inv.EquipPamato(active.CardId);
                }
            }
        }

        // --- Challenge Lifecycle ---

        public void StartChallenge(NpcOpponent npc)
        {
            StartChallengeImmediate(npc, openWagerScreen: true);
        }

        public void StartChallengeImmediate(NpcOpponent npc, bool openWagerScreen = true)
        {
            if (npc == null || State != MatchState.Idle) return;

            ActiveOpponent = npc;
            CurrentCoinWager = GetBaseCoinReward(npc);
            LastCoinReward = 0;
            _lastNewlyUnlockedAreas.Clear();
            LastFlickRating = string.Empty;
            RoundResultTitle = string.Empty;
            RoundResultSubtitle = string.Empty;
            WasTablaLastRound = false;
            PlayerWonLastRound = false;
            _hasSavedCharacterTransforms = false;

            EnsureValidSelectedPamato();

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.MovementLocked = true;
            }

            if (openWagerScreen)
            {
                State = MatchState.WagerScreen;
                OnMatchStateChanged?.Invoke();
            }
            else
            {
                BeginFlickMatch(CurrentCoinWager);
            }
        }

        public void CancelChallenge()
        {
            CleanupMatchCards();
            RestoreCharactersAfterMatch();

            State = MatchState.Idle;
            ActiveOpponent = null;
            LastFlickRating = string.Empty;
            WasTablaLastRound = false;
            _flightCamInitialized = false;

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.MovementLocked = false;
            }

            if (Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<CameraFollowController>();
                if (camFollow != null)
                {
                    camFollow.ClearMatchCamera();
                }
            }

            OnMatchStateChanged?.Invoke();
        }

        public void BeginFlickMatch(int coinWager)
        {
            if (ActiveOpponent == null) return;

            // Difficulty-scaled reward pot (zero coin loss on defeat)
            CurrentCoinWager = GetBaseCoinReward(ActiveOpponent);
            EnsureValidSelectedPamato();

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
                    player.Actor.ClearReactionPose();
                    player.Actor.SetPose(CharacterPoseMode.CrouchingFlick);
                    var owned = GetActivePlayerPamato(out var def);
                    var tex = def != null ? CardArtGenerator.GetCardPortraitTexture(def, owned != null ? owned.StarRank : 1) : null;
                    player.Actor.SetHandCardVisible(true, tex);
                }
            }

            if (ActiveOpponent != null)
            {
                _savedOpponentPos = ActiveOpponent.transform.position;
                _savedOpponentRot = ActiveOpponent.transform.rotation;

                ActiveOpponent.InMatch = true;
                ActiveOpponent.transform.position = oppAnchor;
                ActiveOpponent.transform.rotation = Quaternion.LookRotation(-_matchForward, Vector3.up);

                var oppActor = ActiveOpponent.GetComponent<CharacterActor>();
                if (oppActor != null)
                {
                    oppActor.ClearReactionPose();
                    oppActor.SetPose(CharacterPoseMode.CrouchingFlick);
                    oppActor.SetHandCardVisible(false);
                }
            }

            _hasSavedCharacterTransforms = true;
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

        private void UpdateFlightFollowCamera(bool snapImmediately = false)
        {
            if (Camera.main == null) return;
            var camFollow = Camera.main.GetComponent<CameraFollowController>();
            if (camFollow == null) return;

            Vector3 fallbackPos = _matchOrigin + _matchForward * 1.15f + Vector3.up * 0.25f;
            Vector3 pPos = _playerCard != null ? _playerCard.transform.position : fallbackPos;
            Vector3 oPos = _opponentCard != null ? _opponentCard.transform.position : pPos;

            Vector3 mid = (pPos + oPos) * 0.5f;
            float maxY = Mathf.Max(pPos.y, oPos.y);
            float spread = Vector3.Distance(new Vector3(pPos.x, 0f, pPos.z), new Vector3(oPos.x, 0f, oPos.z));

            Vector3 desiredFocus = new Vector3(mid.x, Mathf.Max(0.08f, mid.y * 0.85f), mid.z);
            if (!_flightCamInitialized || snapImmediately)
            {
                _flightCamFocus = desiredFocus;
                _flightCamInitialized = true;
            }
            else
            {
                _flightCamFocus = Vector3.Lerp(_flightCamFocus, desiredFocus, Mathf.Clamp01(Time.deltaTime * 9f));
            }

            float backDist = 1.55f + Mathf.Clamp(spread * 0.45f, 0f, 1.10f) + Mathf.Clamp(maxY * 0.25f, 0f, 0.55f);
            float sideOffset = 0.72f + Mathf.Clamp(spread * 0.15f, 0f, 0.35f);
            float camHeight = Mathf.Clamp(1.18f + maxY * 0.58f + spread * 0.14f, 1.10f, 2.85f);

            Vector3 camPos = _flightCamFocus - _matchForward * backDist - _matchRight * sideOffset + Vector3.up * camHeight;
            if (camPos.y < 0.75f) camPos.y = 0.75f;
            Vector3 lookTarget = _flightCamFocus + Vector3.up * 0.06f;

            camFollow.SetMatchCamera(camPos, lookTarget);
        }

        private void SetRoundResultCamera()
        {
            if (Camera.main == null) return;
            var camFollow = Camera.main.GetComponent<CameraFollowController>();
            if (camFollow == null) return;

            Vector3 mid = _matchOrigin + _matchForward * 1.15f;
            float spread = 0.60f;
            if (_playerCard != null && _opponentCard != null)
            {
                Vector3 p = _playerCard.transform.position;
                Vector3 o = _opponentCard.transform.position;
                mid = (p + o) * 0.5f;
                spread = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(o.x, 0f, o.z));
            }
            mid.y = 0.05f;

            float backDist = 1.45f + Mathf.Clamp(spread * 0.50f, 0f, 0.90f);
            float height = 1.32f + Mathf.Clamp(spread * 0.18f, 0f, 0.45f);
            Vector3 camPos = mid - _matchForward * backDist - _matchRight * 0.42f + Vector3.up * height;
            Vector3 lookTarget = mid + _matchForward * 0.12f + Vector3.up * 0.10f;
            camFollow.SetMatchCamera(camPos, lookTarget);
        }

        private void RestoreCharactersAfterMatch()
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                if (player.Actor != null)
                {
                    player.Actor.ClearReactionPose();
                    player.Actor.SetHandCardVisible(false);
                    player.Actor.SetPose(CharacterPoseMode.StandingOrWalking);
                }
                if (_hasSavedCharacterTransforms)
                {
                    player.TeleportForMatch(_savedPlayerPos, _savedPlayerRot);
                }
            }

            if (ActiveOpponent != null)
            {
                ActiveOpponent.InMatch = false;
                if (_hasSavedCharacterTransforms)
                {
                    ActiveOpponent.transform.position = _savedOpponentPos;
                    ActiveOpponent.transform.rotation = _savedOpponentRot;
                }
                var oppActor = ActiveOpponent.GetComponent<CharacterActor>();
                if (oppActor != null)
                {
                    oppActor.SetPose(ActiveOpponent.GetRestingPose());
                }
            }

            _hasSavedCharacterTransforms = false;
            _savedPlayerPos = Vector3.zero;
            _savedOpponentPos = Vector3.zero;
        }

        private void ArmFlickMeter()
        {
            CleanupMatchCards();
            WasTablaLastRound = false;
            LastFlickRating = string.Empty;
            _flightCamInitialized = false;

            var player = PlayerController.Instance;
            var owned = GetActivePlayerPamato(out var def);
            float streakBonus = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetStreakSweetSpotBonus() : 0f;
            SweetSpotHalfWidth = 0.15f + (owned != null ? owned.GetFlickSweetSpotBonus() : 0f) + streakBonus;

            float diffFactor = ActiveOpponent != null ? ActiveOpponent.Difficulty switch
            {
                AIDifficulty.Easy => 2.25f,
                AIDifficulty.Medium => 2.75f,
                AIDifficulty.Hard => 3.25f,
                AIDifficulty.Boss => 3.70f,
                AIDifficulty.Extreme => 3.85f,
                _ => 2.65f
            } : 2.65f;
            _needleSpeed = diffFactor;
            _needlePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            if (player != null && player.Actor != null)
            {
                player.Actor.ClearReactionPose();
                player.Actor.SetPose(CharacterPoseMode.CrouchingFlick);
                var tex = def != null ? CardArtGenerator.GetCardPortraitTexture(def, owned != null ? owned.StarRank : 1) : null;
                player.Actor.SetHandCardVisible(true, tex);
            }

            if (ActiveOpponent != null)
            {
                var oppActor = ActiveOpponent.GetComponent<CharacterActor>();
                if (oppActor != null)
                {
                    oppActor.ClearReactionPose();
                    oppActor.SetPose(CharacterPoseMode.CrouchingFlick);
                    oppActor.SetHandCardVisible(false);
                }
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
            float spinBoost;

            // Flick meter controls launch power, spin feel & SFX intensity only — NEVER win probability
            if (absVal <= SweetSpotHalfWidth)
            {
                powerFactor = 1.00f;
                spinBoost = 1.22f;
                LastFlickRating = "SWEET SPOT! MAX SPIN & POWER!";
            }
            else if (absVal <= GoodZoneHalfWidth)
            {
                powerFactor = 0.82f;
                spinBoost = 1.00f;
                LastFlickRating = "GOOD FLICK!";
            }
            else
            {
                powerFactor = 0.62f;
                spinBoost = 0.82f;
                LastFlickRating = "LIGHT FLICK!";
            }

            var player = PlayerController.Instance;
            Vector3 handPos = _matchOrigin + _matchRight * 0.22f + Vector3.up * 0.56f + _matchForward * 0.36f;
            Vector3 forwardDir = _matchForward;

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

            var ownedPamato = GetActivePlayerPamato(out var playerDef);
            string playerCardId = ownedPamato != null ? ownedPamato.CardId : "hero";
            int playerRank = ownedPamato != null ? ownedPamato.StarRank : 1;
            CardRarity playerRarity = playerDef != null ? playerDef.Rarity : CardRarity.Common;

            string oppCardId = ActiveOpponent != null ? ActiveOpponent.FavoriteCardId : "bakunawa";
            var oppDef = TeksDatabase.GetCard(oppCardId);
            int oppRank = ActiveOpponent != null ? ActiveOpponent.CardStarRank : 3;
            CardRarity oppRarity = oppDef != null ? oppDef.Rarity : CardRarity.Common;

            // Strict SPEC battle odds: Player is ALWAYS 50% Front / 50% Back; Opponent uses difficulty table (40/50/65/80%)
            float playerChubProb = PlayerChubProbability;
            float oppChubProb = ActiveOpponent != null ? ActiveOpponent.GetAiChubProbability() : 0.50f;

            CleanupMatchCards();
            FlickAirElapsed = 0f;

            if (TeksAudioManager.Instance != null)
            {
                TeksAudioManager.Instance.PlayCardFlick(powerFactor);
            }

            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

            // 1. Player's Pamato Card (Left trajectory — strictly 2 cards in play)
            _playerCard = TeksCardPhysics.SpawnPhysicalCard(
                playerCardId, playerRank,
                handPos - rightDir * 0.08f, Quaternion.Euler(15f, player != null ? player.transform.eulerAngles.y : 0f, 0f),
                isPlayerPamato: true, isOpponentPamato: false);
            _playerCard.RoleLabel = "YOUR PAMATO";
            _spawnedCards.Add(_playerCard);
            _playerCard.LaunchFlick(
                forwardDir * (1.80f * powerFactor) + Vector3.up * (3.25f * powerFactor) - rightDir * 0.38f,
                new Vector3(26f * powerFactor * spinBoost, 7f, 14f * spinBoost),
                playerChubProb,
                playerRarity);

            // 2. Opponent's Pamato Card (Right trajectory)
            _opponentCard = TeksCardPhysics.SpawnPhysicalCard(
                oppCardId, oppRank,
                handPos + rightDir * 0.08f, Quaternion.Euler(-12f, 25f, 0f),
                isPlayerPamato: false, isOpponentPamato: true);
            _opponentCard.RoleLabel = "OPPONENT";
            _spawnedCards.Add(_opponentCard);
            _opponentCard.LaunchFlick(
                forwardDir * (1.90f * powerFactor) + Vector3.up * (3.18f * powerFactor) + rightDir * 0.42f,
                new Vector3(-24f * powerFactor * spinBoost, -9f, 16f * spinBoost),
                oppChubProb,
                oppRarity);

            _flightCamFocus = handPos + forwardDir * 0.35f;
            _flightCamInitialized = true;
            UpdateFlightFollowCamera(snapImmediately: true);

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
                UpdateFlightFollowCamera();

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
            _lastNewlyUnlockedAreas.Clear();
            LastCoinReward = 0;

            if (PlayerCardFaceUp && !OpponentCardFaceUp)
            {
                // Player = Front (Chub), Opponent = Back (Cha) -> PLAYER WINS!
                PlayerWonLastRound = true;
                WasTablaLastRound = false;

                int baseReward = GetBaseCoinReward(ActiveOpponent);
                int payout = baseReward;
                string streakNote = string.Empty;

                if (inv != null)
                {
                    // Disjoint reporters: RecordMatchResult handles win count/streak; ReportMatchWin handles NPC defeat, area wins, story missions & area unlocks
                    inv.RecordMatchResult(true);
                    float mult = inv.GetStreakCoinMultiplier();
                    payout = Mathf.RoundToInt(baseReward * mult);
                    if (inv.WinStreak >= 2)
                    {
                        streakNote = $" [HOT HAND x{inv.WinStreak}!]";
                    }
                    inv.AddCoins(payout);

                    if (ActiveOpponent != null)
                    {
                        string npcId = ActiveOpponent.GetEffectiveNpcId();
                        var unlocked = inv.ReportMatchWin(
                            npcId,
                            ActiveOpponent.Difficulty,
                            ActiveOpponent.AreaId,
                            ActiveOpponent.IsChampion);
                        if (unlocked != null && unlocked.Count > 0)
                        {
                            _lastNewlyUnlockedAreas.AddRange(unlocked);
                        }
                    }
                }

                LastCoinReward = payout;
                string unlockNote = _lastNewlyUnlockedAreas.Count > 0 ? "  |  NEW AREA UNLOCKED!" : string.Empty;

                RoundResultTitle = "YOU WIN! (CHUB vs CHA)";
                RoundResultSubtitle = $"Won +{payout} Coins{streakNote}! Both players keep their Teks cards.{unlockNote}";

                if (TeksAudioManager.Instance != null)
                {
                    TeksAudioManager.Instance.PlayWinFanfare();
                    TeksAudioManager.Instance.PlayCoinClink();
                }
            }
            else if (!PlayerCardFaceUp && OpponentCardFaceUp)
            {
                // Player = Back (Cha), Opponent = Front (Chub) -> OPPONENT WINS!
                // Zero coin deduction and zero card stealing on defeat
                PlayerWonLastRound = false;
                WasTablaLastRound = false;

                if (inv != null)
                {
                    inv.RecordMatchResult(false);
                }

                string oppName = ActiveOpponent != null ? ActiveOpponent.NpcName : "Opponent";
                RoundResultTitle = $"{oppName.ToUpper()} WINS! (CHA vs CHUB)";
                RoundResultSubtitle = $"{oppName} takes the round! No coins lost & you keep your Pamato card.";

                if (TeksAudioManager.Instance != null)
                {
                    TeksAudioManager.Instance.PlayLoseTone();
                }
            }
            else
            {
                // Tabla (both Front or both Back) -> automatic retry!
                PlayerWonLastRound = false;
                WasTablaLastRound = true;
                _tablaRearmTimer = 2.60f;

                RoundResultTitle = PlayerCardFaceUp ? "TABLA! (BOTH CHUB)" : "TABLA! (BOTH CHA)";
                RoundResultSubtitle = "Tie! Both shooter cards landed the same — re-flicking!";

                if (TeksAudioManager.Instance != null)
                {
                    TeksAudioManager.Instance.PlayTablaRetry();
                }
            }

            // Transition both characters out of CrouchingFlick to StandingOrWalking BEFORE triggering reaction poses
            var player = PlayerController.Instance;
            if (player != null && player.Actor != null)
            {
                player.Actor.SetPose(CharacterPoseMode.StandingOrWalking);
                int playerReaction = WasTablaLastRound
                    ? CharacterActor.ReactionTabla
                    : (PlayerWonLastRound ? CharacterActor.ReactionWin : CharacterActor.ReactionLose);
                player.Actor.PlayReactionPose(playerReaction, WasTablaLastRound ? 2.0f : 2.4f);
            }

            if (ActiveOpponent != null)
            {
                var oppActor = ActiveOpponent.GetComponent<CharacterActor>();
                if (oppActor != null)
                {
                    oppActor.SetPose(CharacterPoseMode.StandingOrWalking);
                }
                bool npcWon = !PlayerWonLastRound && !WasTablaLastRound;
                ActiveOpponent.PlayBattleReaction(npcWon, WasTablaLastRound);
            }

            SetRoundResultCamera();

            State = MatchState.RoundComplete;
            // Fire OnMatchResolved strictly BEFORE OnMatchStateChanged so GameModeManager updates mode state before UI refreshes
            OnMatchResolved?.Invoke(ActiveOpponent, PlayerWonLastRound, WasTablaLastRound);
            OnMatchStateChanged?.Invoke();
        }

        public void Rematch()
        {
            if (ActiveOpponent == null) return;
            CurrentCoinWager = GetBaseCoinReward(ActiveOpponent);
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
