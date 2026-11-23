using System;
using System.Collections.Generic;
using UnityEngine;
using TeksMaster.Audio;
using TeksMaster.Characters;
using TeksMaster.Core;
using TeksMaster.World;

namespace TeksMaster.Gameplay
{
    public enum ModeRunState
    {
        Inactive,
        InProgress,
        AwaitingNextRound,
        Victory,
        Failed
    }

    public readonly struct ChallengeRuleInfo
    {
        public readonly ChallengeRuleType RuleType;
        public readonly string Title;
        public readonly string Description;
        public readonly string RuleSummary;
        public readonly int TargetWins;
        public readonly int CoinReward;
        public readonly int GemReward;
        public readonly bool RequiresCommonOnly;

        public ChallengeRuleInfo(
            ChallengeRuleType ruleType,
            string title,
            string description,
            string ruleSummary,
            int targetWins,
            int coinReward,
            int gemReward,
            bool requiresCommonOnly)
        {
            RuleType = ruleType;
            Title = title;
            Description = description;
            RuleSummary = ruleSummary;
            TargetWins = targetWins;
            CoinReward = coinReward;
            GemReward = gemReward;
            RequiresCommonOnly = requiresCommonOnly;
        }
    }

    public class TeksGameModeManager : MonoBehaviour
    {
        public static TeksGameModeManager Instance { get; private set; }

        public const int TournamentRoundCount = 3;
        public const int TournamentEntryFee = 150;
        public const int TournamentTrophyCoins = 600;
        public const int TournamentTrophyGems = 40;

        public const int WinStreak3TargetWins = 3;
        public const int WinStreak3Coins = 350;
        public const int WinStreak3Gems = 20;

        public const int CommonOnlyTargetWins = 2;
        public const int CommonOnlyCoins = 300;
        public const int CommonOnlyGems = 20;

        public const int ExtremeGauntletTargetWins = 2;
        public const int ExtremeGauntletCoins = 450;
        public const int ExtremeGauntletGems = 30;

        public GameModeType ActiveMode { get; private set; } = GameModeType.Story;
        public ModeRunState RunState { get; private set; } = ModeRunState.Inactive;
        public ChallengeRuleType ActiveChallengeRule { get; private set; } = ChallengeRuleType.WinStreak3;

        public IReadOnlyList<NpcOpponent> TournamentBracket => _tournamentBracket;
        public int TournamentCurrentRoundIndex => _tournamentRoundIndex;
        public int TournamentWinsCount => _tournamentWins;

        public IReadOnlyList<NpcOpponent> ChallengeRoster => _challengeRoster;
        public int ChallengeCurrentWins => _challengeWins;
        public int ChallengeTargetWins => _challengeTargetWins;

        public int LastModeBonusCoins { get; private set; }
        public int LastModeBonusGems { get; private set; }
        public string LastModeSummary { get; private set; } = string.Empty;
        public bool IsSpecialModeActive => ActiveMode != GameModeType.Story && RunState != ModeRunState.Inactive;

        public event Action OnModeStateChanged;
        public event Action<GameModeType, bool, string, int, int> OnModeCompleted;

        private readonly List<NpcOpponent> _tournamentBracket = new List<NpcOpponent>();
        private readonly List<NpcOpponent> _challengeRoster = new List<NpcOpponent>();

        private TeksMatchManager _subscribedMatchManager;
        private int _tournamentRoundIndex;
        private int _tournamentWins;
        private int _challengeRoundIndex;
        private int _challengeWins;
        private int _challengeTargetWins;

        private bool _modeRewardAwarded;
        private bool _isTransitioningRound;
        private bool _isCancelingMode;

        private bool _hasReturnTransform;
        private Vector3 _returnPlayerPos;
        private Quaternion _returnPlayerRot;

        private bool _hasSavedPamatoBeforeChallenge;
        private string _savedPamatoBeforeChallenge;

        private NpcOpponent _relocatedTournamentOpponent;
        private Vector3 _savedOpponentPos;
        private Quaternion _savedOpponentRot;
        private Vector3 _savedOpponentWanderCenter;

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

        private void Start()
        {
            EnsureMatchSubscription();
        }

