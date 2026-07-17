using System.Collections.Generic;
using _001_Scripts._001_Manager;
using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._000_Structure.Inventory;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Hall
{
    // 기획서 3번의 '서빙 받는 곳'. 홀과 주방을 가르는 벽에 뚫린 유일한 접점이다.
    // 두 사람이 벽 너머로 물건을 주고받는 창구이며, 재료 상자의 하역장(짐 내리는 곳)도 겸한다.
    //
    // 오가는 것이 두 방향이다:
    //   주방 -> 홀 : 완성 요리   (dishInventory)
    //   홀 -> 주방 : 재료 상자   (bundles) - 해체는 여기가 아니라 주방 UnpackingStation에서 한다
    public sealed class ServingCounter : BaseStructure, IItemContainerOwner
    {
        [SerializeField] private string structureId = "ServingCounter";

        [Tooltip("주방이 올려둔 완성 요리. 홀이 집어간다.")]
        [SerializeField] private InventoryModel dishInventory = new();

        [Header("재료 상자 하역 (기획서 8번)")]
        [Tooltip("홀이 내려놓은 상자가 얹히는 자리. 비워두면 카운터 본체 위치를 쓴다.")]
        [SerializeField] private Transform bundleSlot;

        [Tooltip("카운터에 쌓아둘 수 있는 상자 수. 주방이 안 치우면 홀이 더 못 올린다.")]
        [SerializeField, Min(1)] private int bundleCapacity = 3;

        [SerializeField, Min(0f)] private float bundleStackSpacing = 0.5f;

        // 상자는 ItemBase가 아니라 ItemBundleData를 들고 있어서 InventoryModel에 담기지 않는다.
        // 그래서 개수를 세는 대신 오브젝트를 그대로 얹어둔다.
        private readonly List<IngredientBundle> bundles = new();

        public InventoryModel Inventory => dishInventory;
        public string StructureId => structureId;
        public int BundleCount => bundles.Count;

        private Transform SlotRoot => bundleSlot != null ? bundleSlot : transform;

        protected override void Awake()
        {
            base.Awake();
            UpdateFirstItemDisplay();
        }

        public override void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out ISingleItemCarrier carrier))
            {
                return;
            }

            bool fromHall = IsFromHall(interactor);

            switch (carrier.HeldItem)
            {
                // 상자는 WorldItem이 아니라 CarryableItem을 따로 상속한 형제 타입이라
                // 아래 WorldItem 분기에는 절대 안 걸린다. 반드시 먼저 걸러야 한다.
                case IngredientBundle bundle:
                    TryDropOffBundle(carrier, bundle, fromHall);
                    return;

                case WorldItem heldItem:
                    TryPutDown(carrier, heldItem);
                    return;
            }

            TryPickUp(carrier, interactor, fromHall);
        }

        // 홀은 카운터보다 왼쪽(x<), 주방은 오른쪽(x>)에 있다.
        // Border 벽이 둘을 완전히 갈라놓아 서로 넘어갈 수 없으므로 이 판정이 어긋날 일이 없다.
        private bool IsFromHall(GameObject interactor) => interactor.transform.position.x < transform.position.x;

        // 홀이 상자를 카운터에 내려놓는다. 해체하지 않고 그대로 얹어둔다.
        private void TryDropOffBundle(ISingleItemCarrier carrier, IngredientBundle bundle, bool fromHall)
        {
            // 주방이 상자를 되돌려놓는 흐름은 없다.
            if (!fromHall || bundles.Count >= bundleCapacity)
            {
                return;
            }

            // 먼저 손에서 놓게 한다. TryConsumeHeldItem은 상자를 파괴해버리므로 절대 쓰면 안 된다.
            if (!carrier.TryDropHeldItem(SlotRoot.position))
            {
                return;
            }

            StackOnCounter(bundle, bundles.Count);
            bundles.Add(bundle);
            Debug.Log($"[ServingCounter] 재료 상자 하역 ({bundles.Count}/{bundleCapacity})");
        }

        private void TryPutDown(ISingleItemCarrier carrier, WorldItem heldItem)
        {
            if (heldItem.Item == null)
            {
                return;
            }

            // 빈 그릇 반납은 재고로 쌓지 않고 그대로 소멸시킨다.
            if (heldItem.Item.Category == ItemCategory.Plate)
            {
                carrier.TryConsumeHeldItem(heldItem);
                return;
            }

            if (dishInventory.TryAdd(heldItem.Item, 1))
            {
                carrier.TryConsumeHeldItem(heldItem);
                UpdateFirstItemDisplay();
            }
        }

        // 맨손으로 상호작용하면 자기 쪽이 받아야 할 것을 집는다.
        // 홀은 완성 요리를, 주방은 재료 상자를 가져간다.
        private void TryPickUp(ISingleItemCarrier carrier, GameObject interactor, bool fromHall)
        {
            if (!fromHall)
            {
                TryGiveBundle(carrier);
                return;
            }

            TryGiveDish(carrier, interactor);
        }

        private void TryGiveBundle(ISingleItemCarrier carrier)
        {
            if (bundles.Count == 0)
            {
                return;
            }

            IngredientBundle bundle = bundles[^1];
            bundles.RemoveAt(bundles.Count - 1);

            // TryCarry는 이미 들려 있는(IsCarried) 물건을 거부하므로 먼저 카운터가 놓아줘야 한다.
            bundle.SetWorldPosition(bundle.transform.position);

            if (!carrier.TryCarry(bundle))
            {
                // 못 가져가면 원래 자리에 도로 얹어둔다. 안 그러면 상자가 바닥에 떨어진 채 남는다.
                StackOnCounter(bundle, bundles.Count);
                bundles.Add(bundle);
            }
        }

        // 카운터가 대신 들고 있는 상태로 만들어 물리를 끄고, 쌓인 순서대로 위로 얹는다.
        private void StackOnCounter(IngredientBundle bundle, int index)
        {
            bundle.SetCarried(SlotRoot);
            bundle.transform.localPosition = new Vector3(0f, index * bundleStackSpacing, 0f);
        }

        private void TryGiveDish(ISingleItemCarrier carrier, GameObject interactor)
        {
            if (dishInventory.Stacks.Count == 0)
            {
                return;
            }

            ItemStack stack = dishInventory.Stacks[0];
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
            dishInventory.TryRemove(pickedItem, 1);

            if (pickedItem.Category == ItemCategory.Dish && HallManager.Instance != null)
            {
                HallManager.Instance.TryCollectDish(pickedItem.ItemId, interactor);
            }
        }

        // 손님/홀 플레이어는 여기 맨 앞(가장 먼저 들어온) 요리부터 가져간다. 그 요리를 카운터 위에 띄워 보여준다.
        private void UpdateFirstItemDisplay()
        {
            if (firstItemDisplay == null)
            {
                return;
            }

            ItemBase first = inventory.Stacks.Count > 0 ? inventory.Stacks[0].Item : null;
            firstItemDisplay.sprite = first != null && first.WorldPrefab != null
                ? first.WorldPrefab.GetComponentInChildren<SpriteRenderer>()?.sprite
                : null;
        }
    }
}
