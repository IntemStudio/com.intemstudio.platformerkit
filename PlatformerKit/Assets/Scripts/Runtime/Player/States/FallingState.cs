using UnityEngine;

namespace IntemStudio
{
    public class FallingState : IPlayerState
    {
        private PlayerStateMachine _stateMachine;
        private PlayerPhysics _physics;
        private PlayerInput _input;

        public FallingState(PlayerStateMachine stateMachine, PlayerPhysics physics, PlayerInput input)
        {
            _stateMachine = stateMachine;
            _physics = physics;
            _input = input;
        }

        public void OnEnter()
        {
            // Falling 상태 진입 시 처리
        }

        public void OnUpdate()
        {
            // 바닥에 닿았고, 아래로 내려가는 중이 아니면 Idle 또는 Walk로 전환
            if (_physics.IsGrounded && _physics.RigidbodyVelocity.y >= 0f)
            {
                // 실제 착지 시 점프 카운터 리셋
                _physics.ResetJumpCounterOnLanding();

                if (Mathf.Abs(_input.HorizontalInput) > 0.01f)
                {
                    _stateMachine.ChangeState(PlayerState.Walk);
                }
                else
                {
                    _stateMachine.ChangeState(PlayerState.Idle);
                }
                return;
            }
        }

        public void OnFixedUpdate()
        {
            // Falling 상태 FixedUpdate
        }

        public void OnExit()
        {
            // Falling 상태 종료 시 처리
        }

        public void OnJumpRequested()
        {
            // 공중 점프 처리
            if (_physics.RemainingJumps > 0)
            {
                _stateMachine.ChangeState(PlayerState.Jumping);
            }
        }

        public void OnDashRequested(Vector2 direction)
        {
            // 공중 대시 처리
            if (_physics.CanDash && _physics.IsAirDashEnabled)
            {
                _stateMachine.ChangeState(PlayerState.Dash);
            }
        }

        public bool CanTransitionTo(PlayerState targetState)
        {
            // Falling에서 전환 가능한 상태
            return targetState == PlayerState.Idle ||
                   targetState == PlayerState.Walk ||
                   targetState == PlayerState.Jumping ||
                   targetState == PlayerState.Dash;
        }
    }
}