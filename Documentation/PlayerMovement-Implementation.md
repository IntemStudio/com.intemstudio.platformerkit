# 플레이어 이동 시스템 구현

2D 플랫포머 게임의 플레이어 이동 시스템 구현 세부사항과 사용 방법을 설명합니다.

## 개요

이 문서는 플레이어 이동 시스템의 실제 구현 세부사항, 코드, 사용 방법을 다룹니다.

설계 원칙과 아키텍처는 [PlayerMovement-Design.md](./PlayerMovement-Design.md)를 참조하세요.

## 구현 세부사항

### 입력 시스템

**수평 이동:**

- **Input.GetAxis("Horizontal")**: Unity의 기본 Input Manager를 사용합니다.
- **입력 범위**: -1.0 (왼쪽) ~ 1.0 (오른쪽)
- **지원 키**: A/D, 좌/우 화살표 키

**점프:**

- **Input.GetKeyDown(KeyCode.Space)**: 점프 시작 (Jump Buffer에 저장)
- **Input.GetKeyUp(KeyCode.Space)**: 점프 키 해제 (가변 점프 처리)

### 이동 속도

- **Move Speed**: 기본 이동 속도 (기본값: 5.0)
- **Inspector에서 조정 가능**: 게임 디자인에 맞게 조정

### 리지드바디 2D(`Rigidbody2D`) 설정

#### Body Type: Dynamic (필수)

- **Dynamic 사용**: 물리 시뮬레이션에 참여하여 중력, 충돌 등이 자동으로 처리됩니다.
- **Kinematic 사용 금지**: Kinematic은 물리 시뮬레이션에 참여하지 않아 중력이 작동하지 않습니다.
- **이유**: 점프, 낙하, 충돌 등이 자연스럽게 작동해야 하므로 Dynamic이 필수입니다.

#### 기본 설정 값

- **Body Type**: `RigidbodyType2D.Dynamic`
- **Freeze Rotation**: `true` (회전 방지)
- **Gravity Scale**: `3.0` (기본값, Inspector에서 조정 가능)
  - 일반적으로 1~3 사이의 값을 사용
  - 더 빠른 낙하가 필요하면 높은 값 사용
  - 느린 낙하가 필요하면 낮은 값 사용

#### 코드에서 자동 설정

`PlayerPhysics` 클래스에서 자동으로 설정됩니다.

## 코드 구조

### PlayerController

입력 처리 및 게임플레이 로직을 담당합니다.

**주요 기능:**

- 수평 이동 입력 처리
- 점프 입력 처리 (점프 요청(`RequestJump`)/점프 키 해제(`ReleaseJump`))
- Model 자식 오브젝트 방향 반전 (스프라이트 좌우 전환)

```csharp
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;

    private PlayerPhysics _physics;
    private float _horizontalInput;
    private Transform _modelTransform;

    private void Awake()
    {
        _physics = GetComponent<PlayerPhysics>();
        if (_physics == null)
        {
            _physics = gameObject.AddComponent<PlayerPhysics>();
        }
        SetupModel();
    }

    private void SetupModel()
    {
        // "Model" 자식 오브젝트 찾기
        Transform modelChild = transform.Find("Model");
        if (modelChild != null)
        {
            _modelTransform = modelChild;
        }
    }

    // 입력 읽기 (Update)
    private void Update()
    {
        _horizontalInput = Input.GetAxis("Horizontal");

        // 점프 입력 감지
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _physics.RequestJump();
        }

        // 점프 키 해제 (가변 점프)
        if (Input.GetKeyUp(KeyCode.Space))
        {
            _physics.ReleaseJump();
        }

        // Model 스프라이트 방향 반전
        UpdateModelDirection();
    }

    private void UpdateModelDirection()
    {
        if (_modelTransform == null) return;
        if (Mathf.Abs(_horizontalInput) > 0.01f)
        {
            Vector3 scale = _modelTransform.localScale;
            scale.x = _horizontalInput > 0 ? 1f : -1f;
            _modelTransform.localScale = scale;
        }
    }

    // 물리 계산 (FixedUpdate)
    private void FixedUpdate()
    {
        float targetVelocityX = _horizontalInput * _moveSpeed;
        _physics.ApplyHorizontalVelocity(targetVelocityX);
    }
}
```

