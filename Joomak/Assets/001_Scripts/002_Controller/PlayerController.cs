using _001_Scripts._002_Controller.Interface;
using _001_Scripts._003_Object.Interface;
using _001_Scripts._003_Object._001_Entity.Item;
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

        [Header("Key Map")]
        [SerializeField] private Key moveUpKey = Key.W;
        [SerializeField] private Key moveDownKey = Key.S;
        [SerializeField] private Key moveLeftKey = Key.A;
        [SerializeField] private Key moveRightKey = Key.D;
        [SerializeField] private Key interactKey = Key.Space;

        [Header("Interaction")]
        [SerializeField, Min(0f)] private float interactionRadius = 1.2f;
        [SerializeField] private LayerMask interactionLayer = ~0;

        private readonly RaycastHit2D[] interactionHits = new RaycastHit2D[24];
        private Rigidbody2D body;
        private Vector2 moveInput;
        private Vector2 lookDirection = Vector2.down;
        private ContactFilter2D interactionFilter;
        private IInteractable focusedInteractable;
        private InteractionOutline2D focusedOutline;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            interactionFilter = new ContactFilter2D();
            interactionFilter.SetLayerMask(interactionLayer);
            interactionFilter.useTriggers = true;
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

            UpdateFocusedObject();

            if (keyboard[interactKey].wasPressedThisFrame && focusedInteractable != null)
            {
                focusedInteractable.Interact(gameObject);
            }
        }

        private void FixedUpdate()
        {
            Vector2 nextPosition = body.position + moveInput * (moveSpeed * Time.fixedDeltaTime);
            body.MovePosition(nextPosition);
        }

        private void OnDisable()
        {
            moveInput = Vector2.zero;
            SetFocusedObject(null);
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
