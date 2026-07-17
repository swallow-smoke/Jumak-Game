using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts._005_Data.Upgrade
{
    // 상점에 진열되는 모든 업그레이드 목록. 생성기가 기획서 6번 값으로 채운다.
    [CreateAssetMenu(menuName = "Joomak/Upgrade/Catalog", fileName = "UpgradeCatalog")]
    public sealed class UpgradeCatalog : ScriptableObject
    {
        [SerializeField] private List<UpgradeDefinition> upgrades = new();

        private Dictionary<UpgradeId, UpgradeDefinition> byId;

        public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;

        public bool TryGet(UpgradeId id, out UpgradeDefinition definition)
        {
            BuildIndexIfNeeded();
            return byId.TryGetValue(id, out definition);
        }

        public IEnumerable<UpgradeDefinition> InCategory(UpgradeCategory category)
        {
            foreach (UpgradeDefinition definition in upgrades)
            {
                if (definition.Category == category)
                {
                    yield return definition;
                }
            }
        }

        private void OnEnable() => byId = null;
        private void OnValidate() => byId = null;

        private void BuildIndexIfNeeded()
        {
            if (byId != null)
            {
                return;
            }

            byId = new Dictionary<UpgradeId, UpgradeDefinition>();
            foreach (UpgradeDefinition definition in upgrades)
            {
                if (!byId.TryAdd(definition.Id, definition))
                {
                    Debug.LogWarning($"[UpgradeCatalog] 중복된 업그레이드 id: {definition.Id}", this);
                }
            }
        }
    }
}
