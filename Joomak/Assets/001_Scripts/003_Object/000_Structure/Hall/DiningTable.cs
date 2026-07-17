using System.Collections.Generic;
using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._003_Object._001_Entity.NPC;
using _001_Scripts._005_Data._000_Item;
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
                TryGetFreeSeat(out Seat freeSeat) &&
                escort.Escorted.TrySit(freeSeat))
            {
                return;
            }

            TryGiveDirtyPlate(interactor);
        }

        public bool TryGetFreeSeat(out Seat freeSeat)
        {
            foreach (Seat seat in seats)
            {
                if (seat != null && seat.IsFree)
                {
                    freeSeat = seat;
                    return true;
                }
            }

            freeSeat = null;
            return false;
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
