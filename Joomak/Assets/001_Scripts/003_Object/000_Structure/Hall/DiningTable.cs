using System.Collections.Generic;
using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._003_Object._001_Entity.NPC;
using _001_Scripts._005_Data._000_Item;
using _001_Scripts._004_UI.Components;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Hall
{
    public sealed class DiningTable : BaseStructure
    {
        [SerializeField] private List<Seat> seats = new();
        [SerializeField] private ItemBase plateItem;

        public IReadOnlyList<Seat> Seats => seats;
        public bool HasFreeSeat => TryGetFreeSeat(out _);

        protected override void Awake()
        {
            base.Awake();
            foreach (Seat seat in seats)
            {
                if (seat != null)
                {
                    seat.BindTable(this);
                }
            }
        }

        public override void Interact(GameObject interactor)
        {
            // 손님을 데리고 왔으면 착석이 우선이고, 아니면 빈 그릇을 치운다.
            if (interactor.TryGetComponent(out CustomerEscort escort) &&
                escort.Escorted != null &&
                TryGetFreeSeat(escort.Escorted.transform.position, out Seat freeSeat) &&
                escort.Escorted.TrySit(freeSeat))
            {
                GameplayFeedback.Burst(freeSeat.SitPosition, new Color(0.45f, 0.8f, 1f), "착석", 8);
                return;
            }

            TryGiveDirtyPlate(interactor);
        }

        public bool TryGetFreeSeat(out Seat freeSeat) => TryGetNearestFreeSeat(null, out freeSeat);

        // 손님과 가까운 쪽 좌석을 내준다. 늘 0번 좌석만 주면 반대편에서 온 손님이
        // 굳이 테이블을 빙 둘러 반대편으로 가야 한다.
        public bool TryGetFreeSeat(Vector2 nearTo, out Seat freeSeat) => TryGetNearestFreeSeat(nearTo, out freeSeat);

        private bool TryGetNearestFreeSeat(Vector2? nearTo, out Seat freeSeat)
        {
            freeSeat = null;
            float bestDistance = float.MaxValue;

            foreach (Seat seat in seats)
            {
                if (seat == null || !seat.IsFree)
                {
                    continue;
                }

                if (!nearTo.HasValue)
                {
                    freeSeat = seat;
                    return true;
                }

                float distance = ((Vector2)seat.SitPosition - nearTo.Value).sqrMagnitude;
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                freeSeat = seat;
            }

            return freeSeat != null;
        }

        private void TryGiveDirtyPlate(GameObject interactor)
        {
            if (plateItem == null ||
                !interactor.TryGetComponent(out ISingleItemCarrier carrier) ||
                carrier.HeldItem != null ||
                !TryGetDirtySeat(out Seat dirtySeat) ||
                !WorldItem.TryCreate(plateItem, transform.position, out WorldItem plate))
            {
                return;
            }

            if (carrier.TryCarry(plate))
            {
                dirtySeat.ClearPlate();
                GameplayFeedback.Burst(transform.position, new Color(0.72f, 0.86f, 1f), "정리!", 8);
                return;
            }

            Destroy(plate.gameObject);
        }

        private bool TryGetDirtySeat(out Seat dirtySeat)
        {
            foreach (Seat seat in seats)
            {
                if (seat != null && seat.HasDirtyPlate)
                {
                    dirtySeat = seat;
                    return true;
                }
            }

            dirtySeat = null;
            return false;
        }

        private void OnValidate()
        {
            if (plateItem != null && plateItem.Category != ItemCategory.Plate)
            {
                Debug.LogWarning($"{name}: plateItem은 Plate 카테고리여야 합니다.", this);
            }
        }
    }
}
