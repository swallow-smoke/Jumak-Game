using System;
using _001_Scripts._001_Manager;
using _001_Scripts._003_Object._000_Structure.Hall;
using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._003_Object.Interface;
using _001_Scripts._005_Data._000_Item;
using _001_Scripts._005_Data.Hall;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.NPC
{
    public sealed class Customer : BaseEntity, IBroomTarget
    {
        private const int TimeoutPenalty = 5;
        private const int UnsatisfiedLeavePenalty = 3;
        private const float FallbackResolveSeconds = 30f;
        private const float FallbackTelegraphSeconds = 3f;

        private static readonly RaycastHit2D[] AvoidHits = new RaycastHit2D[8];

        // 트리거(손님·바닥 아이템)까지 다 받아서 BaseStructure만 직접 걸러낸다.
        // 전용 레이어를 새로 파면 ProjectSettings가 바뀌어 팀원과 충돌한다.
        private static readonly ContactFilter2D NoFilter = new()
        {
            useTriggers = true,
            useLayerMask = false,
            useDepth = false
        };

        // 아래 거리 값들은 카메라 orthographicSize 12(월드 높이 24, 타일 1.53유닛) 기준.
        // 맵 크기가 또 바뀌면 같은 비율로 따라가야 한다.
        [Header("Movement")]
        [Tooltip("반드시 플레이어 이동속도보다 느려야 한다. 빠르면 먹튀 손님을 쫓아가 잡을 수 없다.\n" +
                 "월드가 커졌지만 플레이어가 5로 남아 있어 이 값도 그대로 둔다. 플레이어를 올릴 때 같은 비율로 올릴 것.")]
        [SerializeField, Min(0f)] private float moveSpeed = 2.4f;
        [SerializeField, Min(0.01f)] private float arriveThreshold = 0.22f;
        [SerializeField, Min(0.1f)] private float followDistance = 2.2f;

        [Header("Obstacle Avoidance")]
        [Tooltip("손님의 몸 반경. 이 반경으로 앞을 훑어 테이블에 닿을지 미리 본다. 손님 스프라이트 반지름에 맞출 것.")]
        [SerializeField, Min(0.05f)] private float avoidRadius = 0.78f;
        [Tooltip("몇 유닛 앞까지 내다볼지. 너무 길면 멀리 있는 테이블을 보고 미리 피해 돌아간다.")]
        [SerializeField, Min(0.1f)] private float avoidLookAhead = 3.1f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color dineAndDashColor = new(0.9f, 0.15f, 0.15f);

        private Color defaultColor;
        private HallManager hall;
        private CustomerPatienceSettings patience;
        private CustomerEscort escort;
        private Seat seat;
        private ItemBase orderedDish;
        private Vector3 waitPosition;
        private Vector3 exitPosition;
        private float stateTimer;
        private float decideDurationSeconds;
        private float eatDuration;
        private float satisfaction;
        private float satisfactionDecayPerSecond;
        private int remainingHits;

        public CustomerState State { get; private set; } = CustomerState.WaitingForSeat;
        public ItemBase OrderedDish => orderedDish;
        public bool HasPaid { get; private set; }
        public Guid OrderId { get; private set; }
        public string CustomerId => ObjectId.ToString();
        public float Satisfaction => satisfaction;

        // 손놈은 자리에 앉지 않지만 입구 자리를 차지하고 난동을 부린다.
        public bool OccupiesWaitingSpot =>
            State is CustomerState.WaitingForSeat or CustomerState.Following or CustomerState.Rowdy;

        // 빗자루가 필요한 건 난동 중이거나 도주 중일 때뿐. 평소엔 맨손으로 접객한다.
        public bool RequiresBroom => State is CustomerState.Rowdy or CustomerState.DineAndDash;

        // 남은 연타 횟수. 손놈 5회 / 먹튀 1회 (기획서 8번)
        public int RemainingHits => remainingHits;

        // 만족도가 실제로 깎이고 확인되는 구간. 식사가 시작되면(대접받은 순간 평판 계산이 끝났으므로) 더 이상 중요하지 않다.
        private bool IsSatisfactionActive =>
            State is CustomerState.WaitingForSeat or CustomerState.Following or CustomerState.WalkingToSeat
                or CustomerState.Deciding or CustomerState.ReadyToOrder or CustomerState.WaitingForFood;

        private static HallEventSettings EventSettings =>
            EventManager.Instance != null ? EventManager.Instance.Settings : null;

        protected override void Awake()
        {
            base.Awake();

            // 인스펙터에서 안 물려줬으면 Visual 자식을 찾는다.
            // 하이라이트 외곽선은 나중에 붙으므로 이 시점엔 본체 스프라이트만 있다.
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            if (visual != null)
            {
                defaultColor = visual.color;
            }
        }

        private void SetVisualColor(Color color)
        {
            if (visual != null)
            {
                visual.color = color;
            }
        }

        public void Initialize(
            HallManager hallManager,
            CustomerPatienceSettings patienceSettings,
            Vector3 waitSpot,
            Vector3 exitSpot,
            bool startsRowdy = false)
        {
            hall = hallManager;
            patience = patienceSettings;
            waitPosition = waitSpot;
            exitPosition = exitSpot;
            satisfaction = patience.StartingSatisfaction;
            satisfactionDecayPerSecond = patience.RandomSatisfactionDecayPerSecond;
            name = "Customer";

            if (startsRowdy)
            {
                remainingHits = EventSettings?.RowdyHits ?? 5;
                name = "손놈";
                SetState(CustomerState.Rowdy);
                return;
            }

            SetState(CustomerState.WaitingForSeat);
        }

        private void Update()
        {
            if (patience == null)
            {
                return;
            }

            stateTimer += Time.deltaTime;

            if (IsSatisfactionActive)
            {
                satisfaction = Mathf.Max(0f, satisfaction - satisfactionDecayPerSecond * Time.deltaTime);
                if (satisfaction <= 0f)
                {
                    LeaveAngry("만족도가 바닥나 자동 퇴장", UnsatisfiedLeavePenalty);
                    return;
                }
            }

            switch (State)
            {
                case CustomerState.WaitingForSeat:
                    MoveTowards(waitPosition);
                    break;

                case CustomerState.Following:
                    FollowEscort();
                    break;

                case CustomerState.WalkingToSeat:
                    if (MoveTowards(seat.SitPosition))
                    {
                        decideDurationSeconds = patience.RandomDecideSeconds;
                        SetState(CustomerState.Deciding);
                    }

                    break;

                case CustomerState.Deciding:
                    if (stateTimer >= decideDurationSeconds)
                    {
                        DecideOrder();
                    }

                    break;

                case CustomerState.ReadyToOrder:
                    // 플레이어가 와서 상호작용해줄 때까지 기다린다. 너무 오래 걸리면 위의 만족도가 0이 되어 알아서 나간다.
                    break;

                case CustomerState.WaitingForFood:
                    // ReadyToOrder와 마찬가지로 만족도가 실질적인 제한시간 역할을 한다.
                    break;

                case CustomerState.Eating:
                    if (stateTimer >= eatDuration)
                    {
                        FinishEating();
                    }

                    break;

                case CustomerState.Rowdy:
                    // 입구 근처에서 난동. 제한시간 안에 제압하지 못하면 도망친다.
                    MoveTowards(waitPosition);
                    if (stateTimer >= ResolveSeconds())
                    {
                        Penalize("손놈이 제압당하지 않고 도망감");
                        SetState(CustomerState.Leaving);
                    }

                    break;

                case CustomerState.DineAndDash:
                    // 빨갛게 변한 채로 잠깐 멈춰 있다가 튄다. 이 틈에 플레이어가 빗자루를 챙길 수 있다.
                    if (stateTimer < TelegraphSeconds())
                    {
                        break;
                    }

                    // 계산 없이 입구로 직행. 나가기 전에 잡아야 한다.
                    if (MoveTowards(exitPosition, avoidCustomers: true))
                    {
                        Penalize("먹튀 손님을 놓침");
                        Despawn();
                    }

                    break;

                case CustomerState.Leaving:
                    if (MoveTowards(exitPosition, avoidCustomers: true))
                    {
                        Despawn();
                    }

                    break;
            }
        }

        private static float ResolveSeconds() => EventSettings?.ResolveSeconds ?? FallbackResolveSeconds;

        private static float TelegraphSeconds() =>
            EventSettings?.DineAndDashTelegraphSeconds ?? FallbackTelegraphSeconds;

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

                // 여기 오려면 빗자루를 들고 있어야 한다. InteractionRules가 이미 걸러준다.
                case CustomerState.Rowdy:
                case CustomerState.DineAndDash:
                    HitWithBroom();
                    break;
            }
        }

        private void HitWithBroom()
        {
            remainingHits--;
            if (remainingHits > 0)
            {
                return;
            }

            // 먹튀는 잡히면 결국 계산하고 나간다. 손놈은 제압당해 그냥 쫓겨난다.
            if (State == CustomerState.DineAndDash)
            {
                Pay();
                SetVisualColor(defaultColor);
            }

            SetState(CustomerState.Leaving);
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

        // 착석 후 얼마간 고민하다가 실제로 주문할지 말지를 정한다.
        // 손님 여럿이 한 테이블에 앉아도 전부 다 주문하는 건 아니다 - 각자 따로 굴린다.
        private void DecideOrder()
        {
            if (UnityEngine.Random.value <= patience.OrderChance && hall != null && hall.TryGetRandomMenuItem(out ItemBase dish))
            {
                orderedDish = dish;
                name = $"Customer_{dish.DisplayName}";
                SetState(CustomerState.ReadyToOrder);
                return;
            }

            // 주문 없이 조용히 나간다. 손님 잘못이 아니므로 페널티는 없다.
            if (seat != null)
            {
                seat.Release(this);
                seat = null;
            }

            SetState(CustomerState.Leaving);
        }

        private void PlaceOrder(GameObject interactor)
        {
            if (hall == null || orderedDish == null)
            {
                return;
            }

            OrderId = hall.CreateOrder(this, interactor);
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

            hall.UpdateOrderStatus(OrderId, HallOrderStatus.Served);
            OrderId = Guid.Empty;
            ApplySatisfactionReputation();
            eatDuration = patience.RandomEatSeconds;
            SetState(CustomerState.Eating);
        }

        // 대접받는 순간의 만족도에 따라 명성이 오르내린다.
        private void ApplySatisfactionReputation()
        {
            if (ReputationManager.Instance == null)
            {
                return;
            }

            if (satisfaction >= 90f)
            {
                ReputationManager.Instance.Restore(4);
            }
            else if (satisfaction >= 50f)
            {
                ReputationManager.Instance.Restore(2);
            }
            else if (satisfaction >= 30f)
            {
                ReputationManager.Instance.Restore(1);
            }
            else if (satisfaction >= 10f)
            {
                ReputationManager.Instance.Penalize(1, $"{name} - 만족도 낮은 상태로 서빙 (만족도 {satisfaction:F0})");
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

            // 기획서 8번 먹튀: 일부 손님은 계산하지 않고 입구로 튄다.
            HallEventSettings events = EventSettings;
            if (events != null && events.RollDineAndDash())
            {
                remainingHits = events.DineAndDashHits;
                name = $"먹튀_{orderedDish?.DisplayName ?? "None"}";
                SetVisualColor(dineAndDashColor);
                SetState(CustomerState.DineAndDash);
                return;
            }

            // 기획서 6번: 전은 식사를 마친 시점에 계산한다.
            // (서빙 시점에 받으면 먹튀가 성립하지 않는다 - 이미 낸 돈을 떼먹을 수 없으므로)
            Pay();
            SetState(CustomerState.Leaving);
        }

        // 먹튀 이벤트가 붙으면 이 호출만 건너뛰고, 빗자루로 잡았을 때 다시 부르면 된다.
        public bool Pay()
        {
            if (HasPaid || orderedDish == null || GameManager.Instance == null)
            {
                return false;
            }

            HasPaid = true;
            GameManager.Instance.UpdateMoney(orderedDish.Price, $"{orderedDish.DisplayName} 판매");
            if (RunState.Instance != null &&
                RunState.Instance.GetLevel(UpgradeId.PremiumDish) > 0 &&
                ReputationManager.Instance != null)
            {
                ReputationManager.Instance.Restore(1);
            }

            return true;
        }

        private void Penalize(string reason) => Penalize(TimeoutPenalty, reason);

        private void Penalize(int amount, string reason)
        {
            if (ReputationManager.Instance != null)
            {
                ReputationManager.Instance.Penalize(amount, $"{name} - {reason}");
            }
        }

        private void LeaveAngry(string reason, int penalty = TimeoutPenalty)
        {
            Penalize(penalty, reason);

            if (OrderId != Guid.Empty)
            {
                hall.DeleteOrder(OrderId, reason, gameObject);
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

        private bool MoveTowards(Vector3 target, bool avoidCustomers = false)
        {
            Vector2 current = transform.position;
            Vector2 toTarget = (Vector2)target - current;
            float distance = toTarget.magnitude;

            if (distance > arriveThreshold)
            {
                Vector2 direction = SteerAroundStructures(current, toTarget / distance, distance, avoidCustomers);
                Vector2 step = direction * (moveSpeed * Time.deltaTime);
                if (step.sqrMagnitude > distance * distance)
                {
                    step = direction * distance;
                }

                Vector2 next = current + step;
                transform.position = new Vector3(next.x, next.y, transform.position.z);
            }

            return ((Vector2)target - (Vector2)transform.position).sqrMagnitude <= arriveThreshold * arriveThreshold;
        }

        // 손님은 물리로 움직이지 않으므로(transform 직접 이동) 콜라이더가 막아주지 않는다.
        // 그래서 진행 방향을 미리 훑어 테이블/카운터에 부딪힐 것 같으면 표면을 따라 미끄러진다.
        // avoidCustomers가 true면(퇴장/먹튀 도주 중) 다른 손님도 장애물로 취급해 서로 부딪히지 않는다.
        private Vector2 SteerAroundStructures(Vector2 origin, Vector2 direction, float distanceToTarget, bool avoidCustomers)
        {
            // 목표 지점까지만 본다. 더 멀리 보면 좌석 뒤에 있는 테이블을 피하려다 좌석에 못 앉는다.
            float lookAhead = Mathf.Min(avoidLookAhead, distanceToTarget);
            int hitCount = Physics2D.CircleCast(origin, avoidRadius, direction, NoFilter, AvoidHits, lookAhead);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = AvoidHits[i];
                if (hit.collider == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                bool blocksMovement = hit.collider.GetComponentInParent<BaseStructure>() != null ||
                                      (avoidCustomers && hit.collider.GetComponentInParent<Customer>() != null);

                if (!blocksMovement)
                {
                    continue;
                }

                // 부딪힐 면을 따라 옆으로 흘려보낸다. 원래 가려던 쪽에 가까운 방향을 고른다.
                Vector2 slide = Vector2.Perpendicular(hit.normal);
                if (Vector2.Dot(slide, direction) < 0f)
                {
                    slide = -slide;
                }

                return slide.normalized;
            }

            return direction;
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
