using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _001_Scripts._005_Data.Config
{
    [Serializable]
    public sealed class GameBalanceData
    {
        public int version = 1;
        public PlayerBalance player = new();
        public DeliveryBalance delivery = new();
        public HallEventBalance hallEvents = new();
        public CustomerBalance customer = new();
        public HallBalance hall = new();
        public ReputationBalance reputation = new();
        public List<ComponentFieldOverride> componentOverrides = new();

        public void Sanitize()
        {
            player ??= new PlayerBalance();
            delivery ??= new DeliveryBalance();
            hallEvents ??= new HallEventBalance();
            customer ??= new CustomerBalance();
            hall ??= new HallBalance();
            reputation ??= new ReputationBalance();
            componentOverrides ??= new List<ComponentFieldOverride>();
            delivery.bundleWeights ??= new List<BundleSpawnWeight>();
        }
    }

    [Serializable]
    public sealed class PlayerBalance
    {
        public float moveSpeed = 5f;
        public float rotationLerpSpeed = 10f;
        public float mass = 8f;
        public float dashSpeedMultiplier = 3f;
        public float dashDurationSeconds = 0.18f;
        public float dashCooldownSeconds = 5f;
        public float interactionRadius = 1.2f;
        public float interactionProbeRadius = 0.42f;
        public float interactionReachPadding = 0.65f;
        public float heldItemDropDistance = 1f;
        public float broomSwingRadius = 3.2f;
        public float broomSwingHalfAngle = 110f;
    }

    [Serializable]
    public sealed class DeliveryBalance
    {
        public float intervalSeconds = 60f;
        public int bundleTypesPerDelivery = 3;
        public int minStockPerBundle = 2;
        public int maxStockPerBundle = 3;
        public float bundleSpacing = 2f;
        public bool deliverAtDayStart = true;
        public float defaultBundleWeight = 1f;
        public List<BundleSpawnWeight> bundleWeights = new();

        public float GetWeight(string bundleId)
        {
            if (!string.IsNullOrWhiteSpace(bundleId))
            {
                foreach (BundleSpawnWeight entry in bundleWeights)
                {
                    if (entry != null && string.Equals(entry.bundleId, bundleId, StringComparison.OrdinalIgnoreCase))
                    {
                        return Mathf.Max(0f, entry.weight);
                    }
                }
            }

            return Mathf.Max(0f, defaultBundleWeight);
        }
    }

    [Serializable]
    public sealed class BundleSpawnWeight
    {
        public string bundleId;
        public float weight = 1f;
    }

    [Serializable]
    public sealed class HallEventBalance
    {
        public float eventIntervalSeconds = 45f;
        public float resolveSeconds = 30f;
        public float rowdyChance = 0.15f;
        public float dineAndDashChance = 0.15f;
        public float dineAndDashTelegraphSeconds = 3f;
        public int rowdyHits = 5;
        public int trashHits = 3;
        public int dineAndDashHits = 1;
        public int ironBroomRowdyHits = 3;
        public int ironBroomTrashHits = 2;
    }

    [Serializable]
    public sealed class CustomerBalance
    {
        public float moveSpeed = 2.4f;
        public float arriveThreshold = 0.22f;
        public float followDistance = 2.2f;
        public float mass = 1f;
        public float movementDamping = 3f;
        public float seatReturnSpeed = 4.5f;
        public float seatReturnResponsiveness = 8f;
        public float seatSnapDistance = 0.04f;
        public float seatArrivalDistance = 0.85f;
        public float avoidRadius = 0.78f;
        public float avoidLookAhead = 3.1f;
        public float foodSeconds = 60f;
        public float startingSatisfaction = 100f;
        public float minSatisfactionDecayPerSecond = 0.5f;
        public float maxSatisfactionDecayPerSecond = 2f;
        public float minDecideSeconds = 5f;
        public float maxDecideSeconds = 10f;
        public float orderChance = 1f;
        public float minEatSeconds = 10f;
        public float maxEatSeconds = 60f;
    }

    [Serializable]
    public sealed class HallBalance
    {
        public float customerSpawnIntervalSeconds = 20f;
        public int startingTableCount = 2;
        public int maxTableCount = 6;
    }

    [Serializable]
    public sealed class ReputationBalance
    {
        public int startingValue = 20;
        public int maximumValue = 100;
        public int deathEndingThreshold = 0;
        public float endingFadeSeconds = 1.2f;
        public bool deleteSaveOnDeath = true;
    }

    [Serializable]
    public sealed class ComponentFieldOverride
    {
        public bool enabled = true;
        public string scene = "InGame";
        public string objectPath = "";
        public string component = "";
        public string field = "";
        public string value = "";
    }

    public static class GameBalance
    {
        private const string FileName = "GameBalance.json";
        private static bool installed;

        public static GameBalanceData Current { get; private set; } = new();
        public static string FilePath => Path.Combine(Application.streamingAssetsPath, FileName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            Load();
            SceneManager.sceneLoaded -= ApplySceneOverrides;
            SceneManager.sceneLoaded += ApplySceneOverrides;
            installed = true;
        }

        public static void EnsureLoaded()
        {
            if (!installed)
            {
                Load();
                installed = true;
            }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    Current = new GameBalanceData();
                    Current.Sanitize();
                    TryWriteDefaultFile();
                    Debug.LogWarning($"[Balance] 설정 파일이 없어 기본값을 사용합니다: {FilePath}");
                    return;
                }

                string json = File.ReadAllText(FilePath);
                Current = JsonUtility.FromJson<GameBalanceData>(json) ?? new GameBalanceData();
                Current.Sanitize();
                Debug.Log($"[Balance] JSON 설정을 불러왔습니다: {FilePath}");
            }
            catch (Exception exception)
            {
                Current = new GameBalanceData();
                Current.Sanitize();
                Debug.LogError($"[Balance] JSON 로드 실패, 기본값 사용: {exception.Message}");
            }
        }

        private static void TryWriteDefaultFile()
        {
            try
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
                File.WriteAllText(FilePath, JsonUtility.ToJson(Current, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Balance] 기본 설정 파일 생성 실패: {exception.Message}");
            }
        }

        private static void ApplySceneOverrides(Scene scene, LoadSceneMode _)
        {
            foreach (ComponentFieldOverride entry in Current.componentOverrides)
            {
                if (entry == null || !entry.enabled ||
                    (!string.IsNullOrWhiteSpace(entry.scene) && !string.Equals(entry.scene, scene.name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                ApplyOverride(scene, entry);
            }
        }

        private static void ApplyOverride(Scene scene, ComponentFieldOverride entry)
        {
            bool applied = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null || !Matches(behaviour, entry))
                    {
                        continue;
                    }

                    FieldInfo field = behaviour.GetType().GetField(entry.field,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field == null || !TryConvert(entry.value, field.FieldType, out object value))
                    {
                        Debug.LogWarning($"[Balance] 적용 실패: {entry.component}.{entry.field} = {entry.value}");
                        continue;
                    }

                    field.SetValue(behaviour, value);
                    applied = true;
                }
            }

            if (!applied)
            {
                Debug.LogWarning($"[Balance] 대상을 찾지 못함: {entry.scene}/{entry.objectPath}/{entry.component}.{entry.field}");
            }
        }

        private static bool Matches(MonoBehaviour behaviour, ComponentFieldOverride entry)
        {
            Type type = behaviour.GetType();
            if (!string.Equals(type.Name, entry.component, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(type.FullName, entry.component, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.objectPath))
            {
                return true;
            }

            return string.Equals(behaviour.gameObject.name, entry.objectPath, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(GetHierarchyPath(behaviour.transform), entry.objectPath, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetHierarchyPath(Transform target)
        {
            string path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = $"{target.name}/{path}";
            }

            return path;
        }

        private static bool TryConvert(string raw, Type type, out object result)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            if (type == typeof(float) && float.TryParse(raw, NumberStyles.Float, culture, out float floatValue))
            {
                result = floatValue;
                return true;
            }

            if (type == typeof(double) && double.TryParse(raw, NumberStyles.Float, culture, out double doubleValue))
            {
                result = doubleValue;
                return true;
            }

            if (type == typeof(int) && int.TryParse(raw, NumberStyles.Integer, culture, out int intValue))
            {
                result = intValue;
                return true;
            }

            if (type == typeof(long) && long.TryParse(raw, NumberStyles.Integer, culture, out long longValue))
            {
                result = longValue;
                return true;
            }

            if (type == typeof(bool) && bool.TryParse(raw, out bool boolValue))
            {
                result = boolValue;
                return true;
            }

            if (type == typeof(string))
            {
                result = raw;
                return true;
            }

            result = null;
            return false;
        }
    }
}
