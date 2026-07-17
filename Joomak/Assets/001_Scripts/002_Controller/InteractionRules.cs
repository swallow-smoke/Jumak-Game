using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._003_Object.Interface;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._002_Controller
{
    // 빗자루 규칙을 한 곳에 모아둔다. 각 상호작용 오브젝트마다 흩어놓으면
    // 주방/홀 양쪽 파일을 전부 고쳐야 하고 하나만 빠뜨려도 규칙이 새어나간다.
    public static class InteractionRules
    {
        public static bool IsHoldingBroom(ISingleItemCarrier carrier) =>
            carrier?.HeldItem is WorldItem { Item: { Category: ItemCategory.Tool } };

        // 빗자루를 든 동안에는 빗자루로 해결하는 일만, 맨손일 때는 그 외의 일만 가능하다.
        // 즉 "빗자루가 필요한가"와 "빗자루를 들었는가"가 일치해야 상호작용이 성립한다.
        public static bool CanInteract(ISingleItemCarrier carrier, IInteractable target)
        {
            if (target == null)
            {
                return false;
            }

            bool needsBroom = target is IBroomTarget { RequiresBroom: true };
            return needsBroom == IsHoldingBroom(carrier);
        }

        // 빗자루는 아무 바닥에나 내려놓을 수 있고, 그 자리에 남아 다른 플레이어도 집을 수 있다.
        // 요리·재료는 대상 없이 아무데나 버릴 수 없으므로 빗자루(Tool)만 허용한다.
        public static bool TryDropBroom(ISingleItemCarrier carrier, Vector3 position)
        {
            return IsHoldingBroom(carrier) && carrier.TryDropHeldItem(position);
        }
    }
}
