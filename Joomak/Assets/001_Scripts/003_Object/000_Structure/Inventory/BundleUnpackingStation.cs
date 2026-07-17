using System.Collections.Generic;
using _001_Scripts._001_Manager;
using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Inventory
{
    // 홀에서 보급받은 재료 패키지를 여기서 깐다. 패키지 내용물은 종류가 맞는 SupplyBox 전부에게
    // 각각 그대로 보내진다(박스마다 최대 재고 넘는 양은 그 박스에서만 잘려나감).
    public sealed class BundleUnpackingStation : BaseStructure
    {
        [Header("Audio")]
        [SerializeField] private AudioClip unpackSfx;

        protected override void Awake()
        {
            base.Awake();
            unpackSfx ??= Resources.Load<AudioClip>("006_Audio/interactionSound");
        }

        public override void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out ISingleItemCarrier carrier) ||
                carrier.HeldItem is not IngredientBundle bundle ||
                bundle.BundleData == null)
            {
                return;
            }

            ItemBundleData bundleData = bundle.BundleData;
            if (!carrier.TryConsumeHeldItem(bundle))
            {
                return;
            }

            Distribute(bundleData.GetUnpackedItems());
            AudioManager.Instance?.PlaySfx(unpackSfx);
        }

        private static void Distribute(IEnumerable<ItemAmount> unpackedItems)
        {
            SupplyBox[] supplyBoxes = Object.FindObjectsByType<SupplyBox>(FindObjectsInactive.Exclude);
            foreach (ItemAmount entry in unpackedItems)
            {
                foreach (SupplyBox box in supplyBoxes)
                {
                    if (box.SuppliedItem == entry.Item)
                    {
                        box.AddStock(entry.Amount);
                    }
                }
            }
        }
    }
}
