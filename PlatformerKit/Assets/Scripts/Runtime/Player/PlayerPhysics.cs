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
    [SerializeField] private float _jumpForce = 15f;
    [SerializeField] private float _jumpBufferTime = 0.2f;
    [SerializeField] private float _jumpCutMultiplier = 0.5f;

    private Rigidbody2D _rb;
    private BoxCollider2D _boxCollider;

    private bool _isGrounded;
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private bool _isJumping;

    public bool IsGrounded => _isGrounded;
    public bool IsJumping => _isJumping;

    private void Awake()
    {
        SetupRigidbody2D();

        if (_autoSetupCollider)
        {
            SetupCollider();
        }
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

    // 바닥 감지
    public void CheckGrounded()
    {
        // #region agent log
        DebugLogger.Log(
            "PlayerPhysics.cs:89",
            "CheckGrounded entry",
            new { boxColliderNull = _boxCollider == null, previousIsGrounded = _isGrounded, coyoteTimeCounter = _coyoteTimeCounter },
            "A,B,C,D,E"
        );
        // #endregion

        if (_boxCollider == null) return;

        // BoxCollider의 하단 중앙에서 아래로 Raycast
        Vector2 boxSize = _boxCollider.size;
        Vector2 boxCenter = _boxCollider.bounds.center;
        // 콜라이더 하단 경계에서 충분히 아래로 이동 (자신의 콜라이더를 피하기 위해)
        // boxSize.y * 0.5f는 하단 경계, 추가로 0.05f를 더 내려서 자신의 콜라이더와 겹치지 않도록 함
        Vector2 rayOrigin = new Vector2(boxCenter.x, boxCenter.y - boxSize.y * 0.5f - 0.05f);

        // #region agent log
        DebugLogger.Log(
            "PlayerPhysics.cs:106",
            "Raycast parameters",
            new
            {
                boxSize = new { x = boxSize.x, y = boxSize.y },
                boxCenter = new { x = boxCenter.x, y = boxCenter.y },
                rayOrigin = new { x = rayOrigin.x, y = rayOrigin.y },
                groundCheckDistance = _groundCheckDistance,
                groundLayerMask = _groundLayerMask.value
            },
            "A"
        );
        // #endregion

        // 자신의 콜라이더를 제외하기 위해 Raycast 사용
        // Physics2D.Raycast는 자신의 콜라이더를 자동으로 제외하지 않으므로,
        // 모든 콜라이더를 체크한 후 자신의 것을 필터링
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, _groundCheckDistance, _groundLayerMask);

        // #region agent log - Raycast 전 모든 콜라이더 체크
        RaycastHit2D[] allHits = Physics2D.RaycastAll(rayOrigin, Vector2.down, _groundCheckDistance);
        DebugLogger.Log(
            "PlayerPhysics.cs:125",
            "RaycastAll results (before filtering)",
            new
            {
                totalHits = allHits.Length,
                hits = allHits.Select((h, i) => new
                {
                    index = i,
                    colliderName = h.collider != null ? h.collider.name : "null",
                    colliderGameObject = h.collider != null ? h.collider.gameObject.name : "null",
                    colliderLayer = h.collider != null ? h.collider.gameObject.layer : -1,
                    distance = h.distance,
                    isSelf = h.collider != null && (h.collider.gameObject == gameObject || h.collider == _boxCollider)
                }).ToArray()
            },
            "G"
        );
        // #endregion

        // 자신의 콜라이더를 감지한 경우 무시 (안전장치)
        bool isSelfCollision = false;
        if (hit.collider != null && (hit.collider.gameObject == gameObject || hit.collider == _boxCollider))
        {
            isSelfCollision = true;
            hit = new RaycastHit2D();
        }

        // #region agent log
        DebugLogger.Log(
            "PlayerPhysics.cs:122",
            "Self-collision check",
            new
            {
                isSelfCollision = isSelfCollision,
                hitColliderName = hit.collider != null ? hit.collider.name : "null"
            },
            "F"
        );
        // #endregion

        // #region agent log
        var hitName = hit.collider != null ? hit.collider.name : "null";
        var hitGameObject = hit.collider != null ? hit.collider.gameObject.name : "null";
        var hitPointX = hit.collider != null ? hit.point.x : 0;
        var hitPointY = hit.collider != null ? hit.point.y : 0;
        DebugLogger.Log(
            "PlayerPhysics.cs:140",
            "Raycast result",
            new
            {
                hitCollider = hit.collider != null,
                hitColliderName = hitName,
                hitColliderGameObject = hitGameObject,
                hitDistance = hit.distance,
                hitPoint = new { x = hitPointX, y = hitPointY }
            },
            "B"
        );
        // #endregion

        if (hit.collider != null)
        {
            _isGrounded = true;
            _coyoteTimeCounter = _coyoteTime;
            _isJumping = false; // 바닥에 닿으면 점프 상태 초기화

            // #region agent log
            DebugLogger.Log(
                "PlayerPhysics.cs:150",
                "Hit detected - setting grounded",
                new
                {
                    isGrounded = _isGrounded,
                    coyoteTimeCounter = _coyoteTimeCounter,
                    coyoteTime = _coyoteTime
                },
                "C"
            );
            // #endregion
        }
        else
        {
            _coyoteTimeCounter -= Time.fixedDeltaTime;
            _isGrounded = _coyoteTimeCounter > 0f;

            // #region agent log
            DebugLogger.Log(
                "PlayerPhysics.cs:163",
                "No hit - checking coyote time",
                new
                {
                    coyoteTimeCounter = _coyoteTimeCounter,
                    isGrounded = _isGrounded,
                    fixedDeltaTime = Time.fixedDeltaTime
                },
                "D"
            );
            // #endregion
        }

        // #region agent log
        DebugLogger.Log(
            "PlayerPhysics.cs:175",
            "CheckGrounded exit",
            new
            {
                finalIsGrounded = _isGrounded,
                finalCoyoteTimeCounter = _coyoteTimeCounter
            },
            "E"
        );
        // #endregion
    }

    /// <summary>
    /// 점프 요청을 받습니다. Jump Buffer에 저장되고, 다음 FixedUpdate에서 조건을 체크하여 실행됩니다.
    /// </summary>
    public void RequestJump()
    {
        _jumpBufferCounter = _jumpBufferTime;
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
    /// 점프 실행 (내부 메서드)
    /// </summary>
    private void ExecuteJump()
    {
        if (_rb == null) return;

        // Y축 속도를 점프 힘으로 설정 (즉각적인 반응)
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);

        // 점프 후 Coyote Time과 Jump Buffer 초기화
        _coyoteTimeCounter = 0f;
        _jumpBufferCounter = 0f;
        _isJumping = true;
    }

    private void FixedUpdate()
    {
        // 매 프레임 바닥 감지
        CheckGrounded();

        // Jump Buffer 감소
        if (_jumpBufferCounter > 0f)
        {
            _jumpBufferCounter -= Time.fixedDeltaTime;
        }

        // 점프 실행 조건 체크 (Jump Buffer + Coyote Time)
        if (_jumpBufferCounter > 0f && _isGrounded)
        {
            ExecuteJump();
        }
    }
}

