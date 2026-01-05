using UnityEngine;
using System.Linq;

public class PlayerPhysics : MonoBehaviour
{
    [SerializeField] private Vector2 _colliderSize = new Vector2(0.5f, 1f);
    [SerializeField] private bool _autoSetupCollider = true;
    [SerializeField] private float _groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask _groundLayerMask = 1;
    [SerializeField] private float _coyoteTime = 0.2f;

    // 점프 관련 파라미터
    [Header("Jump")]
    [SerializeField] private float _jumpForce = 15f;
    [SerializeField] private float _jumpBufferTime = 0.2f;
    [SerializeField] private float _jumpCutMultiplier = 0.5f;
    [SerializeField] private int _extraJumps = 0; // 공중 점프 횟수 (기본값: 0, 기능 해금 시 증가)

    // 대시 관련 파라미터
    [Header("Dash")]
    [SerializeField] private float _dashDistance = 2f; // 대시 거리
    [SerializeField] private float _dashDuration = 0.2f; // 대시 지속 시간
    [SerializeField] private float _dashCooldown = 0.5f; // 대시 쿨타임
    [SerializeField] private bool _airDashEnabled = false; // 공중 대시 활성화 여부

    // 대시 관련 상수
    private const float DASH_VELOCITY_Y = 0f; // 대시 중 Y축 속도 고정값

    // 아래 점프 관련 파라미터
    [Header("Down Jump")]
    [SerializeField] private float _downJumpForce = -20f; // 아래 점프 힘 (음수 값)
    [SerializeField] private float _downJumpPlatformIgnoreTime = 0.3f; // One-way platform 충돌 무시 시간

    // 충돌 감지 관련 파라미터
    [Header("Collision Detection")]
    [SerializeField] private float _skinWidth = 0.03f; // Raycast 간격을 위한 여유 공간
    [SerializeField][Range(2, 8)] private int _horizontalRayCount = 4; // 수평 Raycast 개수
    [SerializeField][Range(2, 8)] private int _verticalRayCount = 4; // 수직 Raycast 개수

    private Rigidbody2D _rb;
    private BoxCollider2D _boxCollider;

    private bool _isGrounded;
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private bool _isJumping;
    private int _jumpCounter; // 현재 사용 가능한 점프 횟수

    // 대시 관련 필드
    private bool _isDashing;
    private float _dashCooldownCounter;
    private float _dashDurationCounter;
    private Vector2 _dashDirection;
    private bool _wasGroundedBeforeDash; // 대시 시작 시 지상 상태 저장

    // 아래 점프 관련 필드
    private bool _isIgnoringPlatforms; // One-way platform 충돌 무시 중
    private float _platformIgnoreTimer; // One-way platform 충돌 무시 타이머

    // 충돌 감지 관련 필드
    private RaycastOrigins _raycastOrigins;
    private PlayerCollisionInfo _collisionInfo;
    private float _horizontalRaySpacing;
    private float _verticalRaySpacing;

    // 원본 bounds (양 끝 레이캐스트용)
    private Bounds _originalBounds;

    public bool IsGrounded => _isGrounded;
    public bool IsJumping => _isJumping;
    public bool IsOnOneWayPlatform { get; private set; }

    // 남은 점프 횟수 (읽기 전용)
    public int RemainingJumps => _jumpCounter;

    // 현재 공중 점프 횟수 (읽기 전용)
    public int ExtraJumps => _extraJumps;

    // 대시 관련 프로퍼티
    public bool IsDashing => _isDashing;

    public float DashCooldownRemaining => _dashCooldownCounter;
    public bool CanDash => _dashCooldownCounter <= 0f && (!_isDashing);
    public bool IsAirDashEnabled => _airDashEnabled;

    // Rigidbody2D 속도 (읽기 전용)
    public Vector2 RigidbodyVelocity => _rb != null ? _rb.linearVelocity : Vector2.zero;

    // 충돌 감지 상태 프로퍼티
    public bool IsLeftCollision => _collisionInfo.left;
    public bool IsRightCollision => _collisionInfo.right;
    public bool IsCeiling => _collisionInfo.above;
    public bool IsCollideX => _collisionInfo.left || _collisionInfo.right;
    public bool IsCollideY => _collisionInfo.above || _collisionInfo.below;