### PlayerPhysics

물리 설정 및 속도 적용을 담당합니다.

**주요 기능:**

- 리지드바디 2D(`Rigidbody2D`) 및 박스 콜라이더 2D(`BoxCollider2D`) 자동 설정
- 수평/수직/전체 속도 적용
- Raycast 기반 바닥 감지
- Coyote Time 및 Jump Buffer를 활용한 점프 시스템
- 가변 점프 (점프 키를 빨리 떼면 낮게 점프)
- 더블 점프 (공중 점프 횟수 기반, 기능 해금 시스템 연동)

```csharp
public class PlayerPhysics : MonoBehaviour
{
    [SerializeField] private Vector2 _colliderSize = new Vector2(0.5f, 1f);
    [SerializeField] private bool _autoSetupCollider = true;
    [SerializeField] private float _groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask _groundLayerMask = 1;
    [SerializeField] private float _coyoteTime = 0.2f;
    [SerializeField] private float _jumpForce = 15f;
    [SerializeField] private float _jumpBufferTime = 0.2f;
    [SerializeField] private float _jumpCutMultiplier = 0.5f;
    [SerializeField] private int _extraJumps = 0; // 공중 점프 횟수 (기본값: 0, 기능 해금 시 증가)

    private Rigidbody2D _rb;
    private BoxCollider2D _boxCollider;
    private bool _isGrounded;
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private bool _isJumping;
    private int _jumpCounter; // 현재 사용 가능한 점프 횟수

    public bool IsGrounded => _isGrounded;
    public bool IsJumping => _isJumping;
    
    // 남은 점프 횟수 (읽기 전용)
    public int RemainingJumps => _jumpCounter;
    
    // 현재 공중 점프 횟수 (읽기 전용)
    public int ExtraJumps => _extraJumps;

    private void Awake()
    {
        SetupRigidbody2D();
        if (_autoSetupCollider)
        {
            SetupCollider();
        }
        
        // 점프 카운터 초기화 (기본 점프 1 + 공중 점프 횟수)
        _jumpCounter = 1 + _extraJumps;
    }

    public void ApplyHorizontalVelocity(float velocityX)
    {
        _rb.linearVelocity = new Vector2(velocityX, _rb.linearVelocity.y);
    }

    public void RequestJump()
    {
        _jumpBufferCounter = _jumpBufferTime;
    }

    public void ReleaseJump()
    {
        // 위로 올라가는 중일 때만 속도 감소
        if (_rb.linearVelocity.y > 0f)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * _jumpCutMultiplier);
        }
    }

    // 공중 점프 횟수 설정 (기능 해금 시스템과 연동)
    public void SetExtraJumps(int count)
    {
        _extraJumps = Mathf.Max(0, count); // 음수 방지
        
        // 바닥에 있을 때만 카운터 리셋 (공중에서는 유지)
        if (_isGrounded)
        {
            _jumpCounter = 1 + _extraJumps;
        }
    }

    private void FixedUpdate()
    {
        CheckGrounded();

        // Jump Buffer 감소
        if (_jumpBufferCounter > 0f)
        {
            _jumpBufferCounter -= Time.fixedDeltaTime;
        }

        // 점프 실행 조건 체크
        if (_jumpBufferCounter > 0f && _jumpCounter > 0)
        {
            // 첫 번째 점프: 바닥에 있거나 Coyote Time 내
            if (_isGrounded)
            {
                ExecuteJump();
                // 첫 점프 후 남은 횟수는 공중 점프 횟수만
                _jumpCounter = _extraJumps;
            }
            // 공중 점프: 공중에 있고 공중 점프 횟수가 0보다 클 때
            else if (!_isGrounded && _extraJumps > 0 && _jumpCounter > 0)
            {
                ExecuteJump();
                _jumpCounter--; // 점프 카운터 감소
            }
        }
    }
}
```

**설정 가능한 필드:**

