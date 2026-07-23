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
using _001_Scripts._005_Data.Config;
using _001_Scripts._004_UI.Components;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.NPC
{
    public sealed class Customer : BaseEntity, IBroomTarget
    {
        private const int TimeoutPenalty = 5;
        private const int UnsatisfiedLeavePenalty = 3;
        private const float FallbackResolveSeconds = 30f;
        private const float FallbackTelegraphSeconds = 3f;
        private const int FallbackDishPrice = 50;

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
        [Tooltip("플레이어보다 가볍게 설정해 플레이어와 충돌하면 밀려납니다.")]
        [SerializeField, Min(0.1f)] private float npcMass = 1f;
        [SerializeField, Min(0f)] private float movementDamping = 3f;

        [Header("Seated Position Recovery")]
        [Tooltip("착석한 손님이 밀려났을 때 좌석으로 돌아오는 최대 속도입니다.")]
        [SerializeField, Min(0.1f)] private float seatReturnSpeed = 4.5f;
        [Tooltip("좌석과의 거리 차이를 복귀 속도로 바꾸는 반응도입니다.")]
        [SerializeField, Min(0.1f)] private float seatReturnResponsiveness = 8f;
        [SerializeField, Min(0.001f)] private float seatSnapDistance = 0.04f;
        [Tooltip("물리 충돌로 테이블 앞에 막혀도 이 거리까지 오면 착석 완료로 처리합니다.")]
        [SerializeField, Min(0.1f)] private float seatArrivalDistance = 0.85f;

        [Header("Obstacle Avoidance")]
        [Tooltip("손님의 몸 반경. 이 반경으로 앞을 훑어 테이블에 닿을지 미리 본다. 손님 스프라이트 반지름에 맞출 것.")]
        [SerializeField, Min(0.05f)] private float avoidRadius = 0.78f;
        [Tooltip("몇 유닛 앞까지 내다볼지. 너무 길면 멀리 있는 테이블을 보고 미리 피해 돌아간다.")]
        [SerializeField, Min(0.1f)] private float avoidLookAhead = 3.1f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color dineAndDashColor = new(0.9f, 0.15f, 0.15f);
        [SerializeField] private Color rowdyColor = new(0.55f, 0.15f, 0.7f);

        [Header("Order Indicator Audio")]
        [SerializeField] private AudioClip questionSfx;
        [SerializeField] private AudioClip exclamationSfx;

        private Color defaultColor;
        private Rigidbody2D body;
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
        private bool isRowdy;
        private CustomerOrderIndicator orderIndicator;
        private WorldProgressBar eatingProgress;

        public CustomerState State { get; private set; } = CustomerState.WaitingForSeat;
        public ItemBase OrderedDish => orderedDish;
        public bool HasPaid { get; private set; }
        public Guid OrderId { get; private set; }
        public string CustomerId => ObjectId.ToString();
        public float Satisfaction => satisfaction;

        // 손놈은 식사를 마친 뒤에야 정체가 드러나므로, 대기 줄에서는 평범한 손님과 똑같이 자리를 차지한다.
        public bool OccupiesWaitingSpot =>
            State is CustomerState.WaitingForSeat or CustomerState.Following;

        // 빗자루가 필요한 건 난동 중이거나 도주 중일 때뿐. 평소엔 맨손으로 접객한다.
        public bool RequiresBroom => State is CustomerState.Rowdy or CustomerState.DineAndDash;

        // 남은 연타 횟수. 손놈 5회 / 먹튀 1회 (기획서 8번)
        public int RemainingHits => remainingHits;

        // 만족도가 실제로 깎이고 확인되는 구간. 식사가 시작되면(대접받은 순간 평판 계산이 끝났으므로) 더 이상 중요하지 않다.
        private bool IsSatisfactionActive =>
            State is CustomerState.WaitingForSeat or CustomerState.Following or CustomerState.WalkingToSeat
                or CustomerState.Deciding or CustomerState.ReadyToOrder or CustomerState.WaitingForFood;

        private bool IsSeated =>
            seat != null && State is (CustomerState.Deciding or CustomerState.ReadyToOrder
                or CustomerState.WaitingForFood or CustomerState.Eating);

        private static HallEventSettings EventSettings =>
            EventManager.Instance != null ? EventManager.Instance.Settings : null;

        protected override void Awake()
        {
            base.Awake();
            ApplyGameBalance();

            body = GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody2D>();
            }

            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.mass = npcMass;
            body.linearDamping = movementDamping;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            IgnoreOtherCustomerCollisions();

            AudioClip defaultInteraction = Resources.Load<AudioClip>("006_Audio/interactionSound");
            questionSfx ??= defaultInteraction;
            exclamationSfx ??= defaultInteraction;

            orderIndicator = GetComponent<CustomerOrderIndicator>();
            if (orderIndicator == null)
            {
                orderIndicator = gameObject.AddComponent<CustomerOrderIndicator>();
            }

            orderIndicator.ConfigureAudio(questionSfx, exclamationSfx);

            eatingProgress = GetComponent<WorldProgressBar>();
            if (eatingProgress == null)
            {
                eatingProgress = gameObject.AddComponent<WorldProgressBar>();
            }

            eatingProgress.Configure(new Vector3(0f, 1.45f, -0.25f), 0.012f,
                new Color(0.96f, 0.62f, 0.18f, 1f));
            eatingProgress.SetProgress(0f, "식사 중 0%", false);

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

            // 손놈은 스폰 때 정체를 숨긴다. 평범한 손님처럼 착석·식사하고, 식사가 끝날 때 드러난다.
            isRowdy = startsRowdy;

            SetState(CustomerState.WaitingForSeat);
        }

        private void Update()
        {
            if (patience == null)
            {
                return;
            }

            bool tutorialProtected = TutorialOverlay.IsRunning;
            bool freezeStateTimer = tutorialProtected &&
                                    State is CustomerState.Rowdy or CustomerState.DineAndDash or CustomerState.Leaving;
            if (!freezeStateTimer)
            {
                stateTimer += Time.deltaTime;
            }
            RefreshEatingProgress();

            if (IsSatisfactionActive && !tutorialProtected)
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
                    if (MoveTowardsSeat())
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
                    if (tutorialProtected)
                    {
                        break;
                    }

                    // 정체가 드러난 손놈이 입구로 난동을 부리며 빠져나간다.
                    // 제한시간 안에 빗자루로 제압하지 못하면 그대로 도망친다.
                    if (MoveTowards(exitPosition) || stateTimer >= ResolveSeconds())
                    {
                        Penalize("손놈이 제압당하지 않고 도망감");
                        SetState(CustomerState.Leaving);
                    }

                    break;

                case CustomerState.DineAndDash:
                    if (tutorialProtected)
                    {
                        break;
                    }

                    // 빨갛게 변한 채로 잠깐 멈춰 있다가 튄다. 이 틈에 플레이어가 빗자루를 챙길 수 있다.
                    if (stateTimer < TelegraphSeconds())
                    {
                        break;
                    }

                    // 계산 없이 입구로 직행. 나가기 전에 잡아야 한다.
                    if (MoveTowards(exitPosition))
                    {
                        Penalize("먹튀 손님을 놓침");
                        Despawn();
                    }

                    break;

                case CustomerState.Leaving:
                    if (tutorialProtected)
                    {
                        body.linearVelocity = Vector2.zero;
                        break;
                    }

                    if (MoveTowards(exitPosition))
                    {
                        Despawn();
                    }

                    break;
            }
        }

        private void ApplyGameBalance()
        {
            GameBalance.EnsureLoaded();
            CustomerBalance settings = GameBalance.Current.customer;
            moveSpeed = Mathf.Max(0f, settings.moveSpeed);
            arriveThreshold = Mathf.Max(0.01f, settings.arriveThreshold);
            followDistance = Mathf.Max(0.1f, settings.followDistance);
            npcMass = Mathf.Max(0.1f, settings.mass);
            movementDamping = Mathf.Max(0f, settings.movementDamping);
            seatReturnSpeed = Mathf.Max(0.1f, settings.seatReturnSpeed);
            seatReturnResponsiveness = Mathf.Max(0.1f, settings.seatReturnResponsiveness);
            seatSnapDistance = Mathf.Max(0.001f, settings.seatSnapDistance);
            seatArrivalDistance = Mathf.Max(0.1f, settings.seatArrivalDistance);
            avoidRadius = Mathf.Max(0.05f, settings.avoidRadius);
            avoidLookAhead = Mathf.Max(0.1f, settings.avoidLookAhead);
        }

        private void FixedUpdate()
        {
            if (!IsSeated || body == null)
            {
                return;
            }

            Vector2 seatPosition = seat.SitPosition;
            Vector2 offset = seatPosition - body.position;
            if (offset.sqrMagnitude <= seatSnapDistance * seatSnapDistance)
            {
                body.linearVelocity = Vector2.zero;
                body.MovePosition(seatPosition);
                return;
            }

            // 플레이어에게 밀리는 반응은 남기되, 자리를 점유하는 동안에는 좌석을 기준점으로 되돌아간다.
            body.linearVelocity = Vector2.ClampMagnitude(offset * seatReturnResponsiveness, seatReturnSpeed);
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
            GameplayFeedback.Burst(transform.position + Vector3.up * 0.7f,
                new Color(1f, 0.35f, 0.16f), remainingHits > 0 ? $"남은 타격 {remainingHits}" : "제압!", 10);
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
            TutorialProgress.Report(TutorialAction.CustomerSeated);
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
            TutorialProgress.Report(TutorialAction.CustomerEscorted);
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
            TutorialProgress.Report(TutorialAction.OrderTaken);
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
            TutorialProgress.Report(TutorialAction.DishServed);
            GameplayFeedback.Burst(transform.position + Vector3.up * 0.6f,
                new Color(1f, 0.68f, 0.2f), "서빙!", 12);
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
            // 실습이 끝날 때까지는 식사를 마친 손님도 자리를 지키게 한다.
            if (TutorialOverlay.IsRunning)
            {
                return;
            }

            // 기획서 4-1 7번: 식사가 끝나면 빈 그릇이 자리에 남고 손님은 퇴장한다.
            if (seat != null)
            {
                seat.MarkDirty();
                seat.Release(this);
                seat = null;
            }

            // 손놈은 식사를 마친 지금 정체를 드러내고 난동을 부린다.
            // 난동을 부리기 전에 이미 값은 치렀으므로 계산은 정상으로 한다.
            if (isRowdy)
            {
                Pay();
                remainingHits = EventSettings?.RowdyHits ?? 5;
                name = "손놈";
                SetVisualColor(rowdyColor);
                SetState(CustomerState.Rowdy);
                return;
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
            int payment = orderedDish.Price > 0 ? orderedDish.Price : FallbackDishPrice;
            GameManager.Instance.UpdateMoney(payment, $"{orderedDish.DisplayName} 판매");
            GameplayFeedback.Burst(transform.position + Vector3.up * 0.6f,
                new Color(1f, 0.82f, 0.2f), $"+{payment}전", 14);
            if (UpgradeApi.SaleReputationBonus > 0 &&
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

        private void IgnoreOtherCustomerCollisions()
        {
            Collider2D[] ownColliders = GetComponentsInChildren<Collider2D>(true);
            Customer[] existingCustomers = FindObjectsByType<Customer>(FindObjectsInactive.Exclude);
            foreach (Customer other in existingCustomers)
            {
                if (other == null || other == this)
                {
                    continue;
                }

                Collider2D[] otherColliders = other.GetComponentsInChildren<Collider2D>(true);
                foreach (Collider2D ownCollider in ownColliders)
                {
                    foreach (Collider2D otherCollider in otherColliders)
                    {
                        if (ownCollider != null && otherCollider != null)
                        {
                            Physics2D.IgnoreCollision(ownCollider, otherCollider, true);
                        }
                    }
                }
            }
        }

        private bool MoveTowardsSeat()
        {
            if (seat == null)
            {
                return false;
            }

            Vector2 seatPosition = seat.SitPosition;
            Vector2 current = body != null ? body.position : (Vector2)transform.position;
            if ((seatPosition - current).sqrMagnitude > seatArrivalDistance * seatArrivalDistance)
            {
                return MoveTowards(seat.SitPosition);
            }

            // 동적 Rigidbody는 테이블 충돌체에 먼저 닿을 수 있으므로 근처까지 오면 좌석 중심으로 확정한다.
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.position = seatPosition;
            }
            else
            {
                transform.position = new Vector3(seatPosition.x, seatPosition.y, transform.position.z);
            }

            return true;
        }

        private bool MoveTowards(Vector3 target)
        {
            Vector2 current = body != null ? body.position : (Vector2)transform.position;
            Vector2 toTarget = (Vector2)target - current;
            float distance = toTarget.magnitude;

            if (distance > arriveThreshold)
            {
                Vector2 direction = SteerAroundStructures(current, toTarget / distance, distance);
                if (body != null)
                {
                    // 속도로 이동해야 플레이어와의 질량 차이와 충돌 반응이 실제 물리 계산에 반영된다.
                    body.linearVelocity = direction * moveSpeed;
                }
                else
                {
                    Vector2 step = direction * (moveSpeed * Time.deltaTime);
                    if (step.sqrMagnitude > distance * distance)
                    {
                        step = direction * distance;
                    }

                    Vector2 next = current + step;
                    transform.position = new Vector3(next.x, next.y, transform.position.z);
                }

                return false;
            }

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            return true;
        }

        // 진행 방향을 미리 훑어 테이블/카운터에 부딪힐 것 같으면 표면을 따라 미끄러진다.
        // 손님끼리는 충돌을 무시하므로 구조물만 회피한다. 좁은 퇴장로에서 상호 회피 교착이 생기지 않는다.
        private Vector2 SteerAroundStructures(Vector2 origin, Vector2 direction, float distanceToTarget)
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

                bool blocksMovement = hit.collider.GetComponentInParent<BaseStructure>() != null;

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
            orderIndicator?.SetCustomerState(next);
            RefreshEatingProgress();
        }

        private void RefreshEatingProgress()
        {
            if (eatingProgress == null)
            {
                return;
            }

            bool isEating = State == CustomerState.Eating;
            float normalized = isEating && eatDuration > 0f ? Mathf.Clamp01(stateTimer / eatDuration) : 0f;
            eatingProgress.SetProgress(normalized, $"식사 중 {Mathf.RoundToInt(normalized * 100f)}%", isEating);
        }

        private void Despawn()
        {
            if (TutorialOverlay.IsRunning)
            {
                return;
            }

            if (hall != null)
            {
                hall.Unregister(this);
            }

            Destroy(gameObject);
        }
    }
}