    private void Awake()
    {
        SetupRigidbody2D();

        if (_autoSetupCollider)
        {
            SetupCollider();
        }

        // 점프 카운터 초기화 (기본 점프 1 + 공중 점프 횟수)
        _jumpCounter = 1 + _extraJumps;

        // 충돌 감지 시스템 초기화
        _collisionInfo.Reset();
        CalculateRaySpacing();
    }

    private void SetupRigidbody2D()
    {
        _rb = GetComponent<Rigidbody2D>();

        // Rigidbody2D가 없으면 추가
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Dynamic Body Type 설정 (물리 시뮬레이션에 참여)
        _rb.bodyType = RigidbodyType2D.Dynamic;

        // 회전 방지 (플랫포머에서 중요)
        _rb.freezeRotation = true;

        // 중력 스케일은 Inspector에서 조정 가능 (기본값: 3)
        _rb.gravityScale = 3f;
    }

    private void SetupCollider()
    {
        _boxCollider = GetComponent<BoxCollider2D>();

        // BoxCollider2D가 없으면 추가
        if (_boxCollider == null)
        {
            _boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        // 콜라이더 크기 설정
        _boxCollider.size = _colliderSize;

        // Is Trigger는 false (실제 충돌 필요)
        _boxCollider.isTrigger = false;
    }

    // 수평 속도만 적용 (Y축 속도 유지)
    public void ApplyHorizontalVelocity(float velocityX)
    {
        if (_rb == null) return;
        _rb.linearVelocity = new Vector2(velocityX, _rb.linearVelocity.y);
    }

    // 수직 속도만 적용 (X축 속도 유지)
    public void ApplyVerticalVelocity(float velocityY)
    {
        if (_rb == null) return;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, velocityY);
    }

    // 전체 속도 적용
    public void ApplyVelocity(Vector2 velocity)
    {
        if (_rb == null) return;
        _rb.linearVelocity = velocity;
    }

    /// <summary>
    /// 점프 요청을 받습니다. Jump Buffer에 저장되고, 다음 FixedUpdate에서 조건을 체크하여 실행됩니다.
    /// (상태 머신 사용 시에는 ExecuteJumpIfPossible()를 직접 호출하는 것을 권장)
    /// </summary>
    public void RequestJump()
    {
        _jumpBufferCounter = _jumpBufferTime;
    }