        private void OnDestroy()
        {
            UnsubscribeMatchManager();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void EnsureMatchSubscription()
        {
            var mm = TeksMatchManager.Instance;
            if (mm == null || _subscribedMatchManager == mm) return;

            UnsubscribeMatchManager();
            _subscribedMatchManager = mm;
            _subscribedMatchManager.OnMatchResolved += HandleMatchResolved;
            _subscribedMatchManager.OnMatchStateChanged += HandleMatchStateChanged;
        }

        private void UnsubscribeMatchManager()
        {
            if (_subscribedMatchManager == null) return;
            _subscribedMatchManager.OnMatchResolved -= HandleMatchResolved;
            _subscribedMatchManager.OnMatchStateChanged -= HandleMatchStateChanged;
            _subscribedMatchManager = null;
        }

        // --- Challenge Metadata ---

        public static ChallengeRuleInfo GetChallengeInfo(ChallengeRuleType rule)
        {
            return rule switch
            {
                ChallengeRuleType.WinStreak3 => new ChallengeRuleInfo(
                    ChallengeRuleType.WinStreak3,
                    "3-Win Streak Trial",
                    "Win 3 consecutive Teks matches without a single loss.",
                    "3 Wins in a Row  |  Any Loss Ends Trial",
                    WinStreak3TargetWins,
                    WinStreak3Coins,
                    WinStreak3Gems,
                    requiresCommonOnly: false),
                ChallengeRuleType.CommonOnly => new ChallengeRuleInfo(
                    ChallengeRuleType.CommonOnly,
                    "Common-Only Duel",
                    "Prove pure Pamato grit: win 2 matches using ONLY Common cards.",
                    "Common Cards Only  |  2 Wins Required",
                    CommonOnlyTargetWins,
                    CommonOnlyCoins,
                    CommonOnlyGems,
                    requiresCommonOnly: true),
                ChallengeRuleType.ExtremeBossGauntlet => new ChallengeRuleInfo(
                    ChallengeRuleType.ExtremeBossGauntlet,
                    "Extreme Boss Gauntlet",
                    "Face 2 elite Hard/Boss/Extreme Barangay challengers back-to-back!",
                    "Elite Opponents Only  |  2 Consecutive Wins",
                    ExtremeGauntletTargetWins,
                    ExtremeGauntletCoins,
                    ExtremeGauntletGems,
                    requiresCommonOnly: false),
                _ => new ChallengeRuleInfo(
                    ChallengeRuleType.WinStreak3,
                    "3-Win Streak Trial",
                    "Win 3 consecutive Teks matches without a single loss.",
                    "3 Wins in a Row",
                    WinStreak3TargetWins,
                    WinStreak3Coins,
                    WinStreak3Gems,
                    requiresCommonOnly: false)
            };
        }

        public static ChallengeRuleInfo[] GetAllChallengeInfos()
        {
            return new ChallengeRuleInfo[]
            {
                GetChallengeInfo(ChallengeRuleType.WinStreak3),
                GetChallengeInfo(ChallengeRuleType.CommonOnly),
                GetChallengeInfo(ChallengeRuleType.ExtremeBossGauntlet)
            };
        }

        public bool CanUseCardInActiveMode(string cardId)
        {
            if (ActiveMode == GameModeType.Challenge
                && RunState != ModeRunState.Inactive
                && ActiveChallengeRule == ChallengeRuleType.CommonOnly)
            {
                var def = TeksDatabase.GetCard(cardId);
                return def != null && def.Rarity == CardRarity.Common;
            }
            return true;
        }

        // --- Quick Battle / Free Play ---

        public List<NpcOpponent> GetAllOpponents()
        {
            var list = new List<NpcOpponent>();
            var builder = BarangayCourtBuilder.Instance;
            if (builder != null && builder.SpawnedOpponents != null && builder.SpawnedOpponents.Count > 0)
            {
                for (int i = 0; i < builder.SpawnedOpponents.Count; i++)
                {
                    if (builder.SpawnedOpponents[i] != null)
                    {
                        list.Add(builder.SpawnedOpponents[i]);
                    }
                }
            }

            if (list.Count == 0)
            {
                var found = FindObjectsByType<NpcOpponent>();
                for (int i = 0; i < found.Length; i++)
                {
                    if (found[i] != null)
                    {
                        list.Add(found[i]);
                    }
                }
            }

            return list;
        }

        public bool IsOpponentUnlocked(NpcOpponent npc)
        {
            if (npc == null) return false;
            var inv = PlayerInventory.Instance;
            if (inv == null) return true;
            return inv.IsAreaUnlocked(npc.AreaId);
        }

        public List<NpcOpponent> GetUnlockedOpponents()
        {
            var all = GetAllOpponents();
            var unlocked = new List<NpcOpponent>();
            for (int i = 0; i < all.Count; i++)
            {
                if (IsOpponentUnlocked(all[i]))
                {
                    unlocked.Add(all[i]);
                }
            }

            unlocked.Sort((a, b) =>
            {
                int areaCmp = ((int)a.AreaId).CompareTo((int)b.AreaId);
                if (areaCmp != 0) return areaCmp;

                int storyCmp = b.IsStoryOpponent.CompareTo(a.IsStoryOpponent);
                if (storyCmp != 0) return storyCmp;

                int diffCmp = GetDifficultySortRank(a).CompareTo(GetDifficultySortRank(b));
                if (diffCmp != 0) return diffCmp;

                return string.Compare(a.NpcName, b.NpcName, StringComparison.OrdinalIgnoreCase);
            });

            return unlocked;
        }

        public bool StartQuickBattle(NpcOpponent npc)
        {
            return StartQuickBattle(npc, out _);
        }

        public bool StartQuickBattle(NpcOpponent npc, out string message)
        {
            EnsureMatchSubscription();

            if (npc == null)
            {
                message = "No opponent selected.";
                return false;
            }

            if (TeksMatchManager.Instance == null)
            {
                message = "Match manager unavailable.";
                return false;
            }

            if (!IsOpponentUnlocked(npc))
            {
                message = $"{npc.NpcName} is in a locked area.";
                return false;
            }

            PrepareForModeLaunch(keepExistingReturnTransform: false);

            ActiveMode = GameModeType.QuickBattle;
            RunState = ModeRunState.InProgress;
            _modeRewardAwarded = false;
            LastModeBonusCoins = 0;
            LastModeBonusGems = 0;
            LastModeSummary = string.Empty;

            TeksMatchManager.Instance.ClearCardRestriction();
            PositionPlayerNearOpponent(npc);
            TeksMatchManager.Instance.StartChallengeImmediate(npc, openWagerScreen: true);

            message = $"Quick Battle vs {npc.NpcName}!";
            OnModeStateChanged?.Invoke();
            return true;
        }

        // --- Tournament Mode (3-Round Bracket: Quarterfinals -> Semifinals -> Finals) ---

        public static string GetTournamentRoundName(int roundIndex)
        {
            switch (roundIndex)
            {
                case 0: return "Quarterfinals";
                case 1: return "Semifinals";
                case 2: return "Finals";
                default: return $"Round {roundIndex + 1}";
            }
        }

        public List<NpcOpponent> PreviewTournamentBracket()
        {
            var all = GetAllOpponents();
            var bracket = new List<NpcOpponent>(TournamentRoundCount);
            if (all.Count == 0) return bracket;

            // Round 1 (Quarterfinals): Medium/Easy (prefer story opponent "carlo" or "dodong")
            NpcOpponent r1 = PickOpponent(all, bracket,
                n => n.GetEffectiveNpcId() == "carlo" || n.GetEffectiveNpcId() == "dodong",
                n => n.Difficulty == AIDifficulty.Medium || n.Difficulty == AIDifficulty.Easy,
                preferLowestDifficulty: true);
            if (r1 != null) bracket.Add(r1);

            // Round 2 (Semifinals): Hard (prefer story opponent "joshua")
            NpcOpponent r2 = PickOpponent(all, bracket,
                n => n.GetEffectiveNpcId() == "joshua",
                n => n.Difficulty == AIDifficulty.Hard,
                preferLowestDifficulty: false);
            if (r2 != null) bracket.Add(r2);

            // Round 3 (Finals): Extreme / Champion / Boss (prefer "teks_master" or IsChampion)
            NpcOpponent r3 = PickOpponent(all, bracket,
                n => n.GetEffectiveNpcId() == "teks_master" || n.IsChampion,
                n => n.Difficulty == AIDifficulty.Extreme || n.Difficulty == AIDifficulty.Boss,
                preferLowestDifficulty: false);
            if (r3 != null) bracket.Add(r3);

            return bracket;
        }

        public bool StartTournament()
        {
            return StartTournament(out _);
        }

        public bool StartTournament(out string message)
        {
            EnsureMatchSubscription();

            if (TeksMatchManager.Instance == null)
            {
                message = "Match manager unavailable.";
                return false;
            }

            var inv = PlayerInventory.Instance;
            if (inv != null && inv.Coins < TournamentEntryFee)
            {
                message = $"Need {TournamentEntryFee} Barya Coins to enter the Tournament! (You have {inv.Coins:N0})";
                return false;
            }

            var bracket = PreviewTournamentBracket();
            if (bracket.Count == 0)
            {
                message = "No challengers found in the Barangay.";
                return false;
            }

            if (inv != null && !inv.TrySpendCoins(TournamentEntryFee))
            {
                message = $"Need {TournamentEntryFee} Barya Coins to enter the Tournament!";
                return false;
            }

            TeksAudioManager.Instance?.PlayCoinClink();

            bool keepReturn = _hasReturnTransform && ActiveMode == GameModeType.Tournament;
            PrepareForModeLaunch(keepExistingReturnTransform: keepReturn);

            _tournamentBracket.Clear();
            _tournamentBracket.AddRange(bracket);
            _tournamentRoundIndex = 0;
            _tournamentWins = 0;

            ActiveMode = GameModeType.Tournament;
            RunState = ModeRunState.InProgress;
            _modeRewardAwarded = false;
            LastModeBonusCoins = 0;
            LastModeBonusGems = 0;
            LastModeSummary = $"Round 1/{_tournamentBracket.Count} ({GetTournamentRoundName(0)}): {_tournamentBracket[0].NpcName}";

            TeksMatchManager.Instance.ClearCardRestriction();
            LaunchCurrentTournamentRound(openWagerScreen: true);

            message = $"Tournament entered (-{TournamentEntryFee} Coins)! {GetTournamentRoundName(0)} vs {_tournamentBracket[0].NpcName}!";
            OnModeStateChanged?.Invoke();
            return true;
        }

        public bool StartNextTournamentRound()
        {
            EnsureMatchSubscription();

            if (ActiveMode != GameModeType.Tournament
                || RunState != ModeRunState.AwaitingNextRound
                || _tournamentRoundIndex < 0
                || _tournamentRoundIndex >= _tournamentBracket.Count
                || TeksMatchManager.Instance == null)
            {
                return false;
            }

            CancelCurrentMatchForRoundTransition();
            RunState = ModeRunState.InProgress;
            LaunchCurrentTournamentRound(openWagerScreen: false);
            OnModeStateChanged?.Invoke();
            return true;
        }

        private void LaunchCurrentTournamentRound(bool openWagerScreen)
        {
            if (_tournamentRoundIndex < 0 || _tournamentRoundIndex >= _tournamentBracket.Count) return;
            var opp = _tournamentBracket[_tournamentRoundIndex];
            if (opp == null || TeksMatchManager.Instance == null) return;

            PositionForTournamentRound(opp);
            TeksMatchManager.Instance.StartChallengeImmediate(opp, openWagerScreen);
        }

        // --- Challenge Mode (WinStreak3, CommonOnly, ExtremeBossGauntlet) ---

        public bool CanStartChallenge(ChallengeRuleType rule, out string reason)
        {
            if (TeksMatchManager.Instance == null)
            {
                reason = "Match manager unavailable.";
                return false;
            }

            var all = GetAllOpponents();
            if (all.Count == 0)
            {
                reason = "No Barangay opponents available.";
                return false;
            }

            if (rule == ChallengeRuleType.CommonOnly)
            {
                if (FindBestOwnedCommonCardId() == null)
                {
                    reason = "You need at least 1 Common Teks card for Common-Only Duel.";
                    return false;
                }
            }

            reason = "Ready";
            return true;
        }

        public bool StartChallengeMode(ChallengeRuleType rule)
        {
            return StartChallengeMode(rule, out _);
        }

        public bool StartChallengeMode(ChallengeRuleType rule, out string message)
        {
            EnsureMatchSubscription();

            if (!CanStartChallenge(rule, out string reason))
            {
                message = reason;
                return false;
            }

            var info = GetChallengeInfo(rule);
            var roster = BuildChallengeRoster(rule, info.TargetWins);
            if (roster.Count == 0)
            {
                message = "Could not assemble challengers for this trial.";
                return false;
            }

            bool keepReturn = _hasReturnTransform && ActiveMode == GameModeType.Challenge;
            PrepareForModeLaunch(keepExistingReturnTransform: keepReturn);

            _challengeRoster.Clear();
            _challengeRoster.AddRange(roster);
            _challengeRoundIndex = 0;
            _challengeWins = 0;
            _challengeTargetWins = Mathf.Max(1, info.TargetWins);

            ActiveMode = GameModeType.Challenge;
            ActiveChallengeRule = rule;
            RunState = ModeRunState.InProgress;
            _modeRewardAwarded = false;
            LastModeBonusCoins = 0;
            LastModeBonusGems = 0;
            LastModeSummary = $"{info.Title} (0/{_challengeTargetWins})";

            if (rule == ChallengeRuleType.CommonOnly)
            {
                ApplyCommonOnlyRestriction();
            }
            else
            {
                RestoreSavedPamatoIfNeeded();
                TeksMatchManager.Instance.ClearCardRestriction();
            }

            LaunchCurrentChallengeMatch(openWagerScreen: true);

            message = $"{info.Title} started! First bout vs {_challengeRoster[0].NpcName}!";
            OnModeStateChanged?.Invoke();
            return true;
        }

        public bool StartNextChallengeMatch()
        {
            EnsureMatchSubscription();

            if (ActiveMode != GameModeType.Challenge
                || RunState != ModeRunState.AwaitingNextRound
                || _challengeRoster.Count == 0
                || TeksMatchManager.Instance == null)
            {
                return false;
            }

            CancelCurrentMatchForRoundTransition();
            RunState = ModeRunState.InProgress;
            LaunchCurrentChallengeMatch(openWagerScreen: false);
            OnModeStateChanged?.Invoke();
            return true;
        }

        private List<NpcOpponent> BuildChallengeRoster(ChallengeRuleType rule, int targetWins)
        {
            var all = GetAllOpponents();
            var roster = new List<NpcOpponent>();
            if (all.Count == 0) return roster;

            if (rule == ChallengeRuleType.ExtremeBossGauntlet)
            {
                // Pick elite Hard / Boss / Extreme / Champion opponents
                NpcOpponent first = PickOpponent(all, roster,
                    n => n.Difficulty == AIDifficulty.Hard || n.Difficulty == AIDifficulty.Boss,
                    n => n.Difficulty == AIDifficulty.Extreme || n.IsChampion,
                    preferLowestDifficulty: false);
                if (first != null) roster.Add(first);

                NpcOpponent second = PickOpponent(all, roster,
                    n => n.IsChampion || n.Difficulty == AIDifficulty.Extreme || n.Difficulty == AIDifficulty.Boss,
                    n => n.Difficulty == AIDifficulty.Hard,
                    preferLowestDifficulty: false);
                if (second != null) roster.Add(second);
            }
            else if (rule == ChallengeRuleType.CommonOnly)
            {
                NpcOpponent first = PickOpponent(all, roster,
                    n => n.Difficulty == AIDifficulty.Easy,
                    n => n.Difficulty == AIDifficulty.Medium,
                    preferLowestDifficulty: true);
                if (first != null) roster.Add(first);

                NpcOpponent second = PickOpponent(all, roster,
                    n => n.Difficulty == AIDifficulty.Medium,
                    n => n.Difficulty == AIDifficulty.Easy,
                    preferLowestDifficulty: true);
                if (second != null) roster.Add(second);
            }
            else
            {
                // WinStreak3: escalating Easy -> Medium -> Hard
                NpcOpponent r1 = PickOpponent(all, roster,
                    n => n.Difficulty == AIDifficulty.Easy,
                    n => n.Difficulty == AIDifficulty.Medium,
                    preferLowestDifficulty: true);
                if (r1 != null) roster.Add(r1);

                NpcOpponent r2 = PickOpponent(all, roster,
                    n => n.Difficulty == AIDifficulty.Medium,
                    n => n.Difficulty == AIDifficulty.Easy,
                    preferLowestDifficulty: true);
                if (r2 != null) roster.Add(r2);

                NpcOpponent r3 = PickOpponent(all, roster,
                    n => n.Difficulty == AIDifficulty.Hard,
                    n => n.Difficulty == AIDifficulty.Medium,
                    preferLowestDifficulty: false);
                if (r3 != null) roster.Add(r3);
            }

            while (roster.Count < targetWins && roster.Count < all.Count)
            {
                NpcOpponent filler = PickOpponent(all, roster, _ => false, _ => true, preferLowestDifficulty: true);
                if (filler == null) break;
                roster.Add(filler);
            }

            return roster;
        }

        private void LaunchCurrentChallengeMatch(bool openWagerScreen)
        {
            if (_challengeRoster.Count == 0 || TeksMatchManager.Instance == null) return;
            var opp = _challengeRoster[_challengeRoundIndex % _challengeRoster.Count];
            if (opp == null) return;

            PositionPlayerNearOpponent(opp);
            TeksMatchManager.Instance.StartChallengeImmediate(opp, openWagerScreen);
        }

        private void ApplyCommonOnlyRestriction()
        {
            var inv = PlayerInventory.Instance;
            if (inv != null && !_hasSavedPamatoBeforeChallenge)
            {
                _savedPamatoBeforeChallenge = inv.EquippedPamatoId;
                _hasSavedPamatoBeforeChallenge = true;
            }

            if (TeksMatchManager.Instance != null)
            {
                TeksMatchManager.Instance.SetCardRestriction(CardRarity.Common, "Common Only");
                string bestCommon = FindBestOwnedCommonCardId();
                if (!string.IsNullOrEmpty(bestCommon))
                {
                    var current = TeksMatchManager.Instance.GetActivePlayerPamato(out var def);
                    if (current == null || def == null || def.Rarity != CardRarity.Common)
                    {
                        TeksMatchManager.Instance.TrySelectPamatoCard(bestCommon, out _);
                    }
                }
            }
        }

        private void RestoreSavedPamatoIfNeeded()
        {
            if (!_hasSavedPamatoBeforeChallenge) return;

            var inv = PlayerInventory.Instance;
            if (inv != null && !string.IsNullOrEmpty(_savedPamatoBeforeChallenge)
                && inv.GetOwnedCard(_savedPamatoBeforeChallenge) != null)
            {
                inv.EquipPamato(_savedPamatoBeforeChallenge);
            }

            _hasSavedPamatoBeforeChallenge = false;
            _savedPamatoBeforeChallenge = null;
        }

        private static string FindBestOwnedCommonCardId()
        {
            var inv = PlayerInventory.Instance;
            if (inv == null) return null;

            string bestId = null;
            int bestLevel = -1;
            var owned = inv.OwnedCards;
            for (int i = 0; i < owned.Count; i++)
            {
                var c = owned[i];
                var def = TeksDatabase.GetCard(c.CardId);
                if (def != null && def.Rarity == CardRarity.Common)
                {
                    if (c.Level > bestLevel)
                    {
                        bestLevel = c.Level;
                        bestId = c.CardId;
                    }
                }
            }
            return bestId;
        }

        // --- Mode Exit & Cleanup ---

        public void ExitActiveMode(bool cancelActiveMatch = true)
        {
            if (_isCancelingMode) return;
            _isCancelingMode = true;
            try
            {
                if (TeksMatchManager.Instance != null)
                {
                    TeksMatchManager.Instance.ClearCardRestriction();
                }

                RestoreSavedPamatoIfNeeded();

                if (cancelActiveMatch && TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle)
                {
                    TeksMatchManager.Instance.CancelChallenge();
                }

                RestoreRelocatedTournamentOpponent();
                RestoreReturnTransform();

                ActiveMode = GameModeType.Story;
                RunState = ModeRunState.Inactive;
                _tournamentBracket.Clear();
                _challengeRoster.Clear();
                _tournamentRoundIndex = 0;
                _tournamentWins = 0;
                _challengeRoundIndex = 0;
                _challengeWins = 0;
                _challengeTargetWins = 0;
                _modeRewardAwarded = false;
                LastModeBonusCoins = 0;
                LastModeBonusGems = 0;
                LastModeSummary = string.Empty;
            }
            finally
            {
                _isCancelingMode = false;
            }

            OnModeStateChanged?.Invoke();
        }

        private void PrepareForModeLaunch(bool keepExistingReturnTransform)
        {
            bool prevTransition = _isTransitioningRound;
            _isTransitioningRound = true;
            try
            {
                if (!keepExistingReturnTransform)
                {
                    RestoreSavedPamatoIfNeeded();
                    if (TeksMatchManager.Instance != null)
                    {
                        TeksMatchManager.Instance.ClearCardRestriction();
                    }
                }

                if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State != MatchState.Idle)
                {
                    TeksMatchManager.Instance.CancelChallenge();
                }

                RestoreRelocatedTournamentOpponent();

                if (!keepExistingReturnTransform)
                {
                    if (_hasReturnTransform)
                    {
                        RestoreReturnTransform();
                    }
                    CaptureReturnTransform();
                }
                else if (!_hasReturnTransform)
                {
                    CaptureReturnTransform();
                }
            }
            finally
            {
                _isTransitioningRound = prevTransition;
            }
        }

