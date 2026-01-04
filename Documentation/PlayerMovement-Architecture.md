# 플레이어 이동 시스템 아키텍처

2D 플랫포머 게임의 플레이어 이동 시스템 클래스 분리 및 상태 머신 설계 문서입니다.

## 개요

이 문서는 플레이어 이동 시스템의 클래스 분리 구조와 상태 머신 설계를 설명합니다. 관심사 분리(Separation of Concerns) 원칙을 따라 각 클래스의 책임을 명확히 하고, 상태 머신을 통해 복잡한 이동 상태를 관리합니다.

설계 원칙과 구현 세부사항은 다음 문서를 참조하세요:

- [PlayerMovement-Design.md](./PlayerMovement-Design.md) - 설계 원칙 및 아키텍처
- [PlayerMovement-Implementation.md](./PlayerMovement-Implementation.md) - 구현 세부사항

## 현재 구조의 문제점

### PlayerController

- **입력 처리** + **로직 조율** + **모델 방향 업데이트**가 혼재
- Unity Input API에 직접 의존하여 테스트 및 교체가 어려움
- 입력 처리 로직이 복잡해짐에 따라 유지보수가 어려워짐

### PlayerPhysics

- **물리 처리** + **상태 관리** + **로직 실행**이 혼재
- 점프, 대시, 아래 점프 등 다양한 이동 기능의 상태 관리가 복잡함
- 향후 벽 매달리기, 난간 매달리기, 사다리 등 추가 기능 확장 시 복잡도 증가

## 제안하는 클래스 분리 구조

### 전체 아키텍처

```mermaid
graph TD
    A[PlayerInput] -->|입력 값 제공| B[PlayerController]
    B -->|상태 전환 요청| C[PlayerStateMachine]
    B -->|물리 요청| D[PlayerPhysics]
    C -->|상태별 물리 설정| D
    C -->|상태별 동작| E[PlayerState 구현체들]
    D -->|Rigidbody2D 조작| F[Rigidbody2D]
    D -->|Collider 관리| G[BoxCollider2D]
    D -->|바닥/벽 감지| H[Raycast 시스템]

    E -->|IdleState| I[IdleState]
    E -->|WalkState| J[WalkState]
    E -->|JumpState| K[JumpState]
    E -->|DashState| L[DashState]
    E -->|FallingState| M[FallingState]
    E -->|WallHangState| N[WallHangState]
    E -->|WallSlideState| O[WallSlideState]
    E -->|LedgeHangState| P[LedgeHangState]
    E -->|LadderState| Q[LadderState]
```

### 클래스 계층 구조

1. **PlayerInput** - 입력 처리 계층
2. **PlayerController** - 로직 조율 계층
3. **PlayerStateMachine** - 상태 관리 계층
4. **PlayerState** (인터페이스/추상 클래스) - 상태 베이스
5. **각 상태별 클래스** - 상태 구현체
6. **PlayerPhysics** - 물리 처리 계층

## 클래스 상세 설계

### 1. PlayerInput

**책임**: Unity Input API 래핑, 입력 값만 읽어서 반환

**주요 기능:**

- Unity Input API를 추상화하여 입력 시스템 교체 용이성 확보
- 입력 값만 읽어서 프로퍼티로 제공
- 입력 처리 로직은 포함하지 않음

**공개 API:**

```csharp
public class PlayerInput : MonoBehaviour
{
    // 수평 이동 입력 (-1.0 ~ 1.0)
    public float HorizontalInput { get; private set; }

    // 수직 이동 입력 (-1.0 ~ 1.0)
    public float VerticalInput { get; private set; }

    // 점프 입력 (한 프레임만 true)
    public bool IsJumpPressed { get; private set; }

    // 점프 키 해제 (한 프레임만 true)
    public bool IsJumpReleased { get; private set; }

    // 아래 방향 키 입력 상태
    public bool IsDownInputPressed { get; private set; }

    // 대시 입력 (한 프레임만 true)
    public bool IsDashPressed { get; private set; }

    private void Update()
    {
        // 입력 읽기만 수행
        HorizontalInput = Input.GetAxis("Horizontal");
        VerticalInput = Input.GetAxis("Vertical");
        IsDownInputPressed = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        IsJumpPressed = Input.GetKeyDown(KeyCode.Space);
        IsJumpReleased = Input.GetKeyUp(KeyCode.Space);
        IsDashPressed = Input.GetKeyDown(KeyCode.LeftShift);
    }
}
```

**장점:**

- Unity Input System으로 전환 시 이 클래스만 수정하면 됨
- 테스트 시 Mock 객체로 교체 가능
- 입력 처리 로직이 한 곳에 집중됨

