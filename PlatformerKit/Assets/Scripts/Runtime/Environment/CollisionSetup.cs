using UnityEngine;

public class CollisionSetup : MonoBehaviour
{
    [SerializeField] private Vector2 _colliderSize = new Vector2(10f, 0.5f);
    [SerializeField] private bool _autoSetupCollider = true;

    private Rigidbody2D _rb;
    private BoxCollider2D _boxCollider;

    private void Awake()
    {
        SetupLayer();
        SetupSpriteRenderer();
        SetupRigidbody2D();

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
            // 레이어가 존재하지 않으면 경고
            Debug.LogWarning($"GroundSetup: 'Collision' 레이어가 존재하지 않습니다. 게임 오브젝트 '{gameObject.name}'의 레이어를 설정하지 않았습니다.");
            return;
        }

        gameObject.layer = collisionLayer;
    }

    private void SetupSpriteRenderer()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            // SpriteRenderer가 없으면 경고만 출력 (필수 컴포넌트가 아님)
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

        // Static Body Type 설정 (땅은 움직이지 않음)
        _rb.bodyType = RigidbodyType2D.Static;

        // Simulated 활성화 (충돌 감지)
        _rb.simulated = true;
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

    // 에디터에서 수동으로 호출 가능한 메서드
    [ContextMenu("Setup Ground")]
    private void SetupGround()
    {
        SetupLayer();
        SetupSpriteRenderer();
        SetupRigidbody2D();
        SetupCollider();
    }
}