        private void CancelCurrentMatchForRoundTransition()
        {
            if (TeksMatchManager.Instance == null || TeksMatchManager.Instance.State == MatchState.Idle) return;

            _isTransitioningRound = true;
            try
            {
                TeksMatchManager.Instance.CancelChallenge();
            }
            finally
            {
                _isTransitioningRound = false;
            }
        }

        // --- Match Event Handlers ---

        private void HandleMatchResolved(NpcOpponent opponent, bool playerWon, bool wasTabla)
        {
            if (ActiveMode == GameModeType.Story || RunState == ModeRunState.Inactive) return;
            if (wasTabla) return; // Tabla auto-retries the current round

            if (ActiveMode == GameModeType.Tournament)
            {
                if (playerWon)
                {
                    _tournamentWins++;
                    _tournamentRoundIndex++;
                    if (_tournamentRoundIndex >= _tournamentBracket.Count)
                    {
                        RunState = ModeRunState.Victory;
                        if (!_modeRewardAwarded)
                        {
                            _modeRewardAwarded = true;
                            LastModeBonusCoins = TournamentTrophyCoins;
                            LastModeBonusGems = TournamentTrophyGems;
                            LastModeSummary = $"TOURNAMENT CHAMPION! Trophy Prize: +{TournamentTrophyCoins} Coins & +{TournamentTrophyGems} Gems!";

                            if (PlayerInventory.Instance != null)
                            {
                                PlayerInventory.Instance.AddCoins(TournamentTrophyCoins);
                                PlayerInventory.Instance.AddGems(TournamentTrophyGems);
                                PlayerInventory.Instance.ReportTournamentWon();
                            }

                            if (TeksAudioManager.Instance != null)
                            {
                                TeksAudioManager.Instance.PlayCoinClink();
                            }

                            OnModeCompleted?.Invoke(
                                GameModeType.Tournament,
                                true,
                                LastModeSummary,
                                LastModeBonusCoins,
                                LastModeBonusGems);
                        }
                    }
                    else
                    {
                        RunState = ModeRunState.AwaitingNextRound;
                        var nextOpp = _tournamentBracket[_tournamentRoundIndex];
                        string nextName = nextOpp != null ? nextOpp.NpcName : "Next Challenger";
                        string clearedName = GetTournamentRoundName(_tournamentWins - 1);
                        string nextRoundName = GetTournamentRoundName(_tournamentRoundIndex);
                        LastModeSummary = $"{clearedName} Cleared ({_tournamentWins}/{_tournamentBracket.Count})! Next: {nextRoundName} vs {nextName}.";
                    }
                }
                else
                {
                    RunState = ModeRunState.Failed;
                    string failedRoundName = GetTournamentRoundName(_tournamentRoundIndex);
                    LastModeSummary = $"Eliminated in {failedRoundName} (Round {_tournamentRoundIndex + 1}/{Mathf.Max(1, _tournamentBracket.Count)}). Pay {TournamentEntryFee} Coins to retry!";
                    OnModeCompleted?.Invoke(GameModeType.Tournament, false, LastModeSummary, 0, 0);
                }

                OnModeStateChanged?.Invoke();
            }
            else if (ActiveMode == GameModeType.Challenge)
            {
                var info = GetChallengeInfo(ActiveChallengeRule);
                if (playerWon)
                {
                    _challengeWins++;
                    _challengeRoundIndex++;
                    if (_challengeWins >= _challengeTargetWins)
                    {
                        RunState = ModeRunState.Victory;
                        if (!_modeRewardAwarded)
                        {
                            _modeRewardAwarded = true;
                            LastModeBonusCoins = info.CoinReward;
                            LastModeBonusGems = info.GemReward;
                            LastModeSummary = $"CHALLENGE COMPLETE! {info.Title}: +{info.CoinReward} Coins & +{info.GemReward} Gems!";

                            if (PlayerInventory.Instance != null)
                            {
                                PlayerInventory.Instance.AddCoins(info.CoinReward);
                                PlayerInventory.Instance.AddGems(info.GemReward);
                                PlayerInventory.Instance.ReportChallengeCompleted(ActiveChallengeRule);
                            }

                            if (TeksAudioManager.Instance != null)
                            {
                                TeksAudioManager.Instance.PlayCoinClink();
                            }

                            OnModeCompleted?.Invoke(
                                GameModeType.Challenge,
                                true,
                                LastModeSummary,
                                LastModeBonusCoins,
                                LastModeBonusGems);
                        }
                    }
                    else
                    {
                        RunState = ModeRunState.AwaitingNextRound;
                        LastModeSummary = $"{info.Title}: {_challengeWins}/{_challengeTargetWins} Wins! Ready for next bout!";
                    }
                }
                else
                {
                    RunState = ModeRunState.Failed;
                    LastModeSummary = $"{info.Title} Failed ({_challengeWins}/{_challengeTargetWins} Wins). Try again!";
                    OnModeCompleted?.Invoke(GameModeType.Challenge, false, LastModeSummary, 0, 0);
                }

                OnModeStateChanged?.Invoke();
            }
            else if (ActiveMode == GameModeType.QuickBattle)
            {
                LastModeSummary = playerWon
                    ? "Quick Battle Victory! Rematch or return to the Barangay."
                    : "Quick Battle complete! Rematch or return to the Barangay.";
                OnModeStateChanged?.Invoke();
            }
        }

