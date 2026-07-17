using _001_Scripts._001_Manager;
using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._000_Structure.Inventory;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Hall
{
    // 기획서 3번의 '서빙 받는 곳'. 주방과 홀이 함께 쓰는 유일한 접점이다.
    // 주방이 완성 요리를 올려두면 홀이 집어가고, 홀이 회수한 빈 그릇을 여기로 반납한다.
    public sealed class ServingCounter : BaseStructure, IItemContainerOwner
    {
        [SerializeField] private string structureId = "ServingCounter";
        [SerializeField] private InventoryModel inventory = new();

        public InventoryModel Inventory => inventory;
        public string StructureId => structureId;

        public override void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out ISingleItemCarrier carrier))
            {
                return;
            }

            if (carrier.HeldItem is WorldItem heldItem)
            {
                TryPutDown(carrier, heldItem);
                return;
            }

            TryPickUpDish(carrier, interactor);
        }

        private void TryPutDown(ISingleItemCarrier carrier, WorldItem heldItem)
        {
            if (heldItem.Item == null)
            {
                return;
            }

            // 빈 그릇 반납은 주방 재고로 쌓지 않고 그대로 소멸시킨다.
            if (heldItem.Item.Category == ItemCategory.Plate)
            {
                carrier.TryConsumeHeldItem(heldItem);
                return;
            }

            if (inventory.TryAdd(heldItem.Item, 1))
            {
                carrier.TryConsumeHeldItem(heldItem);
            }
        }

        private void TryPickUpDish(ISingleItemCarrier carrier, GameObject interactor)
        {
            if (inventory.Stacks.Count == 0)
            {
                return;
            }

            ItemStack stack = inventory.Stacks[0];
            if (!WorldItem.TryCreate(stack.Item, transform.position, out WorldItem worldItem))
            {
                return;
            }

            if (!carrier.TryCarry(worldItem))
            {
                Destroy(worldItem.gameObject);
                return;
            }

            ItemBase pickedItem = stack.Item;
            inventory.TryRemove(pickedItem, 1);

            if (pickedItem.Category == ItemCategory.Dish && HallManager.Instance != null)
            {
                HallManager.Instance.NotifyDishCollected(pickedItem.ItemId, interactor);
            }
        }
    }
}
