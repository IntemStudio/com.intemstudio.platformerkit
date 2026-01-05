using UnityEngine;

public interface IPlayerState
{
    void OnEnter();

    void OnUpdate();

    void OnFixedUpdate();

    void OnExit();

    void OnJumpRequested();

    void OnDashRequested(Vector2 direction);

    bool CanTransitionTo(PlayerState targetState);
}