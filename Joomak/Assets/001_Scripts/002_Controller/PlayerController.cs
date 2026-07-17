using _001_Scripts._001_Manager;
using _001_Scripts._002_Controller.Interface;
using _001_Scripts._003_Object.Interface;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._005_Data.Upgrade;
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

        [Header("Interaction")]
        [SerializeField, Min(0f)] private float interactionRadius = 1.2f;
        [SerializeField] private LayerMask interactionLayer = ~0;

        [Header("Broom Swing")]
        [Tooltip("빗자루를 들고 있을 때 상호작용 키를 누르면 정밀 조준 대신 이 반경의 부채꼴로 휘두른다.")]
        [SerializeField, Min(0f)] private float broomSwingRadius = 1.6f;
        [Tooltip("바라보는 방향 기준 좌우로 이 각도 안에 있으면 맞는다.")]
        [SerializeField, Range(0f, 180f)] private float broomSwingHalfAngle = 60f;
        [SerializeField] private AudioClip broomSwingSfx;

        private readonly RaycastHit2D[] interactionHits = new RaycastHit2D[24];
        private readonly Collider2D[] broomSwingHits = new Collider2D[8];
        private Rigidbody2D body;
        private ISingleItemCarrier carrier;
        private Vector2 moveInput;
        private Vector2 lookDirection = Vector2.down;
        private ContactFilter2D interactionFilter;
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

        public static bool IsAnySelectionInputCaptured => selectionCaptureCount > 0;
        public static bool ShouldBlockPauseMenu =>
            IsAnySelectionInputCaptured || selectionEscapeConsumedFrame == Time.frameCount;

        public string InteractKeyLabel => interactKey switch
        {
            Key.Space => "SPACE",
            Key.Enter => "ENTER",
            Key.NumpadEnter => "NUM ENTER",
            _ => interactKey.ToString().ToUpperInvariant()
        };

        // WASD 플레이어는 왼쪽 Shift, 방향키 플레이어는 오른쪽 Shift를 사용한다.
        private Key DashKey => moveUpKey == Key.UpArrow ? Key.RightShift : Key.LeftShift;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            carrier = GetComponent<ISingleItemCarrier>();
            broomSwingSfx ??= Resources.Load<AudioClip>("006_Audio/broom_stick");
            body.gravityScale = 0f;
            body.freezeRotation = true;

            interactionFilter = new ContactFilter2D();
            interactionFilter.SetLayerMask(interactionLayer);
            interactionFilter.useTriggers = true;
        }

        private void OnEnable()
        {
            UpgradeApi.UpgradePurchased += OnUpgradePurchased;

            RefreshCommonUpgrades();
        }

        private void Update()
        {
            if (PauseSettingsMenu.IsPaused)
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
            }

            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - Time.deltaTime);
            if (keyboard[DashKey].wasPressedThisFrame)
            {
                TryStartDash();
            }

            UpdateFocusedObject();

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
            // 빗자루를 든 동안엔 정밀 조준 대신 바라보는 방향으로 휘둘러서 부채꼴 안의 진상 손님을 전부 때린다.
            // 맞은 대상이 하나도 없으면(휘둘렀는데 허공) 평소처럼 내려놓기로 넘어간다.
            if (InteractionRules.IsHoldingBroom(carrier) && TrySwingBroom())
            {
                return;
            }

            // 포커스한 대상이 있어도 실제로 상호작용이 안 먹히면(예: 빗자루 든 채 일반 구조물을 봄)
            // 그냥 멈추지 않고 들고 있는 걸 내려놓는 쪽으로 넘어간다.
            if (focusedInteractable != null && InteractionRules.CanInteract(carrier, focusedInteractable))
            {
                focusedInteractable.Interact(gameObject);
                return;
            }

            InteractionRules.TryDropHeldItem(carrier, body.position + lookDirection * 0.6f);
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
            int hitCount = Physics2D.OverlapCircle(body.position, broomSwingRadius, interactionFilter, broomSwingHits);
            bool hitAny = false;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = broomSwingHits[i];
                if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                Vector2 toTarget = (Vector2)hit.transform.position - body.position;
                if (toTarget.sqrMagnitude > 0.0001f && Vector2.Angle(lookDirection, toTarget) > broomSwingHalfAngle)
                {
                    continue;
                }

                if (hit.GetComponentInParent<IBroomTarget>() is not { RequiresBroom: true } target)
                {
                    continue;
                }

                target.Interact(gameObject);
                hitAny = true;
            }

            if (hitAny)
            {
                AudioManager.Instance?.PlaySfx(broomSwingSfx);
            }

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
            int hitCount = Physics2D.Raycast(
                body.position,
                lookDirection,
                interactionFilter,
                interactionHits,
                interactionRadius);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = interactionHits[i].collider;
                if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                IInteractable interactable = hit.GetComponentInParent<IInteractable>();
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
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction * interactionRadius);

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