- `Collider Size`: 박스 콜라이더 2D(`BoxCollider2D`) 크기 (기본값: 0.5, 1)
- `Auto Setup Collider`: 자동 콜라이더 설정 여부
- `Ground Check Distance`: 바닥 감지 거리 (기본값: 0.1)
- `Ground Layer Mask`: 바닥 레이어 마스크
- `Coyote Time`: 바닥 이탈 후 점프 허용 시간 (기본값: 0.2초)
- `Jump Force`: 점프 힘 (기본값: 15.0)
- `Jump Buffer Time`: 점프 버퍼 시간 (기본값: 0.2초)
- `Jump Cut Multiplier`: 가변 점프 시 속도 감소 배율 (기본값: 0.5)
- `Extra Jumps`: 공중 점프 횟수 (기본값: 0, 기능 해금 시 1로 설정)

**공개 API:**

- `SetExtraJumps(int count)`: 공중 점프 횟수 설정 (기능 해금 시스템과 연동)
- `ExtraJumps { get; }`: 현재 공중 점프 횟수 조회
- `RemainingJumps { get; }`: 남은 점프 횟수 조회

## 점프 시스템 구현

점프 시스템은 `PlayerPhysics` 클래스에 구현되어 있습니다.

### 구현된 기능

- **바닥 감지**: Raycast 기반으로 바닥 감지
- **Coyote Time**: 바닥 이탈 후 0.2초 동안 점프 허용 (기본값)
- **Jump Buffer**: 점프 키를 미리 눌러도 착지 시 자동으로 점프 실행 (0.2초 버퍼)
- **가변 점프**: 점프 키를 빨리 떼면 낮게 점프 (Jump Cut Multiplier: 0.5)
- **즉각적인 반응**: 점프 힘을 속도로 직접 적용하여 지연 없음
- **더블 점프**: 공중 점프 횟수 기반으로 공중에서 추가 점프 가능 (기본값: 0, 기능 해금 시 1)

### 사용 방법

```csharp
// PlayerController에서 점프 입력 처리
if (Input.GetKeyDown(KeyCode.Space))
{
    _physics.RequestJump(); // Jump Buffer에 저장
}

if (Input.GetKeyUp(KeyCode.Space))
{
    _physics.ReleaseJump(); // 가변 점프 처리
}
```

### 기능 해금 시스템 연동

더블 점프 기능을 해금할 때 `SetExtraJumps()` 메서드를 사용합니다:

```csharp
// 기능 해금 시 더블 점프 활성화
playerPhysics.SetExtraJumps(1);

// 현재 상태 확인
int remaining = playerPhysics.RemainingJumps; // 남은 점프 횟수
int extra = playerPhysics.ExtraJumps; // 공중 점프 횟수

// 예시: 기능 해금 시스템과 연동
public class AbilitySystem : MonoBehaviour
{
    private PlayerPhysics _playerPhysics;
    
    public void UnlockDoubleJump()
    {
        _playerPhysics.SetExtraJumps(1);
        Debug.Log($"더블 점프 해금! 남은 점프: {_playerPhysics.RemainingJumps}");
    }
}
```

### 점프 카운터 동작 방식

- **초기 상태**: `_jumpCounter = 1 + _extraJumps`
  - `_extraJumps = 0`일 때: `_jumpCounter = 1` (일반 점프만 가능)
  - `_extraJumps = 1`일 때: `_jumpCounter = 2` (더블 점프 가능)
- **첫 번째 점프 실행 후**: `_jumpCounter = _extraJumps`
  - 더블 점프 가능한 경우: `_jumpCounter = 1` (공중 점프 1회 남음)
- **공중 점프 실행 시**: `_jumpCounter--`
- **바닥 착지 시**: `_jumpCounter = 1 + _extraJumps` (리셋)

## 디버깅 도구

### DebugLogger

NDJSON 형식으로 디버그 로그를 파일에 기록하는 유틸리티 클래스입니다.

**특징:**

- 에디터에서만 동작 (빌드에서는 빈 구현)
- 로그 파일 경로: `.cursor/debug.log`
- 세션 ID, 실행 ID, 가설 ID 지원

