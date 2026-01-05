using UnityEngine;

namespace IntemStudio
{
    public partial class PlayerPhysics
    {
        private const float DASH_VELOCITY_Y = 0f;

        [Header("Dash")]
        [SerializeField] private float _dashDistance = 2f;
        [SerializeField] private float _dashDuration = 0.2f;
        [SerializeField] private float _dashCooldown = 0.5f;
        [SerializeField] private bool _airDashEnabled = false;

        private float _dashDurationCounter;
        private Vector2 _dashDirection;
        private bool _wasGroundedBeforeDash;

        private void InitializeDashSystem()
        {
        }

        public void RequestDash(Vector2 direction)
        {
            if (!CanDash) return;
            if (!_airDashEnabled && !IsGrounded) return;
            ExecuteDash(direction);
        }

        private void ExecuteDash(Vector2 direction)
        {
            if (_rb == null) return;

            if (direction.magnitude < 0.01f)
            {
                direction = Vector2.right;
            }
            else
            {
                direction.Normalize();
            }

            float dashSpeed = _dashDistance / _dashDuration;
            _dashDirection = direction;
            _rb.linearVelocity = new Vector2(_dashDirection.x * dashSpeed, DASH_VELOCITY_Y);

            IsDashing = true;
            _dashDurationCounter = _dashDuration;
            _wasGroundedBeforeDash = IsGrounded;
            DashCooldownRemaining = _dashCooldown;
        }

        private void SetAirDashEnabledInternal(bool enabled)
        {
            _airDashEnabled = enabled;
            IsAirDashEnabled = enabled;
        }

        private void UpdateDash()
        {
            if (IsDashing)
            {
                _dashDurationCounter -= Time.fixedDeltaTime;

                if (_rb != null)
                {
                    float dashSpeed = _dashDistance / _dashDuration;
                    _rb.linearVelocity = new Vector2(_dashDirection.x * dashSpeed, DASH_VELOCITY_Y);
                }

                if (_dashDurationCounter <= 0f)
                {
                    IsDashing = false;
                    _dashDirection = Vector2.zero;
                }
            }

            if (DashCooldownRemaining > 0f)
            {
                DashCooldownRemaining -= Time.fixedDeltaTime;
            }
        }
    }
}