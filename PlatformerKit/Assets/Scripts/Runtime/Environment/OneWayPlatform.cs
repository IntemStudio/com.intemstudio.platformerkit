using UnityEngine;

/// <summary>
/// 위로는 통과하고 아래로는 막히는 One-way Platform 컴포넌트
/// 플레이어가 위에서 아래로 내려올 때만 통과할 수 있습니다.
/// </summary>
public class OneWayPlatform : MonoBehaviour
{
    [SerializeField] private Vector2 _colliderSize = new Vector2(10f, 0.5f);
    [SerializeField] private bool _autoSetupCollider = true;
    [SerializeField] private LayerMask _playerLayerMask = 1; // 플레이어 레이어 마스크

    private Rigidbody2D _rb;
    private BoxCollider2D _boxCollider;
    private PlatformEffector2D _platformEffector;

    private void Awake()
    {
        SetupLayer();
        SetupSpriteRenderer();
        SetupRigidbody2D();
        SetupPlatformEffector();

        if (_autoSetupCollider)
        {
            SetupCollider();
        }
    }

    private void SetupLayer()
    {
        // "Collision" 레이어로 설정
        int collisionLayer = LayerMask.NameToLayer("Collision");

        if (collisionLayer == -1)
        {
            Debug.LogWarning($"OneWayPlatform: 'Collision' 레이어가 존재하지 않습니다. 게임 오브젝트 '{gameObject.name}'의 레이어를 설정하지 않았습니다.");
            return;
        }

        gameObject.layer = collisionLayer;
    }

    private void SetupSpriteRenderer()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }

        // Sorting Layer를 "Collision"으로 설정
        spriteRenderer.sortingLayerName = "Collision";
    }

    private void SetupRigidbody2D()
    {
        _rb = GetComponent<Rigidbody2D>();

        // Rigidbody2D가 없으면 추가
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Static Body Type 설정 (플랫폼은 움직이지 않음)
        _rb.bodyType = RigidbodyType2D.Static;

        // Simulated 활성화 (충돌 감지)
        _rb.simulated = true;
    }

    private void SetupPlatformEffector()
    {
        _platformEffector = GetComponent<PlatformEffector2D>();

        // PlatformEffector2D가 없으면 추가
        if (_platformEffector == null)
        {
            _platformEffector = gameObject.AddComponent<PlatformEffector2D>();
        }

        // One-way platform 설정: 위에서 아래로만 통과 가능
        _platformEffector.useOneWay = true;
        _platformEffector.useOneWayGrouping = true;
        _platformEffector.surfaceArc = 180f; // 위쪽 180도만 통과 가능
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

        // PlatformEffector와 함께 사용하기 위해 usedByEffector 활성화
        _boxCollider.usedByEffector = true;
    }

    /// <summary>
    /// 플레이어가 아래 점프로 플랫폼을 통과할 수 있도록 일시적으로 충돌을 비활성화합니다.
    /// </summary>
    public void DisableCollisionTemporarily(float duration = 0.5f)
    {
        if (_boxCollider == null) return;

        // 콜라이더를 일시적으로 비활성화
        _boxCollider.enabled = false;

        // 지정된 시간 후 다시 활성화
        Invoke(nameof(EnableCollision), duration);
    }

    private void EnableCollision()
    {
        if (_boxCollider != null)
        {
            _boxCollider.enabled = true;
        }
    }

    // 에디터에서 수동으로 호출 가능한 메서드
    [ContextMenu("Setup One-Way Platform")]
    private void SetupOneWayPlatform()
    {
        SetupLayer();
        SetupSpriteRenderer();
        SetupRigidbody2D();
        SetupPlatformEffector();
        SetupCollider();
    }
}

