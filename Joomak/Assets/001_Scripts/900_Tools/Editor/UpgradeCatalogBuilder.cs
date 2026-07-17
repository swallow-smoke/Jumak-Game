using System.Collections.Generic;
using System.IO;
using System.Reflection;
using _001_Scripts._005_Data.Upgrade;
using UnityEditor;
using UnityEngine;

namespace _001_Scripts._900_Tools.Editor
{
    // 기획서 6번 값으로 UpgradeCatalog와 RunState 에셋을 만든다. 몇 번 실행해도 같은 결과.
    public static class UpgradeCatalogBuilder
    {
        private const string DataFolder = "Assets/002_Resources/001_Datas";
        private const string CatalogPath = DataFolder + "/UpgradeCatalog.asset";
        private const string ResourcesFolder = "Assets/Resources";
        private const string RunStatePath = ResourcesFolder + "/RunState.asset";

        [MenuItem("Joomak/Upgrade/Build Upgrade Catalog")]
        public static void Build()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(ResourcesFolder);

            UpgradeCatalog catalog = LoadOrCreate<UpgradeCatalog>(CatalogPath);
            SetPrivateField(catalog, "upgrades", BuildDefinitions());
            EditorUtility.SetDirty(catalog);

            // RunState는 Resources 밑에 있어야 RunState.Instance가 경로로 읽는다.
            LoadOrCreate<RunState>(RunStatePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UpgradeCatalogBuilder] 완료: {CatalogPath}, {RunStatePath}");
        }

        // 기획서 6-1 / 6-2 / 6-3. 단계형은 이전 단계를 선행 조건으로 건다.
        // UpgradeDefinition(id, category, displayName, basePrice, maxPurchases, priceMultiplier, prerequisite, description)
        private static List<UpgradeDefinition> BuildDefinitions() => new()
        {
            // 6-1 공용
            new(UpgradeId.Dash, UpgradeCategory.Common, "대쉬",
                150, 1, 1f, null, "짧은 거리 돌진 이동 습득 (쿨타임 5초)"),
            new(UpgradeId.MoveSpeed1, UpgradeCategory.Common, "이동 속도 1단계",
                80, 1, 1f, null, "이동속도 +10%"),
            new(UpgradeId.MoveSpeed2, UpgradeCategory.Common, "이동 속도 2단계",
                120, 1, 1f, UpgradeId.MoveSpeed1, "이동속도 +10% (누적 +20%)"),
            new(UpgradeId.MoveSpeed3, UpgradeCategory.Common, "이동 속도 3단계",
                180, 1, 1f, UpgradeId.MoveSpeed2, "이동속도 +10% (누적 +30%, 상한)"),
            new(UpgradeId.ReputationHeal, UpgradeCategory.Common, "명성 회복",
                150, 99, 1f, null, "명성 +10 (최대 100). 반복 구매 가능"),

            // 6-2 주방
            new(UpgradeId.CookTime1, UpgradeCategory.Kitchen, "조리시간 감소 1단계",
                120, 1, 1f, null, "모든 조리 시간 -10%"),
            new(UpgradeId.CookTime2, UpgradeCategory.Kitchen, "조리시간 감소 2단계",
                180, 1, 1f, UpgradeId.CookTime1, "조리 시간 -10% (누적 -20%)"),
            new(UpgradeId.CookTime3, UpgradeCategory.Kitchen, "조리시간 감소 3단계",
                270, 1, 1f, UpgradeId.CookTime2, "조리 시간 -10% (누적 -30%, 하한)"),
            new(UpgradeId.FailureDelay1, UpgradeCategory.Kitchen, "실패시간 지연 1단계",
                100, 1, 1f, null, "완성 요리 실패까지의 시간 +10%"),
            new(UpgradeId.FailureDelay2, UpgradeCategory.Kitchen, "실패시간 지연 2단계",
                150, 1, 1f, UpgradeId.FailureDelay1, "실패까지의 시간 +10% (누적 +20%)"),
            new(UpgradeId.FailureDelay3, UpgradeCategory.Kitchen, "실패시간 지연 3단계",
                225, 1, 1f, UpgradeId.FailureDelay2, "실패까지의 시간 +10% (누적 +30%, 상한)"),
            new(UpgradeId.PremiumDish, UpgradeCategory.Kitchen, "고급 요리",
                300, 1, 1f, null, "요리 판매 시 명성 +1 추가"),

            // 6-3 홀
            new(UpgradeId.Patience1, UpgradeCategory.Hall, "손님 인내심 1단계",
                100, 1, 1f, null, "손님 인내심 +10초"),
            new(UpgradeId.Patience2, UpgradeCategory.Hall, "손님 인내심 2단계",
                150, 1, 1f, UpgradeId.Patience1, "인내심 +10초 (누적 +20초)"),
            new(UpgradeId.Patience3, UpgradeCategory.Hall, "손님 인내심 3단계",
                225, 1, 1f, UpgradeId.Patience2, "인내심 +10초 (누적 +30초, 상한)"),
            new(UpgradeId.IronBroom, UpgradeCategory.Hall, "철제 손잡이 빗자루",
                200, 1, 1f, null, "손놈 척결 5회→3회, 청소 3회→2회"),
            // 기획서 6-3: 시작 2개 -> 최대 6개, 최대 4회 구매
            new(UpgradeId.TableAdd, UpgradeCategory.Hall, "테이블 추가",
                180, 4, 1.5f, null, "테이블 +1개(좌석 +2석). 최대 6개까지")
        };

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                Debug.LogError($"[UpgradeCatalogBuilder] 필드를 못 찾음: {target.GetType().Name}.{name}");
                return;
            }

            field.SetValue(target, value);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
