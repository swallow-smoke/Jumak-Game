using System;
using _001_Scripts._001_Manager;
using _001_Scripts._003_Object._000_Structure.Hall;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._003_Object.Interface;
using _001_Scripts._005_Data._000_Item;
using _001_Scripts._005_Data.Hall;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.NPC
{
    public sealed class Customer : BaseEntity, IInteractable
    {
        private const int TimeoutPenalty = 5;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 3f;
        [SerializeField, Min(0.01f)] private float arriveThreshold = 0.1f;
        [SerializeField, Min(0.1f)] private float followDistance = 1f;

        private HallManager hall;
        private CustomerPatienceSettings patience;
        private CustomerEscort escort;
        private Seat seat;
        private ItemBase orderedDish;
        private Vector3 waitPosition;
        private Vector3 exitPosition;
        private float stateTimer;
        private float seatTimer;
        private float eatDuration;

        public CustomerState State { get; private set; } = CustomerState.WaitingForSeat;
        public ItemBase OrderedDish => orderedDish;
        public Guid OrderId { get; private set; }
        public string CustomerId => ObjectId.ToString();
        public bool IsWaitingForSeat => State is CustomerState.WaitingForSeat or CustomerState.Following;

        public void Initialize(
            HallManager hallManager,
            CustomerPatienceSettings patienceSettings,
            ItemBase dish,
            Vector3 waitSpot,
            Vector3 exitSpot)
        {
            hall = hallManager;
            patience = patienceSettings;
            orderedDish = dish;
            waitPosition = waitSpot;
            exitPosition = exitSpot;
            name = $"Customer_{dish?.DisplayName ?? "None"}";
            SetState(CustomerState.WaitingForSeat);
        }

        private void Update()
        {
            if (patience == null)
            {
                return;
            }

            stateTimer += Time.deltaTime;

            switch (State)
            {
                case CustomerState.WaitingForSeat:
                    MoveTowards(waitPosition);
                    TickSeatPatience();
                    break;

                case CustomerState.Following:
                    FollowEscort();
                    TickSeatPatience();
                    break;

                case CustomerState.WalkingToSeat:
                    if (MoveTowards(seat.SitPosition))
                    {
                        SetState(CustomerState.Deciding);
                    }

                    break;

                case CustomerState.Deciding:
                    if (stateTimer >= patience.DecideSeconds)
                    {
                        SetState(CustomerState.ReadyToOrder);
                    }

                    break;

                case CustomerState.ReadyToOrder:
                    if (stateTimer >= patience.OrderSeconds)
                    {
                        LeaveAngry("주문을 받지 않음");
                    }

                    break;

                case CustomerState.WaitingForFood:
                    if (stateTimer >= patience.FoodSeconds)
                    {
                        LeaveAngry("음식이 나오지 않음");
                    }

                    break;

                case CustomerState.Eating:
                    if (stateTimer >= eatDuration)
                    {
                        FinishEating();
                    }

                    break;

                case CustomerState.Leaving:
                    if (MoveTowards(exitPosition))
                    {
                        Despawn();
                    }

                    break;
            }
        }

        public void Interact(GameObject interactor)
        {
            switch (State)
            {
                case CustomerState.WaitingForSeat:
                    TryStartFollowing(interactor);
                    break;

                case CustomerState.ReadyToOrder:
                    PlaceOrder(interactor);
                    break;

                case CustomerState.WaitingForFood:
                    TryReceiveDish(interactor);
                    break;
            }
        }

        public bool TrySit(Seat targetSeat)
        {
            if (State != CustomerState.Following || targetSeat == null || !targetSeat.TryReserve(this))
            {
                return false;
            }

            seat = targetSeat;
            ReleaseEscort();
            SetState(CustomerState.WalkingToSeat);
            return true;
        }

        private void TryStartFollowing(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out CustomerEscort interactorEscort) || !interactorEscort.TryEscort(this))
            {
                return;
            }

            escort = interactorEscort;
            SetState(CustomerState.Following);
        }

        private void PlaceOrder(GameObject interactor)
        {
            if (hall == null || orderedDish == null)
            {
                return;
            }

            OrderId = hall.SubmitOrder(this, interactor);
            SetState(CustomerState.WaitingForFood);
        }

        private void TryReceiveDish(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out ISingleItemCarrier carrier) ||
                carrier.HeldItem is not WorldItem heldItem ||
                heldItem.Item != orderedDish ||
                !carrier.TryConsumeHeldItem(heldItem))
            {
                return;
            }

            hall.CompleteOrder(OrderId);
            OrderId = Guid.Empty;
            eatDuration = patience.RandomEatSeconds;
            SetState(CustomerState.Eating);
        }

        private void TickSeatPatience()
        {
            seatTimer += Time.deltaTime;
            if (seatTimer >= patience.SeatSeconds)
            {
                LeaveAngry("자리로 안내하지 않음");
            }
        }

        private void FinishEating()
        {
            // 기획서 4-1 7번: 식사가 끝나면 빈 그릇이 자리에 남고 손님은 퇴장한다.
            if (seat != null)
            {
                seat.MarkDirty();
                seat.Release(this);
                seat = null;
            }

            SetState(CustomerState.Leaving);
        }

        private void LeaveAngry(string reason)
        {
            if (ReputationManager.Instance != null)
            {
                ReputationManager.Instance.Penalize(TimeoutPenalty, $"{name} - {reason}");
            }

            if (OrderId != Guid.Empty)
            {
                hall.CancelOrder(OrderId, reason, gameObject);
                OrderId = Guid.Empty;
            }

            // 먹지 않고 나가므로 빈 그릇은 남지 않는다. 자리는 바로 회수된다.
            if (seat != null)
            {
                seat.Release(this);
                seat = null;
            }

            ReleaseEscort();
            SetState(CustomerState.Leaving);
        }

        private void ReleaseEscort()
        {
            if (escort != null)
            {
                escort.Release(this);
                escort = null;
            }
        }

        private void FollowEscort()
        {
            if (escort == null)
            {
                SetState(CustomerState.WaitingForSeat);
                return;
            }

            Vector3 target = escort.transform.position;
            Vector3 toSelf = transform.position - target;
            MoveTowards(target + toSelf.normalized * followDistance);
        }

        private bool MoveTowards(Vector3 target)
        {
            Vector3 next = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            next.z = transform.position.z;
            transform.position = next;
            return (target - transform.position).sqrMagnitude <= arriveThreshold * arriveThreshold;
        }

        private void SetState(CustomerState next)
        {
            State = next;
            stateTimer = 0f;
        }

        private void Despawn()
        {
            if (hall != null)
            {
                hall.Unregister(this);
            }

            Destroy(gameObject);
        }
    }
}
