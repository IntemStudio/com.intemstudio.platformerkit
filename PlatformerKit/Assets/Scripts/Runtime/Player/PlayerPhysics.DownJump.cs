using UnityEngine;

namespace IntemStudio
{
    public partial class PlayerPhysics
    {
        [Header("Down Jump")]
        [SerializeField] private float _downJumpForce = -20f;
        [SerializeField] private float _downJumpPlatformIgnoreTime = 0.3f;

        private float _platformIgnoreTimer;

        private void InitializeDownJumpSystem()
        {
        }

        public void RequestDownJump()
        {
            if (_rb == null) return;
            if (!IsGrounded) return;
            ExecuteDownJump();
        }

        private void ExecuteDownJump()
        {
            if (_rb == null) return;

            float currentVelocityY = _rb.linearVelocity.y;
            float newVelocityY = Mathf.Min(currentVelocityY, _downJumpForce);
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, newVelocityY);

            _isIgnoringPlatforms = true;
            _platformIgnoreTimer = _downJumpPlatformIgnoreTime;
            IgnoreOneWayPlatformsBelow();
        }

        private void IgnoreOneWayPlatformsBelow()
        {
            if (_boxCollider == null) return;

            Vector2 boxSize = _boxCollider.size;
            Vector2 boxCenter = _boxCollider.bounds.center;
            Vector2 rayOrigin = new Vector2(boxCenter.x, boxCenter.y - boxSize.y * 0.5f - 0.05f);
            float checkDistance = 2f;

            RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, Vector2.down, checkDistance, _groundLayerMask);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider == _boxCollider) continue;

                OneWayPlatform oneWayPlatform = hit.collider.GetComponent<OneWayPlatform>();
                if (oneWayPlatform != null)
                {
                    oneWayPlatform.DisableCollisionTemporarily(_boxCollider, _downJumpPlatformIgnoreTime);
                }
            }
        }

        private void UpdateDownJump()
        {
            if (_isIgnoringPlatforms)
            {
                _platformIgnoreTimer -= Time.fixedDeltaTime;
                if (_platformIgnoreTimer <= 0f)
                {
                    _isIgnoringPlatforms = false;
                }
            }
        }
    }
}