        private void HandleMatchStateChanged()
        {
            if (_isTransitioningRound || _isCancelingMode) return;
            if (ActiveMode == GameModeType.Story) return;

            if (TeksMatchManager.Instance != null && TeksMatchManager.Instance.State == MatchState.Idle)
            {
                ExitActiveMode(cancelActiveMatch: false);
            }
        }

        // --- UI Hooks for HUD & RoundResultPanel ---

        public string GetModeStatusBanner()
        {
            if (ActiveMode == GameModeType.Story || RunState == ModeRunState.Inactive)
            {
                return string.Empty;
            }

            if (ActiveMode == GameModeType.QuickBattle)
            {
                return "QUICK BATTLE MODE";
            }

            if (ActiveMode == GameModeType.Tournament)
            {
                int total = Mathf.Max(1, _tournamentBracket.Count);
                if (RunState == ModeRunState.Victory)
                {
                    return $"TOURNAMENT CHAMPION! ({total}/{total})";
                }
                if (RunState == ModeRunState.Failed)
                {
                    string roundName = GetTournamentRoundName(_tournamentRoundIndex).ToUpperInvariant();
                    return $"TOURNAMENT — ELIMINATED ({roundName})";
                }
                int roundDisplay = Mathf.Clamp(_tournamentRoundIndex + 1, 1, total);
                string activeRoundName = GetTournamentRoundName(_tournamentRoundIndex).ToUpperInvariant();
                return $"TOURNAMENT — {activeRoundName} ({roundDisplay}/{total})";
            }

            if (ActiveMode == GameModeType.Challenge)
            {
                var info = GetChallengeInfo(ActiveChallengeRule);
                if (RunState == ModeRunState.Victory)
                {
                    return $"CHALLENGE COMPLETE — {info.Title.ToUpper()}";
                }
                if (RunState == ModeRunState.Failed)
                {
                    return $"CHALLENGE FAILED — {info.Title.ToUpper()}";
                }
                return $"CHALLENGE: {info.Title.ToUpper()} ({_challengeWins}/{_challengeTargetWins})";
            }

            return string.Empty;
        }

