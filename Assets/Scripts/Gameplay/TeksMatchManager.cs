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
        public string ActiveOpponentCardId { get; private set; } = string.Empty;
        public int CurrentCoinWager { get; private set; } = 25;
        public int LastCoinReward { get; private set; }
        public int LastCoinLoss { get; private set; }
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

        public static float CalculatePlayerChubProbability(CardRarity rarity, int starRank)
        {
            float rarityBonus = rarity switch
            {
                CardRarity.Common => 0.00f,
                CardRarity.Uncommon => 0.05f,
                CardRarity.Rare => 0.10f,
                CardRarity.Epic => 0.15f,
                CardRarity.Legendary => 0.20f,
                _ => 0.00f
            };
            float upgradeBonus = (Mathf.Clamp(starRank, 1, 5) - 1) * 0.04f;
            return Mathf.Clamp(PlayerChubProbability + rarityBonus + upgradeBonus, 0.50f, 0.88f);
        }

        public string GetResolvedOpponentCardId(NpcOpponent opp, string excludedPlayerCardId)
        {
            string favoriteId = opp != null ? opp.FavoriteCardId : null;
            TeksCardDefinition favoriteDef = !string.IsNullOrEmpty(favoriteId) ? TeksDatabase.GetCard(favoriteId) : null;

            if (favoriteDef != null &&
                !string.Equals(favoriteId, excludedPlayerCardId, StringComparison.OrdinalIgnoreCase))
            {
                return favoriteId;
            }

            CardRarity preferredRarity = favoriteDef != null
                ? favoriteDef.Rarity
                : (opp != null ? opp.Difficulty switch
                {
                    AIDifficulty.Easy => CardRarity.Common,
                    AIDifficulty.Medium => CardRarity.Uncommon,
                    AIDifficulty.Hard => CardRarity.Rare,
                    AIDifficulty.Boss => CardRarity.Epic,
                    AIDifficulty.Extreme => CardRarity.Legendary,
                    _ => CardRarity.Common
                } : CardRarity.Common);

            var allCards = TeksDatabase.AllCards;
            var sameRarity = new List<TeksCardDefinition>();
            var fallbackAny = new List<TeksCardDefinition>();

            for (int i = 0; i < allCards.Count; i++)
            {
                var card = allCards[i];
                if (card == null) continue;
                if (string.Equals(card.Id, excludedPlayerCardId, StringComparison.OrdinalIgnoreCase)) continue;

                fallbackAny.Add(card);
                if (card.Rarity == preferredRarity)
                {
                    sameRarity.Add(card);
                }
            }

            string seedKey = opp != null ? opp.GetEffectiveNpcId() : "opponent";
            int hash = ComputeDeterministicHash(seedKey + "|" + (excludedPlayerCardId ?? string.Empty));

            if (sameRarity.Count > 0)
            {
                return sameRarity[hash % sameRarity.Count].Id;
            }

            if (fallbackAny.Count > 0)
            {
                return fallbackAny[hash % fallbackAny.Count].Id;
            }

            return !string.Equals("bakunawa", excludedPlayerCardId, StringComparison.OrdinalIgnoreCase)
                ? "bakunawa"
                : "hero";
        }

        private static int ComputeDeterministicHash(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                if (!string.IsNullOrEmpty(text))
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        hash ^= text[i];
                        hash *= 16777619u;
                    }
                }
                return (int)(hash & 0x7FFFFFFFu);
            }
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
            if (ActiveOpponent != null)
            {
                ActiveOpponentCardId = GetResolvedOpponentCardId(ActiveOpponent, cardId);
            }

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

            var inv = PlayerInventory.Instance;
            int defaultWager = GetBaseCoinReward(npc);

            ActiveOpponent = npc;
            CurrentCoinWager = inv != null ? Mathf.Clamp(defaultWager, 0, inv.Coins) : Mathf.Max(0, defaultWager);
            LastCoinReward = 0;
            LastCoinLoss = 0;
            _lastNewlyUnlockedAreas.Clear();
            LastFlickRating = string.Empty;
            RoundResultTitle = string.Empty;
            RoundResultSubtitle = string.Empty;
            WasTablaLastRound = false;
            PlayerWonLastRound = false;
            _hasSavedCharacterTransforms = false;

            EnsureValidSelectedPamato();
            var activePamato = GetActivePlayerPamato(out _);
            ActiveOpponentCardId = GetResolvedOpponentCardId(npc, activePamato != null ? activePamato.CardId : "hero");

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
            ActiveOpponentCardId = string.Empty;
            LastCoinLoss = 0;
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

            var inv = PlayerInventory.Instance;
            CurrentCoinWager = inv != null ? Mathf.Clamp(coinWager, 0, inv.Coins) : Mathf.Max(0, coinWager);
            LastCoinReward = 0;
            LastCoinLoss = 0;
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

            Vector3 fallbackPos = _matchOrigin + _matchForward * 0.85f + Vector3.up * 0.35f;
            Vector3 pPos = _playerCard != null ? _playerCard.transform.position : fallbackPos;
            Vector3 oPos = _opponentCard != null ? _opponentCard.transform.position : pPos;

            Vector3 mid = (pPos + oPos) * 0.5f;
            float maxY = Mathf.Max(pPos.y, oPos.y);
            float spread = Vector3.Distance(new Vector3(pPos.x, 0f, pPos.z), new Vector3(oPos.x, 0f, oPos.z));

            Vector3 desiredFocus = new Vector3(mid.x, Mathf.Max(0.10f, mid.y), mid.z);
            if (!_flightCamInitialized || snapImmediately)
            {
                _flightCamFocus = desiredFocus;
                _flightCamInitialized = true;
            }
            else
            {
                _flightCamFocus = Vector3.Lerp(_flightCamFocus, desiredFocus, Mathf.Clamp01(Time.deltaTime * 11f));
            }

            // Centered directly behind the midpoint of both cards so they stay dead-center in POV from toss to touchdown
            float backDist = Mathf.Clamp(1.05f + spread * 0.32f + maxY * 0.14f, 1.05f, 1.35f);
            float sideOffset = 0f;
            float camHeight = Mathf.Clamp(0.95f + mid.y * 0.65f + spread * 0.12f, 0.92f, 2.35f);

            Vector3 camPos = _flightCamFocus - _matchForward * backDist - _matchRight * sideOffset + Vector3.up * camHeight;
            if (camPos.y < 0.72f) camPos.y = 0.72f;
            Vector3 lookTarget = _flightCamFocus;

            camFollow.SetMatchCamera(camPos, lookTarget);
        }

        private void SetRoundResultCamera()
        {
            if (Camera.main == null) return;
            var camFollow = Camera.main.GetComponent<CameraFollowController>();
            if (camFollow == null) return;

            Vector3 mid = _matchOrigin + _matchForward * 0.95f;
            float spread = 0.45f;
            if (_playerCard != null && _opponentCard != null)
            {
                Vector3 p = _playerCard.transform.position;
                Vector3 o = _opponentCard.transform.position;
                mid = (p + o) * 0.5f;
                spread = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(o.x, 0f, o.z));
            }
            mid.y = 0.05f;

            float backDist = Mathf.Clamp(1.15f + spread * 0.40f, 1.15f, 1.65f);
            float height = Mathf.Clamp(1.12f + spread * 0.16f, 1.10f, 1.65f);
            Vector3 camPos = mid - _matchForward * backDist + Vector3.up * height;
            Vector3 lookTarget = mid + Vector3.up * 0.06f;
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
            LastCoinReward = 0;
            LastCoinLoss = 0;
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
            Vector3 handPos = _matchOrigin + _matchRight * 0.10f + Vector3.up * 0.62f + _matchForward * 0.42f;
            Vector3 forwardDir = _matchForward;

            if (player != null)
            {
                handPos = player.transform.position + player.transform.right * 0.10f + Vector3.up * 0.62f + player.transform.forward * 0.42f;
                forwardDir = player.transform.forward;
                if (player.Actor != null)
                {
                    player.Actor.SetHandCardVisible(false);
                    player.Actor.TriggerFlickAnimation();
                }
            }

            var inv = PlayerInventory.Instance;
            var ownedPamato = GetActivePlayerPamato(out var playerDef);
            string playerCardId = ownedPamato != null ? ownedPamato.CardId : "hero";
            int playerRank = ownedPamato != null ? ownedPamato.StarRank : 1;
            CardRarity playerRarity = playerDef != null ? playerDef.Rarity : CardRarity.Common;

            string oppCardId = GetResolvedOpponentCardId(ActiveOpponent, playerCardId);
            ActiveOpponentCardId = oppCardId;
            var oppDef = TeksDatabase.GetCard(oppCardId);
            int oppRank = ActiveOpponent != null ? ActiveOpponent.CardStarRank : 3;
            CardRarity oppRarity = oppDef != null ? oppDef.Rarity : CardRarity.Common;

            // Player Chub probability scales with equipped Pamato rarity & upgrade level; Opponent uses difficulty table
            float playerChubProb = CalculatePlayerChubProbability(playerRarity, playerRank);
            float oppChubProb = ActiveOpponent != null ? ActiveOpponent.GetAiChubProbability() : 0.50f;

            CleanupMatchCards();
            FlickAirElapsed = 0f;

            if (TeksAudioManager.Instance != null)
            {
                TeksAudioManager.Instance.PlayCardFlick(powerFactor);
            }

            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
            string playerLabel = (inv != null && !string.IsNullOrWhiteSpace(inv.PlayerNickname)
                ? inv.PlayerNickname.Trim()
                : "Player").ToUpper();
            string opponentLabel = (ActiveOpponent != null && !string.IsNullOrWhiteSpace(ActiveOpponent.NpcName)
                ? ActiveOpponent.NpcName.Trim()
                : "Opponent").ToUpper();

            // 1. Player's Pamato Card (Left upward trajectory — strictly 2 cards in play)
            _playerCard = TeksCardPhysics.SpawnPhysicalCard(
                playerCardId, playerRank,
                handPos - rightDir * 0.08f, Quaternion.Euler(15f, player != null ? player.transform.eulerAngles.y : 0f, 0f),
                isPlayerPamato: true, isOpponentPamato: false);
            _playerCard.RoleLabel = playerLabel;
            _spawnedCards.Add(_playerCard);
            _playerCard.LaunchFlick(
                forwardDir * (0.58f + 0.10f * powerFactor) + Vector3.up * (4.15f * powerFactor) - rightDir * 0.18f,
                new Vector3(26f * powerFactor * spinBoost, 6f, 14f * spinBoost),
                playerChubProb,
                playerRarity);

            // 2. Opponent's Pamato Card (Right upward trajectory)
            _opponentCard = TeksCardPhysics.SpawnPhysicalCard(
                oppCardId, oppRank,
                handPos + rightDir * 0.08f, Quaternion.Euler(-12f, 25f, 0f),
                isPlayerPamato: false, isOpponentPamato: true);
            _opponentCard.RoleLabel = opponentLabel;
            _spawnedCards.Add(_opponentCard);
            _opponentCard.LaunchFlick(
                forwardDir * (0.62f + 0.10f * powerFactor) + Vector3.up * (3.95f * powerFactor) + rightDir * 0.19f,
                new Vector3(-24f * powerFactor * spinBoost, -7f, 15f * spinBoost),
                oppChubProb,
                oppRarity);

            _flightCamFocus = handPos + forwardDir * 0.25f;
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
            LastCoinLoss = 0;

            if (PlayerCardFaceUp && !OpponentCardFaceUp)
            {
                // Player = Front (Chub), Opponent = Back (Cha) -> PLAYER WINS!
                PlayerWonLastRound = true;
                WasTablaLastRound = false;

                int basePayout = CurrentCoinWager > 0 ? CurrentCoinWager : GetBaseCoinReward(ActiveOpponent);
                int totalWin = basePayout;
                string streakNote = string.Empty;

                if (inv != null)
                {
                    // Disjoint reporters: RecordMatchResult handles win count/streak; ReportMatchWin handles NPC defeat, area wins, story missions & area unlocks
                    inv.RecordMatchResult(true);
                    float mult = inv.GetStreakCoinMultiplier();
                    totalWin = Mathf.RoundToInt(basePayout * mult);
                    if (inv.WinStreak >= 2)
                    {
                        streakNote = $" [HOT HAND x{inv.WinStreak}!]";
                    }
                    inv.AddCoins(totalWin);

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

                LastCoinReward = totalWin;
                LastCoinLoss = 0;
                string unlockNote = _lastNewlyUnlockedAreas.Count > 0 ? "  |  NEW AREA UNLOCKED!" : string.Empty;

                RoundResultTitle = "YOU WIN! (CHUB vs CHA)";
                RoundResultSubtitle = $"Won +{totalWin} Coins{streakNote}! Both players keep their Teks cards.{unlockNote}";

                if (TeksAudioManager.Instance != null)
                {
                    TeksAudioManager.Instance.PlayWinFanfare();
                    TeksAudioManager.Instance.PlayCoinClink();
                }
            }
            else if (!PlayerCardFaceUp && OpponentCardFaceUp)
            {
                // Player = Back (Cha), Opponent = Front (Chub) -> OPPONENT WINS!
                PlayerWonLastRound = false;
                WasTablaLastRound = false;

                int loss = 0;
                if (inv != null)
                {
                    inv.RecordMatchResult(false);
                    loss = Mathf.Min(CurrentCoinWager, inv.Coins);
                    if (loss > 0 && !inv.TrySpendCoins(loss))
                    {
                        loss = 0;
                    }
                }

                LastCoinLoss = loss;
                LastCoinReward = 0;

                string oppName = ActiveOpponent != null ? ActiveOpponent.NpcName : "Opponent";
                RoundResultTitle = $"{oppName.ToUpper()} WINS! (CHA vs CHUB)";
                RoundResultSubtitle = loss > 0
                    ? $"{oppName} takes the round! Lost -{loss} Coins! You keep your Pamato card."
                    : $"{oppName} takes the round! No coins wagered. You keep your Pamato card.";

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
            var inv = PlayerInventory.Instance;
            CurrentCoinWager = inv != null ? Mathf.Clamp(CurrentCoinWager, 0, inv.Coins) : Mathf.Max(0, CurrentCoinWager);
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
