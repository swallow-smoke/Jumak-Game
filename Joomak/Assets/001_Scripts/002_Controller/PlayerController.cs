using System.Collections.Generic;
using _001_Scripts._001_Manager;
using _001_Scripts._002_Controller.Interface;
using _001_Scripts._003_Object.Interface;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._005_Data.Upgrade;
using _001_Scripts._005_Data.Config;
using _001_Scripts._004_UI.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _001_Scripts._002_Controller
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SingleItemCarrier))]
    public sealed class PlayerController : MonoBehaviour, IController
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float rotationLerpSpeed = 10f;
        [Tooltip("손님보다 무겁게 설정해 부딪혔을 때 손님을 밀어낼 수 있게 합니다.")]
        [SerializeField, Min(1f)] private float playerMass = 8f;

        [Header("Dash Upgrade")]
        [SerializeField, Min(1f)] private float dashSpeedMultiplier = 3f;
        [SerializeField, Min(0.05f)] private float dashDurationSeconds = 0.18f;
        [SerializeField, Min(0f)] private float dashCooldownSeconds = 5f;

        [Header("Key Map")]
        [SerializeField] private Key moveUpKey = Key.W;
        [SerializeField] private Key moveDownKey = Key.S;
        [SerializeField] private Key moveLeftKey = Key.A;
        [SerializeField] private Key moveRightKey = Key.D;
        [SerializeField] private Key interactKey = Key.Space;
        [SerializeField] private Key scrollUpKey = Key.UpArrow;
        [SerializeField] private Key scrollDownKey = Key.DownArrow;
        private Key dashKey = Key.LeftShift;
        private Key dropKey = Key.Q;
        private float heldItemDropDistance = 1f;

        [Header("Interaction")]
        [SerializeField, Min(0f)] private float interactionRadius = 1.2f;
        [Tooltip("얇은 선 대신 이 반경으로 바라보는 대상을 찾습니다. 움직이는 손님을 조준하기 쉽게 합니다.")]
        [SerializeField, Min(0.01f)] private float interactionProbeRadius = 0.42f;
        [SerializeField, Min(0f)] private float interactionReachPadding = 0.65f;
        [SerializeField] private LayerMask interactionLayer = ~0;

        [Header("Broom Swing")]
        [Tooltip("빗자루를 들고 있을 때 상호작용 키를 누르면 정밀 조준 대신 이 반경의 부채꼴로 휘두른다.")]
        [SerializeField, Min(0f)] private float broomSwingRadius = 2.6f;
        [Tooltip("바라보는 방향 기준 좌우로 이 각도 안에 있으면 맞는다.")]
        [SerializeField, Range(0f, 180f)] private float broomSwingHalfAngle = 80f;
        [SerializeField] private AudioClip broomSwingSfx;

        private readonly RaycastHit2D[] interactionHits = new RaycastHit2D[24];
        private readonly Collider2D[] broomSwingHits = new Collider2D[32];
        private readonly HashSet<IBroomTarget> broomTargetsHit = new();
        private Rigidbody2D body;
        private ISingleItemCarrier carrier;
        private Vector2 moveInput;
        private Vector2 lookDirection = Vector2.down;
        private ContactFilter2D interactionFilter;
        private ContactFilter2D broomFilter;
        private IInteractable focusedInteractable;
        private InteractionOutline2D focusedOutline;
        private InteractionPromptBubble focusedPrompt;
        private ISelectionInputCapture capturedSelection;
        private float upgradeMoveMultiplier = 1f;
        private Vector2 dashDirection;
        private float dashRemaining;
        private float dashCooldownRemaining;
        private static int selectionCaptureCount;
        private static int selectionEscapeConsumedFrame = -1;
        private PlayerControlProfile controlProfile;

        public static bool IsAnySelectionInputCaptured => selectionCaptureCount > 0;
        public static bool ShouldBlockPauseMenu =>
            IsAnySelectionInputCaptured || selectionEscapeConsumedFrame == Time.frameCount;

        public PlayerControlProfile ControlProfile => controlProfile;
        public string InteractKeyLabel => PlayerControlBindings.GetKeyLabel(interactKey);

        private Key DashKey => dashKey;

        private void Awake()
        {
            ApplyGameBalance();

            // 저장된 키를 적용하기 전에 씬에 직렬화된 기본 이동키로 플레이어 역할을 판별한다.
            controlProfile = moveUpKey == Key.UpArrow && moveLeftKey == Key.LeftArrow
                ? PlayerControlProfile.Kitchen
                : PlayerControlProfile.Hall;
            ApplyControlBindings();

            body = GetComponent<Rigidbody2D>();
            carrier = GetComponent<ISingleItemCarrier>();
            broomSwingSfx ??= Resources.Load<AudioClip>("006_Audio/broom_stick");
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = playerMass;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            interactionFilter = new ContactFilter2D();
            interactionFilter.SetLayerMask(interactionLayer);
            interactionFilter.useTriggers = true;

            // 빗자루 대상은 손님/쓰레기의 레이어 설정 실수와 무관하게 잡혀야 한다.
            broomFilter = new ContactFilter2D
            {
                useTriggers = true,
                useLayerMask = false,
                useDepth = false
            };
        }

        private void OnEnable()
        {
            UpgradeApi.UpgradePurchased += OnUpgradePurchased;
            PlayerControlBindings.Changed += OnControlBindingsChanged;

            RefreshCommonUpgrades();
        }

        private void Update()
        {
            if (PauseSettingsMenu.IsPaused || TutorialOverlay.ShouldBlockGameplayInput ||
                ReputationDeathEnding.IsActive || CheatConsole.IsOpen)
            {
                moveInput = Vector2.zero;
                SetFocusedObject(null);
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                moveInput = Vector2.zero;
                return;
            }

            if (HandleSelectionInput(keyboard))
            {
                return;
            }

            moveInput = ReadMoveInput(keyboard);
            if (moveInput.sqrMagnitude > 0f)
            {
                lookDirection = moveInput.normalized;
                TutorialProgress.Report(controlProfile == PlayerControlProfile.Hall
                    ? TutorialAction.HallMoved
                    : TutorialAction.KitchenMoved);
            }

            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - Time.deltaTime);
            if (keyboard[DashKey].wasPressedThisFrame)
            {
                TryStartDash();
            }

            UpdateFocusedObject();

            if (keyboard[dropKey].wasPressedThisFrame)
            {
                TryDropHeldItem();
            }

            if (keyboard[interactKey].wasPressedThisFrame)
            {
                TryInteract();
                TryCaptureSelectionInput();
            }

            if (focusedInteractable is IScrollSelectable scrollSelectable &&
                focusedInteractable is not ISelectionInputCapture)
            {
                if (keyboard[scrollUpKey].wasPressedThisFrame)
                {
                    scrollSelectable.Scroll(1);
                }
                else if (keyboard[scrollDownKey].wasPressedThisFrame)
                {
                    scrollSelectable.Scroll(-1);
                }
            }
        }

        private void TryInteract()
        {
            // 빗자루를 든 동안에는 상호작용 키를 언제나 휘두르기로 소비한다.
            // 빗나갔다고 같은 입력으로 빗자루를 내려놓으면 연속 타격이 불가능해진다.
            if (InteractionRules.IsHoldingBroom(carrier))
            {
                TrySwingBroom();
                return;
            }

            // 포커스한 대상이 있어도 실제로 상호작용이 안 먹히면(예: 빗자루 든 채 일반 구조물을 봄)
            // 그냥 멈추지 않고 들고 있는 걸 내려놓는 쪽으로 넘어간다.
            if (focusedInteractable != null && InteractionRules.CanInteract(carrier, focusedInteractable))
            {
                focusedInteractable.Interact(gameObject);
                return;
            }

            TryDropHeldItem();
        }

        private bool TryDropHeldItem()
        {
            return InteractionRules.TryDropHeldItem(carrier,
                body.position + lookDirection * Mathf.Max(0.2f, heldItemDropDistance));
        }

        private bool HandleSelectionInput(Keyboard keyboard)
        {
            if (capturedSelection == null || !capturedSelection.IsSelectionActive)
            {
                ReleaseSelectionInput();
                return false;
            }

            moveInput = Vector2.zero;
            dashRemaining = 0f;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                capturedSelection.CancelSelection(gameObject);
                selectionEscapeConsumedFrame = Time.frameCount;
                ReleaseSelectionInput();
                return true;
            }

            if (keyboard[scrollUpKey].wasPressedThisFrame)
            {
                capturedSelection.Scroll(1);
            }
            else if (keyboard[scrollDownKey].wasPressedThisFrame)
            {
                capturedSelection.Scroll(-1);
            }

            if (keyboard[interactKey].wasPressedThisFrame)
            {
                capturedSelection.ConfirmSelection(gameObject);
                ReleaseSelectionInput();
            }

            return true;
        }

        private void TryCaptureSelectionInput()
        {
            if (focusedInteractable is ISelectionInputCapture selection &&
                selection.IsSelectionActive &&
                selection.CanControlSelection(gameObject))
            {
                capturedSelection = selection;
                selectionCaptureCount++;
                moveInput = Vector2.zero;
                dashRemaining = 0f;
            }
        }

        private void ReleaseSelectionInput()
        {
            if (capturedSelection == null)
            {
                return;
            }

            capturedSelection = null;
            selectionCaptureCount = Mathf.Max(0, selectionCaptureCount - 1);
            moveInput = Vector2.zero;
        }

        private bool TrySwingBroom()
        {
            int hitCount = Physics2D.OverlapCircle(body.position, broomSwingRadius, broomFilter, broomSwingHits);
            bool hitAny = false;
            broomTargetsHit.Clear();

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = broomSwingHits[i];
                if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                Vector2 closestPoint = hit.ClosestPoint(body.position);
                Vector2 toTarget = closestPoint - body.position;
                if (toTarget.sqrMagnitude > 0.0001f && Vector2.Angle(lookDirection, toTarget) > broomSwingHalfAngle)
                {
                    continue;
                }

                if (hit.GetComponentInParent<IBroomTarget>() is not { RequiresBroom: true } target)
                {
                    continue;
                }

                if (!broomTargetsHit.Add(target))
                {
                    continue;
                }

                target.Interact(gameObject);
                hitAny = true;
            }

            AudioManager.Instance?.PlaySfx(broomSwingSfx);

            return hitAny;
        }

        private void FixedUpdate()
        {
            Vector2 movement = moveInput;
            float speed = moveSpeed * upgradeMoveMultiplier;

            if (dashRemaining > 0f)
            {
                dashRemaining = Mathf.Max(0f, dashRemaining - Time.fixedDeltaTime);
                movement = dashDirection;
                speed *= dashSpeedMultiplier;
            }

            Vector2 nextPosition = body.position + movement * (speed * Time.fixedDeltaTime);
            body.MovePosition(nextPosition);

            // 스프라이트 기본 방향(위)을 기준으로 바라보는 방향까지 서서히 회전시킨다.
            float targetAngle = Vector2.SignedAngle(Vector2.up, lookDirection);
            float smoothedAngle = Mathf.LerpAngle(body.rotation, targetAngle, rotationLerpSpeed * Time.fixedDeltaTime);
            body.MoveRotation(smoothedAngle);
        }

        private void OnDisable()
        {
            UpgradeApi.UpgradePurchased -= OnUpgradePurchased;
            PlayerControlBindings.Changed -= OnControlBindingsChanged;
            capturedSelection?.CancelSelection(gameObject);
            ReleaseSelectionInput();
            moveInput = Vector2.zero;
            dashRemaining = 0f;
            SetFocusedObject(null);
        }

        private void TryStartDash()
        {
            if (!UpgradeApi.DashUnlocked || dashCooldownRemaining > 0f)
            {
                return;
            }

            dashDirection = moveInput.sqrMagnitude > 0f ? moveInput.normalized : lookDirection;
            dashRemaining = dashDurationSeconds;
            dashCooldownRemaining = dashCooldownSeconds;
        }

        private void OnUpgradePurchased(UpgradeId _, int __) => RefreshCommonUpgrades();

        private void OnControlBindingsChanged(PlayerControlProfile profile)
        {
            if (profile == controlProfile)
            {
                ApplyControlBindings();
            }
        }

        private void ApplyControlBindings()
        {
            moveUpKey = PlayerControlBindings.Get(controlProfile, PlayerControlAction.MoveUp);
            moveDownKey = PlayerControlBindings.Get(controlProfile, PlayerControlAction.MoveDown);
            moveLeftKey = PlayerControlBindings.Get(controlProfile, PlayerControlAction.MoveLeft);
            moveRightKey = PlayerControlBindings.Get(controlProfile, PlayerControlAction.MoveRight);
            interactKey = PlayerControlBindings.Get(controlProfile, PlayerControlAction.Interact);
            scrollUpKey = PlayerControlBindings.Get(controlProfile, PlayerControlAction.SelectUp);
            scrollDownKey = PlayerControlBindings.Get(controlProfile, PlayerControlAction.SelectDown);
            dashKey = PlayerControlBindings.Get(controlProfile, PlayerControlAction.Dash);
            dropKey = PlayerControlBindings.Get(controlProfile, PlayerControlAction.Drop);
        }

        private void ApplyGameBalance()
        {
            GameBalance.EnsureLoaded();
            PlayerBalance settings = GameBalance.Current.player;
            moveSpeed = Mathf.Max(0f, settings.moveSpeed);
            rotationLerpSpeed = Mathf.Max(0f, settings.rotationLerpSpeed);
            playerMass = Mathf.Max(1f, settings.mass);
            dashSpeedMultiplier = Mathf.Max(1f, settings.dashSpeedMultiplier);
            dashDurationSeconds = Mathf.Max(0.05f, settings.dashDurationSeconds);
            dashCooldownSeconds = Mathf.Max(0f, settings.dashCooldownSeconds);
            interactionRadius = Mathf.Max(0f, settings.interactionRadius);
            interactionProbeRadius = Mathf.Max(0.01f, settings.interactionProbeRadius);
            interactionReachPadding = Mathf.Max(0f, settings.interactionReachPadding);
            heldItemDropDistance = Mathf.Max(0.2f, settings.heldItemDropDistance);
            broomSwingRadius = Mathf.Max(0.1f, settings.broomSwingRadius);
            broomSwingHalfAngle = Mathf.Clamp(settings.broomSwingHalfAngle, 0f, 180f);
        }

        private void RefreshCommonUpgrades()
        {
            upgradeMoveMultiplier = UpgradeApi.MoveSpeedMultiplier;
        }

        private Vector2 ReadMoveInput(Keyboard keyboard)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard[moveLeftKey].isPressed)
            {
                horizontal -= 1f;
            }

            if (keyboard[moveRightKey].isPressed)
            {
                horizontal += 1f;
            }

            if (keyboard[moveDownKey].isPressed)
            {
                vertical -= 1f;
            }

            if (keyboard[moveUpKey].isPressed)
            {
                vertical += 1f;
            }

            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        private void UpdateFocusedObject()
        {
            int hitCount = Physics2D.CircleCast(
                body.position,
                interactionProbeRadius,
                lookDirection,
                interactionFilter,
                interactionHits,
                interactionRadius + interactionReachPadding);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = interactionHits[i].collider;
                if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                IInteractable interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null)
                {
                    continue;
                }

                SetFocusedObject(interactable);
                return;
            }

            SetFocusedObject(null);
        }

        private void SetFocusedObject(IInteractable interactable)
        {
            if (ReferenceEquals(focusedInteractable, interactable))
            {
                return;
            }

            if (focusedOutline != null)
            {
                focusedOutline.SetHighlighted(false);
            }

            if (focusedPrompt != null)
            {
                focusedPrompt.SetFocused(this, false);
            }

            focusedInteractable = interactable;
            focusedOutline = null;
            focusedPrompt = null;

            if (focusedInteractable is Component targetComponent)
            {
                focusedOutline = InteractionOutline2D.GetOrAdd(targetComponent.gameObject);
                focusedOutline.SetHighlighted(true);

                focusedPrompt = targetComponent.GetComponent<InteractionPromptBubble>();
                if (focusedPrompt != null)
                {
                    focusedPrompt.SetFocused(this, true);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector2 direction = Application.isPlaying ? lookDirection : Vector2.down;
            float reach = interactionRadius + interactionReachPadding;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction * reach);
            Gizmos.DrawWireSphere((Vector2)transform.position + direction * reach, interactionProbeRadius);

            Gizmos.color = new Color(0.9f, 0.5f, 0.1f, 0.6f);
            Vector3 origin = transform.position;
            Quaternion leftEdge = Quaternion.Euler(0f, 0f, broomSwingHalfAngle);
            Quaternion rightEdge = Quaternion.Euler(0f, 0f, -broomSwingHalfAngle);
            Gizmos.DrawLine(origin, origin + leftEdge * (Vector3)direction * broomSwingRadius);
            Gizmos.DrawLine(origin, origin + rightEdge * (Vector3)direction * broomSwingRadius);
            Gizmos.DrawWireSphere(origin, broomSwingRadius);
        }
    }
}