        public string GetModeResultBonusNote()
        {
            if (ActiveMode == GameModeType.Story || RunState == ModeRunState.Inactive)
            {
                return string.Empty;
            }
            return LastModeSummary ?? string.Empty;
        }

        public string GetResultPrimaryButtonLabel()
        {
            if (ActiveMode == GameModeType.Tournament)
            {
                return RunState switch
                {
                    ModeRunState.AwaitingNextRound => "NEXT ROUND",
                    ModeRunState.Victory => "CLAIM TROPHY",
                    ModeRunState.Failed => "RETRY TOURNAMENT",
                    _ => "CONTINUE"
                };
            }

            if (ActiveMode == GameModeType.Challenge)
            {
                return RunState switch
                {
                    ModeRunState.AwaitingNextRound => "NEXT BOUT",
                    ModeRunState.Victory => "CLAIM REWARD",
                    ModeRunState.Failed => "RETRY CHALLENGE",
                    _ => "CONTINUE"
                };
            }

            return "REMATCH";
        }

        public string GetResultSecondaryButtonLabel()
        {
            if (ActiveMode == GameModeType.Tournament)
            {
                return RunState == ModeRunState.Victory ? "EXIT TO BARANGAY" : "LEAVE TOURNAMENT";
            }
            if (ActiveMode == GameModeType.Challenge)
            {
                return RunState == ModeRunState.Victory ? "EXIT TO BARANGAY" : "LEAVE CHALLENGE";
            }
            if (ActiveMode == GameModeType.QuickBattle)
            {
                return "RETURN";
            }
            return "LEAVE";
        }

