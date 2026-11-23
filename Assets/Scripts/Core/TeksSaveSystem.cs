using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TeksMaster.Core
{
    [Serializable]
    public class AreaWinRecord
    {
        public int AreaId;
        public int WinCount;

        public AreaWinRecord()
        {
        }

        public AreaWinRecord(BarangayAreaId areaId, int winCount)
        {
            AreaId = (int)areaId;
            WinCount = Mathf.Max(0, winCount);
        }
    }

    [Serializable]
    public class TeksSaveData
    {
        public int SaveVersion = 1;
        public string PlayerNickname = "TeksMaster";
        public int SelectedGender = 0;
        public bool HasStartedGame = false;

        public int Coins = 2450;
        public int Gems = 25;
        public string EquippedPamatoId = "hero";

        public int MatchesWon = 0;
        public int MatchesPlayed = 0;
        public int WinStreak = 0;
        public int BestWinStreak = 0;
        public int TournamentsWon = 0;
        public int ChallengesCompleted = 0;

        public bool FreePlayUnlockAll = false;
        public string LastDailyMissionDate = string.Empty;
        public string LastDailyFreeDrawDate = string.Empty;

        public List<OwnedTeksCard> OwnedCards = new List<OwnedTeksCard>();
        public List<int> UnlockedAreas = new List<int>();
        public List<AreaWinRecord> AreaWins = new List<AreaWinRecord>();
        public List<string> DefeatedNpcIds = new List<string>();
        public List<string> DiscoveredHiddenCardIds = new List<string>();
        public List<BarangayQuest> Quests = new List<BarangayQuest>();
        public AudioSettingsData AudioSettings = new AudioSettingsData();
    }

    public static class TeksSaveSystem
    {
        public const string SaveFileName = "teks_save.json";
        public const string PrefKeyNickname = "Teks_Nickname";
        public const string PrefKeyGender = "Teks_Gender";
        public const string PrefKeyMasterVolume = "Teks_MasterVolume";
        public const string PrefKeyBgmVolume = "Teks_BgmVolume";
        public const string PrefKeySfxVolume = "Teks_SfxVolume";
        public const string PrefKeyIsMuted = "Teks_IsMuted";
        public const string PrefKeyIsFullscreen = "Teks_IsFullscreen";

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static string BackupFilePath => SaveFilePath + ".bak";
        public static string TempFilePath => SaveFilePath + ".tmp";

        public static bool HasSaveFile()
        {
            return File.Exists(SaveFilePath) || File.Exists(BackupFilePath);
        }

        public static bool Save(TeksSaveData data)
        {
            if (!Application.isPlaying) return false;
            if (data == null) return false;

            try
            {
                MigrateAndSanitize(data);
                string json = JsonUtility.ToJson(data, true);
                string dir = Path.GetDirectoryName(SaveFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(TempFilePath, json);
                if (File.Exists(SaveFilePath))
                {
                    File.Copy(SaveFilePath, BackupFilePath, true);
                    File.Delete(SaveFilePath);
                }
                File.Move(TempFilePath, SaveFilePath);

                SyncPlayerPrefs(data);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TeksSaveSystem] Failed to save game data: {ex.Message}");
                return false;
            }
        }

        public static bool TryLoad(out TeksSaveData data)
        {
            data = null;
            if (!Application.isPlaying) return false;

            if (TryReadJsonFile(SaveFilePath, out data))
            {
                MigrateAndSanitize(data);
                return true;
            }

            if (TryReadJsonFile(BackupFilePath, out data))
            {
                MigrateAndSanitize(data);
                return true;
            }

            return false;
        }

        public static void DeleteSave()
        {
            if (!Application.isPlaying) return;
            DeleteSaveFilesForce();
        }

        public static void DeleteSaveFilesForce()
        {
            TryDeleteFile(SaveFilePath);
            TryDeleteFile(BackupFilePath);
            TryDeleteFile(TempFilePath);
        }

        public static void SyncPlayerPrefs(TeksSaveData data)
        {
            if (!Application.isPlaying || data == null) return;

            PlayerPrefs.SetString(PrefKeyNickname, data.PlayerNickname);
            PlayerPrefs.SetInt(PrefKeyGender, Mathf.Clamp(data.SelectedGender, 0, 1));
            if (data.AudioSettings != null)
            {
                PlayerPrefs.SetFloat(PrefKeyMasterVolume, Mathf.Clamp01(data.AudioSettings.MasterVolume));
                PlayerPrefs.SetFloat(PrefKeyBgmVolume, Mathf.Clamp01(data.AudioSettings.BgmVolume));
                PlayerPrefs.SetFloat(PrefKeySfxVolume, Mathf.Clamp01(data.AudioSettings.SfxVolume));
                PlayerPrefs.SetInt(PrefKeyIsMuted, data.AudioSettings.IsMuted ? 1 : 0);
                PlayerPrefs.SetInt(PrefKeyIsFullscreen, data.AudioSettings.IsFullscreen ? 1 : 0);
            }
            PlayerPrefs.Save();
        }

        public static AudioSettingsData LoadAudioSettingsFromPrefs()
        {
            var settings = new AudioSettingsData();
            if (PlayerPrefs.HasKey(PrefKeyMasterVolume))
                settings.MasterVolume = PlayerPrefs.GetFloat(PrefKeyMasterVolume, settings.MasterVolume);
            if (PlayerPrefs.HasKey(PrefKeyBgmVolume))
                settings.BgmVolume = PlayerPrefs.GetFloat(PrefKeyBgmVolume, settings.BgmVolume);
            if (PlayerPrefs.HasKey(PrefKeySfxVolume))
                settings.SfxVolume = PlayerPrefs.GetFloat(PrefKeySfxVolume, settings.SfxVolume);
            if (PlayerPrefs.HasKey(PrefKeyIsMuted))
                settings.IsMuted = PlayerPrefs.GetInt(PrefKeyIsMuted, 0) != 0;
            if (PlayerPrefs.HasKey(PrefKeyIsFullscreen))
                settings.IsFullscreen = PlayerPrefs.GetInt(PrefKeyIsFullscreen, 1) != 0;
            settings.Clamp();
            return settings;
        }

        public static void SaveAudioSettingsToPrefs(AudioSettingsData settings)
        {
            if (!Application.isPlaying || settings == null) return;
            settings.Clamp();
            PlayerPrefs.SetFloat(PrefKeyMasterVolume, settings.MasterVolume);
            PlayerPrefs.SetFloat(PrefKeyBgmVolume, settings.BgmVolume);
            PlayerPrefs.SetFloat(PrefKeySfxVolume, settings.SfxVolume);
            PlayerPrefs.SetInt(PrefKeyIsMuted, settings.IsMuted ? 1 : 0);
            PlayerPrefs.SetInt(PrefKeyIsFullscreen, settings.IsFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void MigrateAndSanitize(TeksSaveData data)
        {
            if (data == null) return;

            data.SaveVersion = 1;
            string cleanedName = string.IsNullOrWhiteSpace(data.PlayerNickname) ? "TeksMaster" : data.PlayerNickname.Trim();
            if (cleanedName.Length > 18) cleanedName = cleanedName.Substring(0, 18);
            data.PlayerNickname = cleanedName;
            data.SelectedGender = Mathf.Clamp(data.SelectedGender, 0, 1);

            data.Coins = Mathf.Max(0, data.Coins);
            data.Gems = Mathf.Max(0, data.Gems);
            data.MatchesWon = Mathf.Max(0, data.MatchesWon);
            data.MatchesPlayed = Mathf.Max(data.MatchesWon, data.MatchesPlayed);
            data.WinStreak = Mathf.Max(0, data.WinStreak);
            data.BestWinStreak = Mathf.Max(data.WinStreak, data.BestWinStreak);
            data.TournamentsWon = Mathf.Max(0, data.TournamentsWon);
            data.ChallengesCompleted = Mathf.Max(0, data.ChallengesCompleted);

            if (data.LastDailyMissionDate == null) data.LastDailyMissionDate = string.Empty;
            if (data.LastDailyFreeDrawDate == null) data.LastDailyFreeDrawDate = string.Empty;
            if (data.AudioSettings == null) data.AudioSettings = new AudioSettingsData();
            data.AudioSettings.Clamp();

            // Sanitize & merge OwnedCards
            if (data.OwnedCards == null) data.OwnedCards = new List<OwnedTeksCard>();
            var cleanedCards = new List<OwnedTeksCard>();
            for (int i = 0; i < data.OwnedCards.Count; i++)
            {
                var card = data.OwnedCards[i];
                if (card == null || string.IsNullOrEmpty(card.CardId)) continue;
                if (TeksDatabase.GetCard(card.CardId) == null) continue;

                card.StarRank = Mathf.Clamp(card.StarRank, 1, 5);
                card.DuplicateCount = Mathf.Max(0, card.DuplicateCount);
                card.FusionStage = Mathf.Max(0, card.FusionStage);

                OwnedTeksCard existing = null;
                for (int j = 0; j < cleanedCards.Count; j++)
                {
                    if (cleanedCards[j].CardId == card.CardId)
                    {
                        existing = cleanedCards[j];
                        break;
                    }
                }

                if (existing != null)
                {
                    existing.StarRank = Mathf.Max(existing.StarRank, card.StarRank);
                    existing.DuplicateCount += 1 + card.DuplicateCount;
                    existing.FusionStage = Mathf.Max(existing.FusionStage, card.FusionStage);
                }
                else
                {
                    cleanedCards.Add(card);
                }
            }
            data.OwnedCards = cleanedCards;

            // Sanitize UnlockedAreas (Areas 0, 1, and 5 always unlocked)
            if (data.UnlockedAreas == null) data.UnlockedAreas = new List<int>();
            var cleanedAreas = new List<int>();
            AddUniqueAreaInt(cleanedAreas, (int)BarangayAreaId.PlayersNeighborhood);
            AddUniqueAreaInt(cleanedAreas, (int)BarangayAreaId.SariSariAndKalye);
            AddUniqueAreaInt(cleanedAreas, (int)BarangayAreaId.TournamentArena);
            for (int i = 0; i < data.UnlockedAreas.Count; i++)
            {
                int areaInt = data.UnlockedAreas[i];
                if (areaInt >= 0 && areaInt <= 5)
                {
                    AddUniqueAreaInt(cleanedAreas, areaInt);
                }
            }
            cleanedAreas.Sort();
            data.UnlockedAreas = cleanedAreas;

            // Sanitize AreaWins
            if (data.AreaWins == null) data.AreaWins = new List<AreaWinRecord>();
            var cleanedAreaWins = new List<AreaWinRecord>();
            for (int i = 0; i < data.AreaWins.Count; i++)
            {
                var rec = data.AreaWins[i];
                if (rec == null || rec.AreaId < 0 || rec.AreaId > 5) continue;
                int wins = Mathf.Max(0, rec.WinCount);
                bool merged = false;
                for (int j = 0; j < cleanedAreaWins.Count; j++)
                {
                    if (cleanedAreaWins[j].AreaId == rec.AreaId)
                    {
                        cleanedAreaWins[j].WinCount = Mathf.Max(cleanedAreaWins[j].WinCount, wins);
                        merged = true;
                        break;
                    }
                }
                if (!merged)
                {
                    cleanedAreaWins.Add(new AreaWinRecord((BarangayAreaId)rec.AreaId, wins));
                }
            }
            data.AreaWins = cleanedAreaWins;

            // Sanitize string ID lists
            data.DefeatedNpcIds = SanitizeIdList(data.DefeatedNpcIds);
            data.DiscoveredHiddenCardIds = SanitizeIdList(data.DiscoveredHiddenCardIds);

            // Sanitize Quests
            if (data.Quests == null) data.Quests = new List<BarangayQuest>();
            var cleanedQuests = new List<BarangayQuest>();
            for (int i = 0; i < data.Quests.Count; i++)
            {
                var q = data.Quests[i];
                if (q == null || string.IsNullOrWhiteSpace(q.Id)) continue;
                q.CurrentProgress = Mathf.Max(0, q.CurrentProgress);
                if (q.TargetCount < 1) q.TargetCount = 1;
                cleanedQuests.Add(q);
            }
            data.Quests = cleanedQuests;
        }

        private static bool TryReadJsonFile(string path, out TeksSaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;
                data = JsonUtility.FromJson<TeksSaveData>(json);
                return data != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TeksSaveSystem] Failed to read save file '{path}': {ex.Message}");
                data = null;
                return false;
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TeksSaveSystem] Could not delete '{path}': {ex.Message}");
            }
        }

        private static void AddUniqueAreaInt(List<int> list, int areaInt)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == areaInt) return;
            }
            list.Add(areaInt);
        }

        private static List<string> SanitizeIdList(List<string> source)
        {
            var result = new List<string>();
            if (source == null) return result;
            for (int i = 0; i < source.Count; i++)
            {
                string item = source[i];
                if (string.IsNullOrWhiteSpace(item)) continue;
                string normalized = item.Trim().ToLowerInvariant();
                bool exists = false;
                for (int j = 0; j < result.Count; j++)
                {
                    if (result[j] == normalized)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists) result.Add(normalized);
            }
            return result;
        }
    }
}