### 2. PlayerController

**책임**: 입력을 받아서 상태 머신과 연동, 로직 조율, 모델 방향 업데이트

**주요 기능:**

- `PlayerInput`에서 입력 값 받기
- 입력 기반 로직 처리 (점프, 대시, 아래 점프 등)
- 상태 머신에 상태 전환 요청
- 모델 방향 업데이트
- 이동 속도 계산 및 `PlayerPhysics`에 전달

**공개 API:**

```csharp
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;

    private PlayerInput _input;
    private PlayerStateMachine _stateMachine;
    private PlayerPhysics _physics;
    private Transform _modelTransform;

    private void Awake()
    {
        _input = GetComponent<PlayerInput>();
        _stateMachine = GetComponent<PlayerStateMachine>();
        _physics = GetComponent<PlayerPhysics>();
        SetupModel();
    }

    private void Update()
    {
        // 입력 기반 로직 처리
        HandleJump();
        HandleDash();
        HandleDownJump();
        UpdateModelDirection();
    }

    private void FixedUpdate()
    {
        // 현재 상태에 따라 이동 처리
        if (_stateMachine.CurrentState != PlayerState.Dashing)
        {
            float targetVelocityX = _input.HorizontalInput * _moveSpeed;
            _physics.ApplyHorizontalVelocity(targetVelocityX);
        }
    }

    private void HandleJump()
    {
        if (_input.IsJumpPressed)
        {
            if (_input.IsDownInputPressed)
            {
                // 아래 점프는 상태 머신을 거치지 않고 직접 처리
                _physics.RequestDownJump();
            }
            else
            {
                // 일반 점프는 상태 머신에 요청
                _stateMachine.RequestJump();
            }
        }

        if (_input.IsJumpReleased && !_input.IsDownInputPressed)
        {
            _physics.ReleaseJump();
        }
    }

    private void HandleDash()
    {
        if (_input.IsDashPressed)
        {
            Vector2 dashDirection = CalculateDashDirection();
            _stateMachine.RequestDash(dashDirection);
        }
    }

    private void HandleDownJump()
    {
        // 아래 점프는 지면에 있을 때만 가능하므로 상태 머신을 거치지 않음
    }

    private Vector2 CalculateDashDirection()
    {
        // 대시 방향 계산 로직
        if (Mathf.Abs(_input.HorizontalInput) > 0.01f)
        {
            return _input.HorizontalInput > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            if (_modelTransform != null)
            {
                return _modelTransform.localScale.x > 0 ? Vector2.right : Vector2.left;
            }
            return Vector2.right;
        }
    }

    private void UpdateModelDirection()
    {
        if (_modelTransform == null) return;

        if (Mathf.Abs(_input.HorizontalInput) > 0.01f)
        {
            Vector3 scale = _modelTransform.localScale;
            scale.x = _input.HorizontalInput > 0 ? 1f : -1f;
            _modelTransform.localScale = scale;
        }
    }
}
```

### 3. PlayerStateMachine

**책임**: 상태 전환 관리, 상태별 업데이트 호출

**주요 기능:**

- 상태 전환 조건 검사
- 상태 전환 시 이전 상태 Exit → 새 상태 Enter 순서 보장
- 상태별 업데이트 호출
- 상태 전환 중복 방지

**상태 열거형:**

```csharp
public enum PlayerState
{
    None,
    Idle,
    Walk,
    Jumping,
    Falling,
    Dash,
    WallHang,
    WallSlide,
    LedgeHang,
    Ladder
}
```

**공개 API:**

```csharp
public class PlayerStateMachine : MonoBehaviour
{
    public PlayerState CurrentState { get; private set; }

    private Dictionary<PlayerState, IPlayerState> _states;
    private PlayerPhysics _physics;
    private PlayerInput _input;

    private void Awake()
    {
        _physics = GetComponent<PlayerPhysics>();
        _input = GetComponent<PlayerInput>();
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
            { PlayerState.Dash, new DashState(this, _physics, _input) },
            { PlayerState.WallHang, new WallHangState(this, _physics, _input) },
            { PlayerState.WallSlide, new WallSlideState(this, _physics, _input) },
            { PlayerState.LedgeHang, new LedgeHangState(this, _physics, _input) },
            { PlayerState.Ladder, new LadderState(this, _physics, _input) }
        };

        // 초기 상태 설정
        ChangeState(PlayerState.Idle);
    }

    private void Update()
    {
        // 현재 상태 업데이트
        if (_states.TryGetValue(CurrentState, out IPlayerState currentState))
        {
            currentState.OnUpdate();
        }

        // 상태 전환 조건 검사
        CheckStateTransitions();
    }

    private void FixedUpdate()
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
            currentState.OnDashRequested(direction);
        }
    }

    private void CheckStateTransitions()
    {
        // 상태 전환 조건 검사
        // 각 상태에서 CanTransitionTo() 메서드를 통해 전환 가능 여부 확인
    }
}
```

