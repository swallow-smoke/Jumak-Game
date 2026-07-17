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
        private static List<UpgradeDefinition> BuildDefinitions() => new()
        {
            // 6-1 공용
            new(UpgradeId.Dash, UpgradeCategory.Common, "대쉬",
                "짧은 거리 돌진 이동 습득 (쿨타임 5초)", 150, 1f, 1, null),
            new(UpgradeId.MoveSpeed1, UpgradeCategory.Common, "이동 속도 1단계",
                "이동속도 +10%", 80, 1f, 1, null),
            new(UpgradeId.MoveSpeed2, UpgradeCategory.Common, "이동 속도 2단계",
                "이동속도 +10% (누적 +20%)", 120, 1f, 1, UpgradeId.MoveSpeed1),
            new(UpgradeId.MoveSpeed3, UpgradeCategory.Common, "이동 속도 3단계",
                "이동속도 +10% (누적 +30%, 상한)", 180, 1f, 1, UpgradeId.MoveSpeed2),
            new(UpgradeId.ReputationHeal, UpgradeCategory.Common, "명성 회복",
                "명성 +10 (최대 100). 반복 구매 가능", 150, 1f, 99, null),

            // 6-2 주방
            new(UpgradeId.CookTime1, UpgradeCategory.Kitchen, "조리시간 감소 1단계",
                "모든 조리 시간 -10%", 120, 1f, 1, null),
            new(UpgradeId.CookTime2, UpgradeCategory.Kitchen, "조리시간 감소 2단계",
                "조리 시간 -10% (누적 -20%)", 180, 1f, 1, UpgradeId.CookTime1),
            new(UpgradeId.CookTime3, UpgradeCategory.Kitchen, "조리시간 감소 3단계",
                "조리 시간 -10% (누적 -30%, 하한)", 270, 1f, 1, UpgradeId.CookTime2),
            new(UpgradeId.FailureDelay1, UpgradeCategory.Kitchen, "실패시간 지연 1단계",
                "완성 요리 실패까지의 시간 +10%", 100, 1f, 1, null),
            new(UpgradeId.FailureDelay2, UpgradeCategory.Kitchen, "실패시간 지연 2단계",
                "실패까지의 시간 +10% (누적 +20%)", 150, 1f, 1, UpgradeId.FailureDelay1),
            new(UpgradeId.FailureDelay3, UpgradeCategory.Kitchen, "실패시간 지연 3단계",
                "실패까지의 시간 +10% (누적 +30%, 상한)", 225, 1f, 1, UpgradeId.FailureDelay2),
            new(UpgradeId.PremiumDish, UpgradeCategory.Kitchen, "고급 요리",
                "요리 판매 시 명성 +1 추가", 300, 1f, 1, null),

            // 6-3 홀
            new(UpgradeId.Patience1, UpgradeCategory.Hall, "손님 인내심 1단계",
                "손님 인내심 +10초", 100, 1f, 1, null),
            new(UpgradeId.Patience2, UpgradeCategory.Hall, "손님 인내심 2단계",
                "인내심 +10초 (누적 +20초)", 150, 1f, 1, UpgradeId.Patience1),
            new(UpgradeId.Patience3, UpgradeCategory.Hall, "손님 인내심 3단계",
                "인내심 +10초 (누적 +30초, 상한)", 225, 1f, 1, UpgradeId.Patience2),
            new(UpgradeId.IronBroom, UpgradeCategory.Hall, "철제 손잡이 빗자루",
                "손놈 척결 5회→3회, 청소 3회→2회", 200, 1f, 1, null),
            // 기획서 6-3: 시작 2개 -> 최대 6개, 최대 4회 구매
            new(UpgradeId.TableAdd, UpgradeCategory.Hall, "테이블 추가",
                "테이블 +1개(좌석 +2석). 최대 6개까지", 180, 1.5f, 4, null)
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
