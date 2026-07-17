using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Inventory
{
    // 정해진 재료 하나만 보급받아 쌓아두는 보급함.
    // suppliedItem을 들고 상호작용하면 채워지고, 빈손으로 상호작용하면 하나 꺼내준다.
    // StorageStructure(아무거나 담는 창고)와 달리 아이템 종류가 고정이고,
    // 최대 보관 수량도 아이템의 MaxStack이 아니라 이 구조물에서 직접 정한다.
    public sealed class SupplyBox : BaseStructure
    {
        [SerializeField] private ItemBase suppliedItem;
        [SerializeField, Min(1)] private int maxStack = 10;
        [SerializeField] private SpriteRenderer itemDisplay;
        [SerializeField, Min(0)] private int storedCount;

        public ItemBase SuppliedItem => suppliedItem;
        public int MaxStack => maxStack;
        public int StoredCount => storedCount;
        public bool IsFull => storedCount >= maxStack;
        public bool IsEmpty => storedCount <= 0;

        // 최대 재고를 넘는 만큼은 받지 않고 잘라낸다. 실제로 받아들인 수량을 돌려준다.
        public int AddStock(int amount)
        {
            int accepted = Mathf.Clamp(amount, 0, maxStack - storedCount);
            storedCount += accepted;
            return accepted;
        }

        public override void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out ISingleItemCarrier carrier))
            {
                return;
            }

            if (carrier.HeldItem is WorldItem heldItem)
            {
                TryReceiveSupply(carrier, heldItem);
                return;
            }

            TryTakeOne(carrier);
        }

        private void TryReceiveSupply(ISingleItemCarrier carrier, WorldItem heldItem)
        {
            if (suppliedItem == null || heldItem.Item != suppliedItem || IsFull)
            {
                return;
            }

            storedCount++;
            carrier.TryConsumeHeldItem(heldItem);
        }

        private void TryTakeOne(ISingleItemCarrier carrier)
        {
            if (IsEmpty || !WorldItem.TryCreate(suppliedItem, transform.position, out WorldItem worldItem))
            {
                return;
            }

            if (carrier.TryCarry(worldItem))
            {
                storedCount--;
                return;
            }

            Destroy(worldItem.gameObject);
        }

        protected override void Awake()
        {
            base.Awake();
            UpdateItemDisplay();
        }

        private void OnValidate()
        {
            maxStack = Mathf.Max(1, maxStack);
            storedCount = Mathf.Clamp(storedCount, 0, maxStack);
            UpdateItemDisplay();
        }

        // suppliedItem의 worldPrefab에 붙은 스프라이트를 그대로 가져다 보급함 위에 띄운다.
        private void UpdateItemDisplay()
        {
            if (itemDisplay == null)
            {
                return;
            }

            itemDisplay.sprite = suppliedItem != null && suppliedItem.WorldPrefab != null
                ? suppliedItem.WorldPrefab.GetComponentInChildren<SpriteRenderer>()?.sprite
                : null;
        }
    }
}