### 4. IPlayerState 인터페이스

**책임**: 상태 인터페이스 정의

**공개 API:**

```csharp
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
```

### 5. 상태 구현체 예시

#### IdleState

```csharp
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
        // Idle 상태 업데이트
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
```

#### WalkState

```csharp
public class WalkState : IPlayerState
{
    private PlayerStateMachine _stateMachine;
    private PlayerPhysics _physics;
    private PlayerInput _input;

    public WalkState(PlayerStateMachine stateMachine, PlayerPhysics physics, PlayerInput input)
    {
        _stateMachine = stateMachine;
        _physics = physics;
        _input = input;
    }

    public void OnEnter()
    {
        // Walk 상태 진입 시 처리
    }

    public void OnUpdate()
    {
        // Walk 상태 업데이트
        // 수평 입력이 없으면 Idle로 전환
        if (Mathf.Abs(_input.HorizontalInput) < 0.01f)
        {
            _stateMachine.ChangeState(PlayerState.Idle);
        }

        // 공중에 떠있으면 Falling로 전환
        if (!_physics.IsGrounded)
        {
            _stateMachine.ChangeState(PlayerState.Falling);
        }
    }

    public void OnFixedUpdate()
    {
        // Walk 상태 FixedUpdate
    }

    public void OnExit()
    {
        // Walk 상태 종료 시 처리
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
        // Walk에서 전환 가능한 상태
        return targetState == PlayerState.Idle ||
               targetState == PlayerState.Jumping ||
               targetState == PlayerState.Dash ||
               targetState == PlayerState.Falling;
    }
}
```

#### JumpState

```csharp
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
        _physics.RequestJump();
    }

    public void OnUpdate()
    {
        // Jumping 상태 업데이트
        // 상승 속도가 0 이하이면 Falling로 전환
        if (_physics.RigidbodyVelocity.y <= 0f)
        {
            _stateMachine.ChangeState(PlayerState.Falling);
        }

        // 바닥에 닿으면 Idle 또는 Walk로 전환
        if (_physics.IsGrounded)
        {
            if (Mathf.Abs(_input.HorizontalInput) > 0.01f)
            {
                _stateMachine.ChangeState(PlayerState.Walk);
            }
            else
            {
                _stateMachine.ChangeState(PlayerState.Idle);
            }
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
            _physics.RequestJump();
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
```

### 6. PlayerPhysics (리팩토링)

**책임**: 순수 물리 처리만 담당

**주요 기능:**

- Rigidbody2D 및 Collider 설정
- 속도 적용 (수평/수직/전체)
- 바닥 감지 (Raycast 기반)
- 벽 감지 (향후 확장)
- 난간 감지 (향후 확장)
- 점프/대시/아래 점프 실행 (내부 메서드)

**변경 사항:**

- 상태 관리 로직 제거 (상태 머신으로 이동)
- 입력 처리 관련 코드 제거 (PlayerInput으로 이동)
- 물리 상태만 관리 (IsGrounded, IsDashing 등)

**공개 API:**

```csharp
public class PlayerPhysics : MonoBehaviour
{
    // 물리 상태 (읽기 전용)
    public bool IsGrounded { get; private set; }
    public bool IsDashing { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsOnWall { get; private set; } // 향후 확장
    public bool IsLedgeHanging { get; private set; } // 향후 확장
    public bool IsOnLadder { get; private set; } // 향후 확장

    // 속도 적용
    public void ApplyHorizontalVelocity(float velocityX);
    public void ApplyVerticalVelocity(float velocityY);
    public void ApplyVelocity(Vector2 velocity);

    // 점프 시스템
    public void RequestJump();
    public void ReleaseJump();
    public void RequestDownJump();

    // 대시 시스템
    public void RequestDash(Vector2 direction);

    // 바닥 감지
    public void CheckGrounded();

    // 벽 감지 (향후 확장)
    public void CheckWall();

    // 난간 감지 (향후 확장)
    public void CheckLedge();
}
```

## 상태 전환 로직

### 기본 이동 상태 전환

