using UnityEngine;

namespace IntemStudio
{
    public partial class PlayerPhysics
    {
        [Header("Jump")]
        [SerializeField] private float _jumpForce = 15f;
        [SerializeField] private float _jumpBufferTime = 0.2f;
        [SerializeField] private float _jumpCutMultiplier = 0.5f;
        [SerializeField] private int _extraJumps = 0;

        private float _jumpBufferCounter;
        private int _jumpCounter;

        private void InitializeJumpSystem()
        {
            _jumpCounter = 1 + _extraJumps;
            ExtraJumps = _extraJumps;
        }

        public void RequestJump()
        {
            _jumpBufferCounter = _jumpBufferTime;
        }

        public void ExecuteJumpIfPossible()
        {
            if (_jumpBufferCounter > 0f && _jumpCounter > 0)
            {
                if (IsGrounded)
                {
                    ExecuteJump();
                    _jumpCounter = _extraJumps;
                }
                else if (!IsGrounded && _extraJumps > 0 && _jumpCounter > 0)
                {
                    ExecuteJump();
                    _jumpCounter--;
                }
            }
        }

        public void ExecuteJump()
        {
            if (_rb == null) return;

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
            _coyoteTimeCounter = 0f;
            _jumpBufferCounter = 0f;
            IsJumping = true;

            if (IsGrounded)
            {
                _jumpCounter = _extraJumps;
            }
            else
            {
                if (_jumpCounter > 0)
                {
                    _jumpCounter--;
                }
            }
        }

        public void ReleaseJump()
        {
            if (_rb == null) return;
            if (_rb.linearVelocity.y > 0f)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * _jumpCutMultiplier);
            }
        }

        public void ResetJumpCounterOnLanding()
        {
            _jumpCounter = 1 + _extraJumps;
        }

        private void SetExtraJumpsInternal(int count)
        {
            _extraJumps = Mathf.Max(0, count);
            ExtraJumps = _extraJumps;

            if (IsGrounded)
            {
                _jumpCounter = 1 + _extraJumps;
            }
        }

        private void UpdateJump()
        {
            if (_jumpBufferCounter > 0f)
            {
                _jumpBufferCounter -= Time.fixedDeltaTime;
            }

            RemainingJumps = _jumpCounter;
        }
    }
}