    /// <summary>
    /// 점프 버퍼와 조건을 체크하여 점프를 실행합니다. (상태 머신에서 사용)
    /// </summary>
    public void ExecuteJumpIfPossible()
    {
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

    /// <summary>
    /// 점프 키를 떼면 가변 점프 처리
    /// </summary>
    public void ReleaseJump()
    {
        if (_rb == null) return;

        // 위로 올라가는 중일 때만 속도 감소
        if (_rb.linearVelocity.y > 0f)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * _jumpCutMultiplier);
        }
    }

    /// <summary>
    /// 실제 착지 시 점프 카운터 리셋 (단방향 플랫폼 통과 시에는 호출하지 않음)
    /// </summary>
    public void ResetJumpCounterOnLanding()
    {
        _jumpCounter = 1 + _extraJumps;
    }

    /// <summary>
    /// 공중 점프 횟수 설정 (기능 해금 시스템과 연동)
    /// </summary>
    public void SetExtraJumps(int count)
    {
        _extraJumps = Mathf.Max(0, count); // 음수 방지

        // 바닥에 있을 때만 카운터 리셋 (공중에서는 유지)
        if (_isGrounded)
        {
            _jumpCounter = 1 + _extraJumps;
        }
    }

    /// <summary>
    /// 대시 요청 (쿨타임 및 조건 확인 후 실행)
    /// </summary>
    public void RequestDash(Vector2 direction)
    {
        // 쿨타임 확인
        if (!CanDash) return;

        // 지상 대시인 경우 바닥 감지 확인
        if (!_airDashEnabled && !_isGrounded) return;

        // 대시 실행
        ExecuteDash(direction);
    }

    /// <summary>
    /// 공중 대시 활성화 여부 설정 (기능 해금 시스템과 연동)
    /// </summary>
    public void SetAirDashEnabled(bool enabled)
    {
        _airDashEnabled = enabled;
    }

    /// <summary>
    /// 아래 점프 요청 (One-way platform 통과 및 아래로 점프)
    /// </summary>
    public void RequestDownJump()
    {
        if (_rb == null) return;

        // 지면에 닿아있을 때만 아래 점프 가능
        if (!_isGrounded) return;

        // 아래 점프 실행
        ExecuteDownJump();
    }

    /// <summary>
    /// 점프 실행 (상태 머신에서 직접 호출 가능)
    /// </summary>
    public void ExecuteJump()
    {
        if (_rb == null) return;

        // Y축 속도를 점프 힘으로 설정 (즉각적인 반응)
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);

        // 점프 후 Coyote Time과 Jump Buffer 초기화
        _coyoteTimeCounter = 0f;
        _jumpBufferCounter = 0f;
        _isJumping = true;

        // 점프 카운터 관리
        if (_isGrounded)
        {
            // 지상 점프: 첫 점프 후 남은 횟수는 공중 점프 횟수만
            _jumpCounter = _extraJumps;
        }
        else
        {
            // 공중 점프: 카운터 감소
            if (_jumpCounter > 0)
            {
                _jumpCounter--;
            }
        }
    }

    /// <summary>
    /// 아래 점프 실행 (내부 메서드)
    /// </summary>
    private void ExecuteDownJump()
    {
        if (_rb == null) return;

        // 아래로 점프 힘 적용 (음수 값이므로 아래로 이동)
        float currentVelocityY = _rb.linearVelocity.y;
        float newVelocityY = Mathf.Min(currentVelocityY, _downJumpForce);
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, newVelocityY);

        // One-way platform 충돌 무시 시작
        _isIgnoringPlatforms = true;
        _platformIgnoreTimer = _downJumpPlatformIgnoreTime;

        // 플레이어 아래에 있는 One-way platform 찾아서 일시적으로 충돌 비활성화
        IgnoreOneWayPlatformsBelow();
    }

    /// <summary>
    /// 플레이어 아래에 있는 One-way platform과의 충돌을 일시적으로 무시
    /// </summary>
    private void IgnoreOneWayPlatformsBelow()
    {
        if (_boxCollider == null) return;

        // 플레이어 콜라이더 하단에서 아래로 Raycast
        Vector2 boxSize = _boxCollider.size;
        Vector2 boxCenter = _boxCollider.bounds.center;
        Vector2 rayOrigin = new Vector2(boxCenter.x, boxCenter.y - boxSize.y * 0.5f - 0.05f);
        float checkDistance = 2f; // 충분한 거리 체크

        // 모든 One-way platform 찾기
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, Vector2.down, checkDistance, _groundLayerMask);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider == _boxCollider) continue;

            // OneWayPlatform 컴포넌트가 있는지 확인
            OneWayPlatform oneWayPlatform = hit.collider.GetComponent<OneWayPlatform>();
            if (oneWayPlatform != null)
            {
                // One-way platform의 충돌을 일시적으로 비활성화
                oneWayPlatform.DisableCollisionTemporarily(_downJumpPlatformIgnoreTime);
            }
        }
    }

    /// <summary>
    /// 대시 실행 (내부 메서드)
    /// </summary>
    private void ExecuteDash(Vector2 direction)
    {
        if (_rb == null) return;

        // 대시 방향 정규화
        if (direction.magnitude < 0.01f)
        {
            direction = Vector2.right; // 기본값: 오른쪽
        }
        else
        {
            direction.Normalize();
        }

        // 대시 속도 계산 (거리 / 시간)
        float dashSpeed = _dashDistance / _dashDuration;

        // 대시 방향 저장
        _dashDirection = direction;

        // 대시 속도 적용 (Y축 속도는 0으로 고정)
        _rb.linearVelocity = new Vector2(_dashDirection.x * dashSpeed, DASH_VELOCITY_Y);

        // 대시 상태 플래그 설정
        _isDashing = true;
        _dashDurationCounter = _dashDuration;
        _wasGroundedBeforeDash = _isGrounded;

        // 쿨타임 타이머 초기화
        _dashCooldownCounter = _dashCooldown;
    }

    public void PhysisUpdate()
    {
        // 충돌 감지 (다중 Raycast 기반)
        CheckCollisions();

        // 대시 처리
        if (_isDashing)
        {
            // 대시 지속 시간 감소
            _dashDurationCounter -= Time.fixedDeltaTime;

            // 대시 속도 유지 (일반 이동 입력 무시, Y축 속도는 0으로 고정)
            if (_rb != null)
            {
                float dashSpeed = _dashDistance / _dashDuration;
                // Y축 속도를 0으로 고정
                _rb.linearVelocity = new Vector2(_dashDirection.x * dashSpeed, DASH_VELOCITY_Y);
            }

            // 대시 지속 시간 종료 시 대시 종료
            if (_dashDurationCounter <= 0f)
            {
                _isDashing = false;
                _dashDirection = Vector2.zero;
            }
        }

        // 대시 쿨타임 감소
        if (_dashCooldownCounter > 0f)
        {
            _dashCooldownCounter -= Time.fixedDeltaTime;
        }

        // One-way platform 충돌 무시 타이머 감소
        if (_isIgnoringPlatforms)
        {
            _platformIgnoreTimer -= Time.fixedDeltaTime;
            if (_platformIgnoreTimer <= 0f)
            {
                _isIgnoringPlatforms = false;
            }
        }

        // Jump Buffer 감소
        if (_jumpBufferCounter > 0f)
        {
            _jumpBufferCounter -= Time.fixedDeltaTime;
        }

        // 점프 실행은 상태 머신에서 처리하므로 여기서는 제거
        // (기존 호환성을 위해 RequestJump()로 버퍼에 저장된 점프는 ExecuteJumpIfPossible()로 처리 가능)
    }

    #region 충돌 감지 시스템

    /// <summary>
    /// Raycast 원점을 업데이트합니다.
    /// 양 끝은 콜라이더의 정확한 끝에서, 가운데는 skinWidth를 적용한 위치에서 레이캐스트를 발사합니다.
    /// </summary>
    private void UpdateRaycastOrigins()
    {
        if (_boxCollider == null) return;

        // 원본 bounds 저장 (양 끝 레이캐스트용)
        _originalBounds = _boxCollider.bounds;

        // innerBounds (가운데 레이캐스트용)
        Bounds innerBounds = _boxCollider.bounds;
        innerBounds.Expand(_skinWidth * -2);

        // 양 끝은 원본 bounds 사용 (정확한 콜라이더 끝)
        _raycastOrigins.bottomLeft = new Vector2(_originalBounds.min.x, _originalBounds.min.y);
        _raycastOrigins.bottomRight = new Vector2(_originalBounds.max.x, _originalBounds.min.y);
        _raycastOrigins.topLeft = new Vector2(_originalBounds.min.x, _originalBounds.max.y);
        _raycastOrigins.topRight = new Vector2(_originalBounds.max.x, _originalBounds.max.y);

        // 가운데는 innerBounds 사용
        _raycastOrigins.top = new Vector2(innerBounds.center.x, _originalBounds.max.y);
        _raycastOrigins.bottom = new Vector2(innerBounds.center.x, _originalBounds.min.y);
        _raycastOrigins.left = new Vector2(_originalBounds.min.x, innerBounds.center.y);
        _raycastOrigins.right = new Vector2(_originalBounds.max.x, innerBounds.center.y);
    }

    /// <summary>
    /// Raycast 간격을 계산합니다.
    /// 콜라이더 크기에 따라 수평/수직 Raycast 간격을 자동으로 계산합니다.
    /// 가운데 레이캐스트 간격은 innerBounds 기준으로 계산합니다.
    /// </summary>
    private void CalculateRaySpacing()
    {
        if (_boxCollider == null) return;

        // innerBounds 기준으로 간격 계산 (가운데 레이캐스트용)
        Bounds innerBounds = _boxCollider.bounds;
        innerBounds.Expand(_skinWidth * -2);

        float innerBoundsWidth = innerBounds.size.x;
        float innerBoundsHeight = innerBounds.size.y;

        _horizontalRayCount = Mathf.Clamp(_horizontalRayCount, 1, int.MaxValue);
        _verticalRayCount = Mathf.Clamp(_verticalRayCount, 1, int.MaxValue);

        // 간격 계산 (양 끝을 제외한 중간 레이캐스트 간격)
        if (_horizontalRayCount > 2)
        {
            _horizontalRaySpacing = innerBoundsHeight / (_horizontalRayCount - 1);
        }
        else
        {
            _horizontalRaySpacing = 0f; // 1개 또는 2개일 때는 간격 불필요
        }

        if (_verticalRayCount > 2)
        {
            _verticalRaySpacing = innerBoundsWidth / (_verticalRayCount - 1);
        }
        else
        {
            _verticalRaySpacing = 0f; // 1개 또는 2개일 때는 간격 불필요
        }
    }

    /// <summary>
    /// 수직 Raycast 원점을 계산합니다.
    /// 양 끝은 Unity의 Default Contact Offset을 고려하여 콜라이더 바깥쪽으로 확장하고,
    /// 가운데는 skinWidth를 적용한 위치에서 레이캐스트를 발사합니다.
    /// 지면 감지 시 원점을 약간 위로 이동하여 안정성을 확보합니다.
    /// </summary>
    /// <param name="index">레이캐스트 인덱스 (0부터 시작)</param>
    /// <param name="isTop">천장 감지 여부 (true: 천장, false: 지면)</param>
    /// <returns>레이캐스트 원점</returns>
    private Vector2 GetVerticalRayOrigin(int index, bool isTop)
    {
        if (_boxCollider == null) return Vector2.zero;

        // innerBounds 계산 (가운데 레이캐스트용)
        Bounds innerBounds = _boxCollider.bounds;
        innerBounds.Expand(_skinWidth * -2);

        // Unity 물리 엔진의 Default Contact Offset을 고려하여 양끝 레이캐스트 위치 조정
        float contactOffset = Physics2D.defaultContactOffset;

        // 지면 감지 시 원점을 약간 위로 이동하여 안정성 확보
        // (콜라이더 내부에서 시작하여 플랫폼과 겹치는 상황에서도 감지 가능)
        float yPos = isTop ? _originalBounds.max.y : _originalBounds.min.y + _skinWidth;

        // 양끝 레이캐스트는 Contact Offset만큼 바깥쪽으로 확장
        float leftEdge = _originalBounds.min.x - contactOffset;
        float rightEdge = _originalBounds.max.x + contactOffset;

        if (_verticalRayCount == 1)
        {
            // 1개: 가운데 (innerBounds 기준)
            return new Vector2(innerBounds.center.x, yPos);
        }
        else if (_verticalRayCount == 2)
        {
            // 2개: 양 끝 (Contact Offset을 고려한 확장된 bounds)
            if (index == 0)
            {
                return new Vector2(leftEdge, yPos);
            }
            else
            {
                return new Vector2(rightEdge, yPos);
            }
        }
        else
        {
            // 3개 이상: 양 끝(Contact Offset 확장) + 가운데(innerBounds 기준 균등 간격)
            if (index == 0)
            {
                // 왼쪽 끝: Contact Offset을 고려한 확장된 bounds
                return new Vector2(leftEdge, yPos);
            }
            else if (index == _verticalRayCount - 1)
            {
                // 오른쪽 끝: Contact Offset을 고려한 확장된 bounds
                return new Vector2(rightEdge, yPos);
            }
            else
            {
                // 가운데: innerBounds 기준으로 균등 간격
                float innerWidth = innerBounds.size.x;
                float spacing = innerWidth / (_verticalRayCount - 1);
                return new Vector2(innerBounds.min.x + spacing * index, yPos);
            }
        }
    }

    /// <summary>
    /// 수평 Raycast 원점을 계산합니다.
    /// 양 끝은 Unity의 Default Contact Offset을 고려하여 콜라이더 바깥쪽으로 확장하고,
    /// 가운데는 skinWidth를 적용한 위치에서 레이캐스트를 발사합니다.
    /// </summary>
    /// <param name="index">레이캐스트 인덱스 (0부터 시작)</param>
    /// <param name="isLeft">왼쪽 방향 여부 (true: 왼쪽, false: 오른쪽)</param>
    /// <returns>레이캐스트 원점</returns>
    private Vector2 GetHorizontalRayOrigin(int index, bool isLeft)
    {
        if (_boxCollider == null) return Vector2.zero;

        // innerBounds 계산 (가운데 레이캐스트용)
        Bounds innerBounds = _boxCollider.bounds;
        innerBounds.Expand(_skinWidth * -2);

        // Unity 물리 엔진의 Default Contact Offset을 고려하여 양끝 레이캐스트 위치 조정
        float contactOffset = Physics2D.defaultContactOffset;

        // 수평 레이캐스트는 Contact Offset만큼 바깥쪽으로 확장
        float xPos = isLeft ? _originalBounds.min.x - contactOffset : _originalBounds.max.x + contactOffset;

        // 양끝 레이캐스트는 Contact Offset만큼 바깥쪽으로 확장
        float bottomEdge = _originalBounds.min.y - contactOffset;
        float topEdge = _originalBounds.max.y + contactOffset;

        if (_horizontalRayCount == 1)
        {
            // 1개: 가운데 (innerBounds 기준)
            return new Vector2(xPos, innerBounds.center.y);
        }
        else if (_horizontalRayCount == 2)
        {
            // 2개: 양 끝 (Contact Offset을 고려한 확장된 bounds)
            if (index == 0)
            {
                return new Vector2(xPos, bottomEdge);
            }
            else
            {
                return new Vector2(xPos, topEdge);
            }
        }
        else
        {
            // 3개 이상: 양 끝(Contact Offset 확장) + 가운데(innerBounds 기준 균등 간격)
            if (index == 0)
            {
                // 아래 끝: Contact Offset을 고려한 확장된 bounds
                return new Vector2(xPos, bottomEdge);
            }
            else if (index == _horizontalRayCount - 1)
            {
                // 위 끝: Contact Offset을 고려한 확장된 bounds
                return new Vector2(xPos, topEdge);
            }
            else
            {
                // 가운데: innerBounds 기준으로 균등 간격
                float innerHeight = innerBounds.size.y;
                float spacing = innerHeight / (_horizontalRayCount - 1);
                return new Vector2(xPos, innerBounds.min.y + spacing * index);
            }
        }
    }

    /// <summary>
    /// 수직 충돌 감지 (지면/천장)
    /// 다중 Raycast를 사용하여 정밀하게 지면과 천장을 감지합니다.
    /// </summary>
    private void CheckVerticalCollisions()
    {
        if (_boxCollider == null || _rb == null) return;

        float velocityY = _rb.linearVelocity.y;

        // 지면 감지: 낙하 중이거나 정지 상태일 때 아래로 체크
        float checkDistanceDown = _groundCheckDistance;
        if (velocityY <= 0f)
        {
            // 낙하 중일 때는 속도에 따라 거리 조정
            checkDistanceDown = Mathf.Max(_groundCheckDistance, Mathf.Abs(velocityY * Time.fixedDeltaTime) + _skinWidth);
        }

        // 최소 거리 보장 (아슬아슬한 접촉도 감지하기 위해)
        // 원점이 skinWidth만큼 위로 이동했으므로, 그만큼 더 쏴야 함
        checkDistanceDown = Mathf.Max(checkDistanceDown, _skinWidth * 2f);

        // 천장 감지: 상승 중일 때 위로 체크
        float checkDistanceUp = 0f;
        if (velocityY > 0f)
        {
            checkDistanceUp = Mathf.Abs(velocityY * Time.fixedDeltaTime) + _skinWidth;
        }

        bool wasGrounded = _collisionInfo.below;
        _collisionInfo.below = false;
        _collisionInfo.above = false;
        IsOnOneWayPlatform = false;

        // 지면 감지 (아래로)
        for (int i = 0; i < _verticalRayCount; i++)
        {
            Vector2 rayOrigin = GetVerticalRayOrigin(i, false);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, checkDistanceDown, _groundLayerMask);

            // 디버그용 레이 그리기
            Debug.DrawRay(rayOrigin, Vector2.down * checkDistanceDown, hit.collider != null ? Color.red : Color.yellow);

            if (hit.collider != null)
            {
                // 자신의 콜라이더는 무시
                if (hit.collider == _boxCollider || hit.collider.gameObject == gameObject)
                {
                    continue;
                }

                // One-way platform 처리
                OneWayPlatform oneWayPlatform = hit.collider.GetComponent<OneWayPlatform>();
                bool isOneWayPlatform = oneWayPlatform != null;
                float currentVelocityY = _rb.linearVelocity.y;

                if (isOneWayPlatform)
                {
                    // 아래로 내려가는 중이면 통과
                    if (currentVelocityY < 0f)
                    {
                        continue;
                    }
                }

                // One-way platform을 통과하는 중이면 실제 착지로 간주하지 않음
                bool isActuallyGrounded = !isOneWayPlatform || currentVelocityY >= 0f;

                if (isActuallyGrounded)
                {
                    _collisionInfo.below = true;
                    IsOnOneWayPlatform = isOneWayPlatform;
                    break; // 첫 번째 충돌만 처리
                }
            }
        }

        // 천장 감지 (위로) - 상승 중일 때만 체크
        if (checkDistanceUp > 0f)
        {
            for (int i = 0; i < _verticalRayCount; i++)
            {
                Vector2 rayOrigin = GetVerticalRayOrigin(i, true);

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, checkDistanceUp, _groundLayerMask);

                // 디버그용 레이 그리기
                Debug.DrawRay(rayOrigin, Vector2.up * checkDistanceUp, hit.collider != null ? Color.red : Color.yellow);

                if (hit.collider != null)
                {
                    // 자신의 콜라이더는 무시
                    if (hit.collider == _boxCollider || hit.collider.gameObject == gameObject)
                    {
                        continue;
                    }

                    // One-way platform은 천장에서 통과
                    OneWayPlatform oneWayPlatform = hit.collider.GetComponent<OneWayPlatform>();
                    if (oneWayPlatform != null)
                    {
                        continue;
                    }

                    _collisionInfo.above = true;
                    break; // 첫 번째 충돌만 처리
                }
            }
        }

        // 지면 상태 업데이트
        if (_collisionInfo.below)
        {
            _isGrounded = true;
            _coyoteTimeCounter = _coyoteTime;
            _isJumping = false;

            // 착지 시 점프 카운터 리셋 (상태 머신에서 호출하는 ResetJumpCounterOnLanding과 별도)
            if (!wasGrounded)
            {
                // 상태 머신에서 ResetJumpCounterOnLanding을 호출할 때까지 대기
            }
        }
        else
        {
            _coyoteTimeCounter -= Time.fixedDeltaTime;
            _isGrounded = _coyoteTimeCounter > 0f;
        }
    }

    /// <summary>
    /// 수평 충돌 감지 (벽 충돌)
    /// 다중 Raycast를 사용하여 좌우 벽 충돌을 감지합니다.
    /// </summary>
    private void CheckHorizontalCollisions()
    {
        if (_boxCollider == null || _rb == null) return;

        float velocityX = _rb.linearVelocity.x;

        // 이동하지 않으면 충돌 감지 불필요
        if (Mathf.Abs(velocityX) < 0.01f)
        {
            _collisionInfo.left = false;
            _collisionInfo.right = false;
            _collisionInfo.faceDir = 1; // 기본값: 오른쪽
            return;
        }

        // 이동 방향 결정
        float directionX = Mathf.Sign(velocityX);
        _collisionInfo.faceDir = (int)directionX;

        // Raycast 거리 계산 (이동 속도에 따라)
        float rayLength = Mathf.Abs(velocityX * Time.fixedDeltaTime) + _skinWidth;

        // 최소 거리 보장
        if (rayLength < _skinWidth * 2f)
        {
            rayLength = _skinWidth * 2f;
        }

        _collisionInfo.left = false;
        _collisionInfo.right = false;

        for (int i = 0; i < _horizontalRayCount; i++)
        {
            Vector2 rayOrigin = GetHorizontalRayOrigin(i, directionX == -1);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, _groundLayerMask);

            // 디버그용 레이 그리기
            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, hit.collider != null ? Color.red : Color.yellow);

            if (hit.collider != null)
            {
                // 자신의 콜라이더는 무시
                if (hit.collider == _boxCollider || hit.collider.gameObject == gameObject)
                {
                    continue;
                }

                // One-way platform은 수평 충돌에서 무시 (위에서 아래로만 통과)
                OneWayPlatform oneWayPlatform = hit.collider.GetComponent<OneWayPlatform>();
                if (oneWayPlatform != null)
                {
                    continue;
                }

                // 충돌 방향 설정
                if (directionX == -1)
                {
                    _collisionInfo.left = true;
                }
                else
                {
                    _collisionInfo.right = true;
                }

                break; // 첫 번째 충돌만 처리
            }
        }
    }

    /// <summary>
    /// 통합 충돌 감지 메서드
    /// 다중 Raycast를 사용하여 정밀한 충돌 감지를 수행합니다.
    /// </summary>
    public void CheckCollisions()
    {
        if (_boxCollider == null) return;

        UpdateRaycastOrigins();
        _collisionInfo.Reset();

        // 수직 충돌 감지 (지면/천장)
        CheckVerticalCollisions();

        // 수평 충돌 감지 (벽)
        CheckHorizontalCollisions();
    }

    #endregion
}