| 현재 상태 | 전환 조건      | 다음 상태         |
| --------- | -------------- | ----------------- |
| Idle      | 수평 입력 있음 | Walk              |
| Idle      | 점프 입력      | Jumping           |
| Idle      | 대시 입력      | Dash              |
| Idle      | 바닥 이탈      | Falling           |
| Walk      | 수평 입력 없음 | Idle              |
| Walk      | 점프 입력      | Jumping           |
| Walk      | 대시 입력      | Dash              |
| Walk      | 바닥 이탈      | Falling           |
| Jumping   | 상승 속도 ≤ 0  | Falling           |
| Jumping   | 바닥 착지      | Idle/Walk         |
| Falling   | 바닥 착지      | Idle/Walk         |
| Dash      | 대시 완료      | Idle/Walk/Falling |

### 벽 관련 상태 전환 (향후 확장)

| 현재 상태          | 전환 조건      | 다음 상태 |
| ------------------ | -------------- | --------- |
| Falling/Jumping    | 벽 감지        | WallHang  |
| WallHang           | 아래 방향 입력 | WallSlide |
| WallHang/WallSlide | 벽 점프 입력   | Jumping   |
| WallHang/WallSlide | 벽 이탈        | Falling   |

### 난간 관련 상태 전환 (향후 확장)

| 현재 상태       | 전환 조건      | 다음 상태               |
| --------------- | -------------- | ----------------------- |
| Falling/Jumping | 난간 감지      | LedgeHang               |
| LedgeHang       | 위 방향 입력   | Idle/Walk (오르기 완료) |
| LedgeHang       | 아래 방향 입력 | Falling                 |

### 사다리 관련 상태 전환 (향후 확장)

| 현재 상태                 | 전환 조건        | 다음 상태         |
| ------------------------- | ---------------- | ----------------- |
| Idle/Walk/Falling/Jumping | 사다리 영역 진입 | Ladder            |
| Ladder                    | 사다리 영역 이탈 | Idle/Walk/Falling |
| Ladder                    | 점프 입력        | Jumping           |

## 리팩토링 계획

### 1단계: PlayerInput 클래스 생성

- [ ] `PlayerInput` 클래스 생성
- [ ] Unity Input API 래핑
- [ ] 입력 값 프로퍼티 제공
- [ ] `PlayerController`에서 `PlayerInput` 사용하도록 수정

### 2단계: 상태 머신 기본 구조 구현

- [ ] `PlayerState` 열거형 정의
- [ ] `IPlayerState` 인터페이스 정의
- [ ] `PlayerStateMachine` 클래스 생성
- [ ] 기본 상태 클래스 구현 (Idle, Walk, Jumping, Falling, Dash)

### 3단계: PlayerController 리팩토링

- [ ] `PlayerInput` 사용하도록 수정
- [ ] 상태 머신과 연동
- [ ] 입력 처리 로직을 상태별로 분리

### 4단계: PlayerPhysics 리팩토링

- [ ] 입력 처리 관련 코드 제거
- [ ] 상태 관리 로직 제거 (상태 머신으로 이동)
- [ ] 순수 물리 처리만 남기기

### 5단계: 상태별 동작 구현

- [ ] 각 상태의 Enter/Update/Exit 메서드 구현
- [ ] 상태 전환 조건 검사 로직 구현
- [ ] 상태별 물리 설정 적용

### 6단계: 테스트 및 검증

- [ ] 기존 기능이 정상 작동하는지 확인
- [ ] 상태 전환이 올바르게 작동하는지 확인
- [ ] 성능 검증

### 7단계: 향후 확장 기능 추가 (선택)

- [ ] 벽 관련 상태 추가 (WallHang, WallSlide)
- [ ] 난간 관련 상태 추가 (LedgeHang)
- [ ] 사다리 관련 상태 추가 (Ladder)

## 장점

### 1. 관심사 분리

- 각 클래스가 단일 책임을 가짐
- 코드 가독성 및 유지보수성 향상

### 2. 테스트 용이성

- `PlayerInput`을 Mock으로 교체 가능
- 각 상태를 독립적으로 테스트 가능

### 3. 확장성

- 새로운 상태 추가가 용이함
- 새로운 입력 추가가 용이함
- Unity Input System으로 전환 시 `PlayerInput`만 수정

### 4. 상태 관리 명확화

- 상태 전환 로직이 명확함
- 상태별 동작이 캡슐화됨
- 상태 머신을 통해 복잡한 상태 관리 가능

### 5. 코드 재사용성

- 상태 클래스를 재사용 가능
- 입력 처리 로직을 재사용 가능

## 참고 사항

- 상태 머신 리팩토링은 새로운 이동 기능 추가 전에 완료하는 것을 권장합니다.
- 각 기능은 독립적으로 구현 가능하나, 상태 관리 시스템 통합이 필요합니다.
- 기능 해금 시스템과 연동 가능하도록 설계되어 있습니다.
- 기존 이동 시스템과의 통합을 고려하여 점진적으로 리팩토링하는 것을 권장합니다.