        public bool ShouldShowResultSecondaryButton()
        {
            if ((ActiveMode == GameModeType.Tournament || ActiveMode == GameModeType.Challenge)
                && RunState == ModeRunState.Victory)
            {
                return false;
            }
            return true;
        }

        public void HandleResultPrimaryAction()
        {
            if (ActiveMode == GameModeType.Tournament)
            {
                if (RunState == ModeRunState.AwaitingNextRound)
                {
                    StartNextTournamentRound();
                }
                else if (RunState == ModeRunState.Failed)
                {
                    if (!StartTournament(out _))
                    {
                        ExitActiveMode(cancelActiveMatch: true);
                    }
                }
                else
                {
                    ExitActiveMode(cancelActiveMatch: true);
                }
                return;
            }

            if (ActiveMode == GameModeType.Challenge)
            {
                if (RunState == ModeRunState.AwaitingNextRound)
                {
                    StartNextChallengeMatch();
                }
                else if (RunState == ModeRunState.Failed)
                {
                    StartChallengeMode(ActiveChallengeRule, out _);
                }
                else
                {
                    ExitActiveMode(cancelActiveMatch: true);
                }
                return;
            }

            if (TeksMatchManager.Instance != null)
            {
                TeksMatchManager.Instance.Rematch();
            }
        }

