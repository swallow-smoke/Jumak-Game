using System;
using System.Collections.Generic;
using System.IO;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;

namespace _001_Scripts._001_Manager
{
    [Serializable]
    public sealed class SaveGameData
    {
        public int version = 1;
        public int currentDay = 1;
        public int money;
        public int reputation = 20;
        public int maxReputation = 100;
        public List<UpgradeLevelSave> upgrades = new();
        public float remainingDaySeconds;
        public int revenueThisDay;
        public int lastDayRevenue;
        public bool dayInProgress;
        public string sceneName = "InGame";
        public string savedAtUtc;
    }

    [Serializable]
    public sealed class UpgradeLevelSave
    {
        public string upgradeId;
        public int level;
    }

    public static class SaveGameManager
    {
        private const string FileName = "savegame.json";

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        public static bool HasSave => File.Exists(SavePath);

        public static void Save(SaveGameData data)
        {
            if (data == null)
            {
                return;
            }

            try
            {
                data.savedAtUtc = DateTime.UtcNow.ToString("O");
                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveGame] 저장 실패: {exception.Message}");
            }
        }

        public static bool TryLoad(out SaveGameData data)
        {
            data = null;
            if (!HasSave)
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<SaveGameData>(json);
                return data != null && data.version > 0;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveGame] 불러오기 실패: {exception.Message}");
                return false;
            }
        }

        public static void DeleteSave()
        {
            try
            {
                if (HasSave)
                {
                    File.Delete(SavePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveGame] 저장 파일 삭제 실패: {exception.Message}");
            }
        }

        public static void RestoreGameState(SaveGameData data)
        {
            if (data == null)
            {
                return;
            }

            UpgradeApi.RestoreRun(data.money, data.reputation, data.maxReputation, data.upgrades);
            DayCycleManager.RestoreProgress(data);
        }
    }
}
