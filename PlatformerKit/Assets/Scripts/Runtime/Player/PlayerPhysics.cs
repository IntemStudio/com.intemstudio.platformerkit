using UnityEngine;

namespace IntemStudio
{
    public partial class PlayerPhysics : MonoBehaviour
    {
        [SerializeField] private Vector2 _colliderSize = new Vector2(0.5f, 1f);
        [SerializeField] private bool _autoSetupCollider = true;

        protected Rigidbody2D _rb;
        protected BoxCollider2D _boxCollider;
        public bool IsGrounded { get; protected set; }
        public bool IsJumping { get; protected set; }
        public bool IsOnOneWayPlatform { get; protected set; }
        public int RemainingJumps { get; protected set; }
        public int ExtraJumps { get; protected set; }
        public bool IsDashing { get; protected set; }
        public float DashCooldownRemaining { get; protected set; }
        public bool CanDash => DashCooldownRemaining <= 0f && !IsDashing;
        public bool IsAirDashEnabled { get; protected set; }
        public Vector2 RigidbodyVelocity => _rb != null ? _rb.linearVelocity : Vector2.zero;
        public bool IsLeftCollision { get; protected set; }
        public bool IsRightCollision { get; protected set; }
        public bool IsCeiling { get; protected set; }
        public bool IsCollideX { get; protected set; }
        public bool IsCollideY { get; protected set; }

        private void Awake()
        {
            SetupRigidbody2D();

            if (_autoSetupCollider)
            {
                SetupCollider();
            }

            InitializeSystems();
        }

        private void SetupRigidbody2D()
        {
            _rb = GetComponent<Rigidbody2D>();

            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody2D>();
            }

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.freezeRotation = true;
            _rb.gravityScale = 3f;
        }

        private void SetupCollider()
        {
            _boxCollider = GetComponent<BoxCollider2D>();

            if (_boxCollider == null)
            {
                _boxCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            _boxCollider.size = _colliderSize;
            _boxCollider.isTrigger = false;
        }

        private void InitializeSystems()
        {
            InitializeCollisionSystem();
            InitializeJumpSystem();
            InitializeDashSystem();
            InitializeDownJumpSystem();
        }

        public void ApplyHorizontalVelocity(float velocityX)
        {
            if (_rb == null) return;
            _rb.linearVelocity = new Vector2(velocityX, _rb.linearVelocity.y);
        }

        public void ApplyVerticalVelocity(float velocityY)
        {
            if (_rb == null) return;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, velocityY);
        }

        public void ApplyVelocity(Vector2 velocity)
        {
            if (_rb == null) return;
            _rb.linearVelocity = velocity;
        }

        public void PhysisUpdate()
        {
            UpdateCollisionDetection();
            UpdateDash();
            UpdateDownJump();
            UpdateJump();
        }

        public void SetExtraJumps(int count)
        {
            SetExtraJumpsInternal(count);
        }

        public void SetAirDashEnabled(bool enabled)
        {
            SetAirDashEnabledInternal(enabled);
        }
    }
}