        public void HandleResultSecondaryAction()
        {
            if (ActiveMode != GameModeType.Story)
            {
                ExitActiveMode(cancelActiveMatch: true);
            }
            else if (TeksMatchManager.Instance != null)
            {
                TeksMatchManager.Instance.CancelChallenge();
            }
        }

        // --- Safe Positioning & Selection Helpers ---

        private void CaptureReturnTransform()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            _returnPlayerPos = player.transform.position;
            _returnPlayerRot = player.transform.rotation;
            _hasReturnTransform = true;
        }

        private void RestoreReturnTransform()
        {
            if (!_hasReturnTransform) return;
            var player = PlayerController.Instance;
            if (player != null)
            {
                player.TeleportForMatch(_returnPlayerPos, _returnPlayerRot);
            }
            _hasReturnTransform = false;
        }

        private void PositionForTournamentRound(NpcOpponent opp)
        {
            RestoreRelocatedTournamentOpponent();

            var player = PlayerController.Instance;
            if (player == null || opp == null) return;

            Vector3 arenaCenter = BarangayCourtBuilder.Instance != null
                ? BarangayCourtBuilder.Instance.TournamentBattleCenter
                : new Vector3(-22.8f, 0f, -28.5f);

            _relocatedTournamentOpponent = opp;
            _savedOpponentPos = opp.transform.position;
            _savedOpponentRot = opp.transform.rotation;
            _savedOpponentWanderCenter = opp.WanderCenter;

            Vector3 oppArenaPos = new Vector3(arenaCenter.x, 0f, arenaCenter.z - 1.15f);
            Vector3 playerArenaPos = new Vector3(arenaCenter.x, 0f, arenaCenter.z + 1.15f);

            opp.transform.position = oppArenaPos;
            opp.transform.rotation = Quaternion.identity;
            opp.WanderCenter = oppArenaPos;

            player.TeleportForMatch(playerArenaPos, Quaternion.Euler(0f, 180f, 0f));
        }

