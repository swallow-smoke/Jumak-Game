using _001_Scripts._002_Controller.Interface;
using _001_Scripts._003_Object.Interface;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._005_Data.Upgrade;
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

        private readonly RaycastHit2D[] interactionHits = new RaycastHit2D[24];
        private Rigidbody2D body;
        private ISingleItemCarrier carrier;
        private Vector2 moveInput;
        private Vector2 lookDirection = Vector2.down;
        private ContactFilter2D interactionFilter;
        private IInteractable focusedInteractable;
        private InteractionOutline2D focusedOutline;
        private RunState runState;
        private float upgradeMoveMultiplier = 1f;
        private Vector2 dashDirection;
        private float dashRemaining;
        private float dashCooldownRemaining;

        // WASD 플레이어는 왼쪽 Shift, 방향키 플레이어는 오른쪽 Shift를 사용한다.
        private Key DashKey => moveUpKey == Key.UpArrow ? Key.RightShift : Key.LeftShift;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            carrier = GetComponent<ISingleItemCarrier>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            interactionFilter = new ContactFilter2D();
            interactionFilter.SetLayerMask(interactionLayer);
            interactionFilter.useTriggers = true;
        }

        private void OnEnable()
        {
            runState = RunState.Instance;
            if (runState != null)
            {
                runState.Purchased += OnUpgradePurchased;
            }

            RefreshCommonUpgrades();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                moveInput = Vector2.zero;
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
            }

            if (focusedInteractable is IScrollSelectable scrollSelectable)
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
            if (focusedInteractable != null)
            {
                if (InteractionRules.CanInteract(carrier, focusedInteractable))
                {
                    focusedInteractable.Interact(gameObject);
                }

                return;
            }

            InteractionRules.TryDropHeldItem(carrier, body.position + lookDirection * 0.6f);
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
            if (runState != null)
            {
                runState.Purchased -= OnUpgradePurchased;
            }

            runState = null;
            moveInput = Vector2.zero;
            dashRemaining = 0f;
            SetFocusedObject(null);
        }

        private void TryStartDash()
        {
            if (runState == null || runState.GetLevel(UpgradeId.Dash) <= 0 || dashCooldownRemaining > 0f)
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
            if (runState == null)
            {
                upgradeMoveMultiplier = 1f;
                return;
            }

            int speedLevels = runState.GetLevel(UpgradeId.MoveSpeed1)
                              + runState.GetLevel(UpgradeId.MoveSpeed2)
                              + runState.GetLevel(UpgradeId.MoveSpeed3);
            upgradeMoveMultiplier = 1f + Mathf.Clamp(speedLevels, 0, 3) * 0.1f;
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

            focusedInteractable = interactable;
            focusedOutline = null;

            if (focusedInteractable is Component targetComponent)
            {
                focusedOutline = InteractionOutline2D.GetOrAdd(targetComponent.gameObject);
                focusedOutline.SetHighlighted(true);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector2 direction = Application.isPlaying ? lookDirection : Vector2.down;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction * interactionRadius);
        }
    }
}
