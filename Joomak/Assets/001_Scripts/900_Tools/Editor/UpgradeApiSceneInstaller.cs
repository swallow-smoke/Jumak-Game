using System;
using _001_Scripts._004_UI._000_Panels;
using _001_Scripts._005_Data.Upgrade;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _001_Scripts._900_Tools.Editor
{
    public static class UpgradeApiSceneInstaller
    {
        private const string ScenePath = "Assets/000_Scenes/Upgrade.unity";

        [MenuItem("Joomak/Upgrade/Install Upgrade API")]
        public static void Install()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("Upgrade Shop UI");
            if (root == null)
            {
                Debug.LogError("[UpgradeApiSceneInstaller] Upgrade Shop UI를 찾지 못했습니다.");
                return;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            }

            if (root.GetComponent<UpgradeShopApiController>() == null)
            {
                root.AddComponent<UpgradeShopApiController>();
            }

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UpgradeApiSceneInstaller] Upgrade 씬 API 연결 완료");
        }

        [MenuItem("Joomak/Upgrade/Run API Smoke Test")]
        public static void RunSmokeTest()
        {
            UpgradeApi.ResetRun(5000, 20, 100);

            Purchase(UpgradeId.Dash);
            Purchase(UpgradeId.MoveSpeed1);
            Purchase(UpgradeId.MoveSpeed2);
            Purchase(UpgradeId.MoveSpeed3);
            Purchase(UpgradeId.Patience1);
            Purchase(UpgradeId.Patience2);
            Purchase(UpgradeId.Patience3);
            Purchase(UpgradeId.IronBroom);
            for (int i = 0; i < 4; i++) Purchase(UpgradeId.TableAdd);
            Purchase(UpgradeId.CookTime1);
            Purchase(UpgradeId.CookTime2);
            Purchase(UpgradeId.CookTime3);
            Purchase(UpgradeId.FailureDelay1);
            Purchase(UpgradeId.FailureDelay2);
            Purchase(UpgradeId.FailureDelay3);
            Purchase(UpgradeId.PremiumDish);

            bool valid = UpgradeApi.DashUnlocked
                         && Mathf.Approximately(UpgradeApi.MoveSpeedMultiplier, 1.3f)
                         && UpgradeApi.PatienceBonusSeconds == 30
                         && UpgradeApi.IronBroomUnlocked
                         && UpgradeApi.AddedTableCount == 4
                         && Mathf.Approximately(UpgradeApi.CookTimeMultiplier, 0.7f)
                         && Mathf.Approximately(UpgradeApi.FailureDelayMultiplier, 1.3f)
                         && UpgradeApi.SaleReputationBonus == 1;

            UpgradeApi.ResetRun(0, 20, 100);
            if (!valid)
            {
                throw new InvalidOperationException("UpgradeApi smoke test failed.");
            }

            Debug.Log("[UpgradeApi] Smoke test 통과: 구매·선행조건·누적 보너스 정상");
        }

        private static void Purchase(UpgradeId id)
        {
            UpgradePurchaseResult result = UpgradeApi.TryPurchase(id);
            if (result != UpgradePurchaseResult.Success)
            {
                throw new InvalidOperationException($"{id} 구매 실패: {result}");
            }
        }
    }
}
