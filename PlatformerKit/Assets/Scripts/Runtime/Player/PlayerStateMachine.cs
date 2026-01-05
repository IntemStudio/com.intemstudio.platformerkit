using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace IntemStudio
{
    public class PlayerStateMachine : MonoBehaviour
    {
        public PlayerState CurrentState { get; private set; }

        private Dictionary<PlayerState, IPlayerState> _states;
        private PlayerPhysics _physics;
        private PlayerInput _input;
        private PlayerController _controller;

        private void Awake()
        {
            _physics = GetComponent<PlayerPhysics>();
            _input = GetComponent<PlayerInput>();
            _controller = GetComponent<PlayerController>();
            InitializeStates();
        }

        private void InitializeStates()
        {
            _states = new Dictionary<PlayerState, IPlayerState>
        {
            { PlayerState.Idle, new IdleState(this, _physics, _input) },
            { PlayerState.Walk, new WalkState(this, _physics, _input) },
            { PlayerState.Jumping, new JumpState(this, _physics, _input) },
            { PlayerState.Falling, new FallingState(this, _physics, _input) },
            { PlayerState.Dash, new DashState(this, _physics, _input) }
        };

            // 초기 상태 설정
            ChangeState(PlayerState.Idle);
        }

        public void LogicUpdate()
        {
            // 입력 처리 (상태 전환보다 먼저 처리)
            HandleInput();

            // 현재 상태 업데이트
            if (_states.TryGetValue(CurrentState, out IPlayerState currentState))
            {
                currentState.OnUpdate();
            }
        }

        private void HandleInput()
        {
            if (_input == null) return;

            // 점프 입력 감지
            if (_input.IsJumpPressed)
            {
                // 아래 방향 키가 눌려있지 않으면 일반 점프
                if (!_input.IsDownInputPressed)
                {
                    RequestJump();
                }
            }

            // 대시 입력 감지
            if (_input.IsDashPressed)
            {
                Vector2 dashDirection = CalculateDashDirection();
                RequestDash(dashDirection);
            }
        }

        private Vector2 CalculateDashDirection()
        {
            if (_input == null) return Vector2.right;

            // 대시 방향 계산
            if (Mathf.Abs(_input.HorizontalInput) > 0.01f)
            {
                return _input.HorizontalInput > 0 ? Vector2.right : Vector2.left;
            }

            // 입력이 없는 경우: Model Transform의 스케일 x 값 기준
            if (_controller != null)
            {
                Transform modelTransform = _controller.transform.Find("Model");
                if (modelTransform != null)
                {
                    return modelTransform.localScale.x > 0 ? Vector2.right : Vector2.left;
                }
            }

            // 기본값: 오른쪽
            return Vector2.right;
        }

        public void PhysisUpdate()
        {
            // 현재 상태 FixedUpdate
            if (_states.TryGetValue(CurrentState, out IPlayerState currentState))
            {
                currentState.OnFixedUpdate();
            }
        }

        public void ChangeState(PlayerState newState)
        {
            if (CurrentState == newState) return;

            // 이전 상태 Exit
            if (_states.TryGetValue(CurrentState, out IPlayerState previousState))
            {
                previousState.OnExit();
            }

            // 새 상태 Enter
            CurrentState = newState;
            if (_states.TryGetValue(CurrentState, out IPlayerState nextState))
            {
                nextState.OnEnter();
            }
        }

        public void RequestJump()
        {
            // 점프 요청 처리 (상태별로 다르게 처리)
            if (_states.TryGetValue(CurrentState, out IPlayerState currentState))
            {
                currentState.OnJumpRequested();
            }
        }

        public void RequestDash(Vector2 direction)
        {
            // 대시 요청 처리
            if (_states.TryGetValue(CurrentState, out IPlayerState currentState))
            {
                // 현재 상태가 Dash가 아니면 Dash 상태로 전환하고 대시 실행
                if (CurrentState != PlayerState.Dash)
                {
                    ChangeState(PlayerState.Dash);
                    // Dash 상태로 전환 후 대시 실행
                    if (_physics != null)
                    {
                        _physics.RequestDash(direction);
                    }
                }
                else
                {
                    // 이미 Dash 상태이면 무시
                    currentState.OnDashRequested(direction);
                }
            }
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            // 플레이어 위치 위에 현재 상태 표시
            Vector3 position = transform.position + Vector3.up;
            string stateText = $"State: {CurrentState}";

            // 라벨 스타일 설정
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            // 라벨 표시
            Handles.Label(position, stateText, style);
#endif
        }
    }
}