**사용 예시:**

```csharp
DebugLogger.Log(
    "PlayerPhysics.cs:100",
    "CheckGrounded entry",
    new { isGrounded = _isGrounded },
    "hypothesis-id"
);
```

### PlayerGroundedDebugger

플레이어의 바닥 감지 상태(`IsGrounded`)를 시각적으로 디버깅하기 위한 컴포넌트입니다.

**특징:**

- 에디터에서만 사용 (빌드에는 포함되지 않음)
- 바닥에 닿으면 녹색, 공중에 있으면 빨간색으로 스프라이트 색상 변경
- "Model" 자식 오브젝트의 SpriteRenderer 색상 변경

**설정:**

- `Grounded Color`: 바닥에 닿았을 때 색상 (기본값: 녹색)
- `Airborne Color`: 공중에 있을 때 색상 (기본값: 빨간색)
- `Model Child Name`: 스프라이트가 있는 자식 오브젝트 이름 (기본값: "Model")

## 환경 설정

### GroundSetup

바닥 오브젝트를 자동으로 설정하는 컴포넌트입니다.

**주요 기능:**

- "Collision" 레이어로 자동 설정
- Sorting Layer를 "Collision"으로 설정
- 리지드바디 2D(`Rigidbody2D`)를 Static으로 설정
- 박스 콜라이더 2D(`BoxCollider2D`) 자동 설정

**설정 가능한 필드:**

- `Collider Size`: 박스 콜라이더 2D(`BoxCollider2D`) 크기 (기본값: 10, 0.5)
- `Auto Setup Collider`: 자동 콜라이더 설정 여부

**사용 방법:**

- 바닥 오브젝트에 `GroundSetup` 컴포넌트 추가
- Inspector에서 설정 조정
- 또는 우클릭 → "Setup Ground" 메뉴로 수동 설정

## 주의사항

1. **입력은 Update에서만 읽기**: FixedUpdate에서 Input을 직접 읽으면 입력을 놓칠 수 있습니다.
2. **물리는 FixedUpdate에서 처리**: 리지드바디 2D(`Rigidbody2D`) 조작은 고정된 시간 간격에서 처리해야 일관성이 유지됩니다.
3. **가속/감속 사용 금지**: 나인솔즈 스타일의 즉각적인 반응성을 유지하기 위해 가속/감속을 사용하지 않습니다.

## 테스트 체크리스트

### 기본 이동

- [ ] 키 입력에 즉시 반응하는가?
- [ ] 키를 떼면 즉시 멈추는가?
- [ ] Y축 속도(중력, 점프)가 정상적으로 작동하는가?
- [ ] 벽과의 충돌이 정상적으로 처리되는가?
- [ ] 이동 속도가 게임 디자인에 적합한가?

### 점프 시스템

- [ ] 바닥에서 점프가 정상적으로 작동하는가?
- [ ] Coyote Time이 정상적으로 작동하는가? (바닥 이탈 후 짧은 시간 동안 점프 가능)
- [ ] Jump Buffer가 정상적으로 작동하는가? (점프 키를 미리 눌러도 착지 시 점프)
- [ ] 가변 점프가 정상적으로 작동하는가? (점프 키를 빨리 떼면 낮게 점프)

### 더블 점프 시스템

- [ ] 공중 점프 횟수가 0일 때 공중에서 점프가 불가능한가?
- [ ] `SetExtraJumps(1)` 호출 후 더블 점프가 가능한가?
- [ ] 더블 점프 실행 후 점프 카운터가 정상적으로 감소하는가?
- [ ] 바닥 착지 시 점프 카운터가 정상적으로 리셋되는가?
- [ ] `RemainingJumps`와 `ExtraJumps` 프로퍼티가 정상적으로 동작하는가?
- [ ] 공중에서 더블 점프 후 추가 점프가 불가능한가? (트리플 점프 미구현)

### 시각적 피드백

- [ ] Model 스프라이트가 이동 방향에 따라 반전되는가?
- [ ] PlayerGroundedDebugger가 정상적으로 색상을 변경하는가? (에디터에서만)