        private void RestoreRelocatedTournamentOpponent()
        {
            if (_relocatedTournamentOpponent == null) return;
            _relocatedTournamentOpponent.transform.position = _savedOpponentPos;
            _relocatedTournamentOpponent.transform.rotation = _savedOpponentRot;
            _relocatedTournamentOpponent.WanderCenter = _savedOpponentWanderCenter;
            _relocatedTournamentOpponent = null;
        }

        private static void PositionPlayerNearOpponent(NpcOpponent npc)
        {
            var player = PlayerController.Instance;
            if (player == null || npc == null) return;

            Vector3 oppPos = npc.transform.position;
            oppPos.y = 0f;

            Vector3 forward = npc.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();

            Vector3 desired = oppPos + forward * 2.15f;
            desired.x = Mathf.Clamp(desired.x, -28.0f, 28.0f);
            desired.z = Mathf.Clamp(desired.z, -36.0f, 14.5f);
            desired.y = 0f;

            // If clamping pulled the spot too close to the NPC, try the opposite direction
            if ((desired - oppPos).sqrMagnitude < 1.25f)
            {
                desired = oppPos - forward * 2.15f;
                desired.x = Mathf.Clamp(desired.x, -28.0f, 28.0f);
                desired.z = Mathf.Clamp(desired.z, -36.0f, 14.5f);
                desired.y = 0f;
            }

            Vector3 lookDir = oppPos - desired;
            lookDir.y = 0f;
            Quaternion rot = lookDir.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(lookDir.normalized, Vector3.up)
                : Quaternion.identity;

            player.TeleportForMatch(desired, rot);
        }

        private static NpcOpponent PickOpponent(
            List<NpcOpponent> pool,
            List<NpcOpponent> used,
            Predicate<NpcOpponent> preferred,
            Predicate<NpcOpponent> secondary,
            bool preferLowestDifficulty)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                var n = pool[i];
                if (n != null && !used.Contains(n) && preferred(n))
                {
                    return n;
                }
            }

            for (int i = 0; i < pool.Count; i++)
            {
                var n = pool[i];
                if (n != null && !used.Contains(n) && secondary(n))
                {
                    return n;
                }
            }

            NpcOpponent bestFallback = null;
            int bestScore = preferLowestDifficulty ? int.MaxValue : int.MinValue;
            for (int i = 0; i < pool.Count; i++)
            {
                var n = pool[i];
                if (n == null || used.Contains(n)) continue;
                int score = GetDifficultySortRank(n);
                if (preferLowestDifficulty ? score < bestScore : score > bestScore)
                {
                    bestScore = score;
                    bestFallback = n;
                }
            }

            return bestFallback;
        }

        private static int GetDifficultySortRank(NpcOpponent npc)
        {
            if (npc == null) return 0;
            if (npc.IsChampion) return 5;
            return npc.Difficulty switch
            {
                AIDifficulty.Easy => 1,
                AIDifficulty.Medium => 2,
                AIDifficulty.Hard => 3,
                AIDifficulty.Boss => 4,
                AIDifficulty.Extreme => 4,
                _ => 1
            };
        }
    }
}
