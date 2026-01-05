using UnityEngine;

public class JumpState : IPlayerState
{
    private PlayerStateMachine _stateMachine;
    private PlayerPhysics _physics;
    private PlayerInput _input;

    public JumpState(PlayerStateMachine stateMachine, PlayerPhysics physics, PlayerInput input)
    {
        _stateMachine = stateMachine;
        _physics = physics;
        _input = input;
    }

    public void OnEnter()
    {
        // Jumping 상태 진입 시 점프 실행
        // 점프 가능 여부 확인 후 실행
        if (_physics.RemainingJumps > 0)
        {
            _physics.ExecuteJump();
        }
    }

    public void OnUpdate()
    {
        // 상승 속도가 0 이하이면 Falling로 전환
        // 바닥 착지는 FallingState에서 처리 (단방향 플랫폼 통과 시 문제 방지)
        if (_physics.RigidbodyVelocity.y <= 0f)
        {
            _stateMachine.ChangeState(PlayerState.Falling);
            return;
        }
    }

    public void OnFixedUpdate()
    {
        // Jumping 상태 FixedUpdate
    }

    public void OnExit()
    {
        // Jumping 상태 종료 시 처리
    }

    public void OnJumpRequested()
    {
        // 공중 점프 처리 (더블 점프 등)
        if (_physics.RemainingJumps > 0)
        {
            _physics.ExecuteJump();
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
        // Jumping에서 전환 가능한 상태
        return targetState == PlayerState.Falling ||
               targetState == PlayerState.Idle ||
               targetState == PlayerState.Walk ||
               targetState == PlayerState.Dash;
    }
}