using UnityEngine;

namespace IntemStudio
{
    public class IdleState : IPlayerState
    {
        private PlayerStateMachine _stateMachine;
        private PlayerPhysics _physics;
        private PlayerInput _input;

        public IdleState(PlayerStateMachine stateMachine, PlayerPhysics physics, PlayerInput input)
        {
            _stateMachine = stateMachine;
            _physics = physics;
            _input = input;
        }

        public void OnEnter()
        {
            // Idle 상태 진입 시 처리
        }

        public void OnUpdate()
        {
            // 수평 입력이 있으면 Walk로 전환
            if (Mathf.Abs(_input.HorizontalInput) > 0.01f)
            {
                _stateMachine.ChangeState(PlayerState.Walk);
                return;
            }

            // 공중에 떠있으면 Falling로 전환
            if (!_physics.IsGrounded)
            {
                _stateMachine.ChangeState(PlayerState.Falling);
                return;
            }
        }

        public void OnFixedUpdate()
        {
            // Idle 상태 FixedUpdate
        }

        public void OnExit()
        {
            // Idle 상태 종료 시 처리
        }

        public void OnJumpRequested()
        {
            // 점프 요청 시 Jumping 상태로 전환
            if (_physics.IsGrounded)
            {
                _stateMachine.ChangeState(PlayerState.Jumping);
            }
        }

        public void OnDashRequested(Vector2 direction)
        {
            // 대시 요청 시 Dash 상태로 전환
            if (_physics.CanDash)
            {
                _stateMachine.ChangeState(PlayerState.Dash);
            }
        }

        public bool CanTransitionTo(PlayerState targetState)
        {
            // Idle에서 전환 가능한 상태
            return targetState == PlayerState.Walk ||
                   targetState == PlayerState.Jumping ||
                   targetState == PlayerState.Dash ||
                   targetState == PlayerState.Falling;
        }
    }
}