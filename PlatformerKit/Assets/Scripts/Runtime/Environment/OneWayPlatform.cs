using UnityEngine;
using System.Collections;

namespace IntemStudio
{
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
        private Coroutine _disableCollisionCoroutine; // 충돌 무시 코루틴 참조
        private float _remainingDisableTime; // 남은 충돌 무시 시간
        private bool _isCollisionDisabled; // 충돌 무시 상태 플래그
        private Collider2D _ignoredCollider; // 현재 충돌 무시 중인 콜라이더 (플레이어)

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
        /// 특정 플레이어와 플랫폼 간의 충돌을 일시적으로 무시합니다.
        /// 플랫폼 콜라이더를 비활성화하지 않고 Physics2D.IgnoreCollision을 사용하여
        /// 해당 플레이어만 통과할 수 있도록 합니다. 다른 오브젝트(적 등)는 영향받지 않습니다.
        /// 중복 호출 방지: 이미 같은 플레이어에 대해 충돌 무시 중이고 새로운 duration이 기존보다 짧거나 같으면 무시합니다.
        /// 더 긴 duration이 요청되면 기존 코루틴을 취소하고 새로운 것으로 교체합니다.
        /// </summary>
        /// <param name="playerCollider">충돌을 무시할 플레이어의 콜라이더</param>
        /// <param name="duration">충돌 무시 지속 시간 (기본값: 0.5초)</param>
        public void DisableCollisionTemporarily(Collider2D playerCollider, float duration = 0.5f)
        {
            if (_boxCollider == null || playerCollider == null) return;

            // 중복 충돌 무시 방지: 같은 플레이어에 대해 이미 무시 중이고 새로운 duration이 기존보다 짧거나 같으면 무시
            if (_isCollisionDisabled && _ignoredCollider == playerCollider && duration <= _remainingDisableTime)
            {
                return; // 이미 충분히 긴 시간 동안 충돌 무시 예정이므로 무시
            }

            // 다른 플레이어에 대해 충돌 무시 중이면 기존 무시 해제
            if (_isCollisionDisabled && _ignoredCollider != null && _ignoredCollider != playerCollider)
            {
                // 기존 충돌 무시 해제
                Physics2D.IgnoreCollision(_boxCollider, _ignoredCollider, false);
            }

            // 기존 코루틴이 실행 중이면 취소
            if (_disableCollisionCoroutine != null)
            {
                StopCoroutine(_disableCollisionCoroutine);
                _disableCollisionCoroutine = null;
            }

            // 플레이어와 플랫폼 간의 충돌만 일시적으로 무시 (다른 오브젝트는 영향받지 않음)
            Physics2D.IgnoreCollision(_boxCollider, playerCollider, true);
            _isCollisionDisabled = true;
            _ignoredCollider = playerCollider;
            _remainingDisableTime = duration;

            // 지정된 시간 후 충돌 무시를 해제하는 코루틴 시작
            _disableCollisionCoroutine = StartCoroutine(EnableCollisionAfterDelay(playerCollider, duration));
        }

        /// <summary>
        /// 지정된 시간 후 플레이어와 플랫폼 간의 충돌 무시를 해제하는 코루틴
        /// </summary>
        private IEnumerator EnableCollisionAfterDelay(Collider2D playerCollider, float duration)
        {
            float elapsedTime = 0f;

            // 매 프레임 남은 시간 업데이트
            while (elapsedTime < duration)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
                _remainingDisableTime = duration - elapsedTime;
            }

            EnableCollision(playerCollider);
            _disableCollisionCoroutine = null;
        }

        /// <summary>
        /// 플레이어와 플랫폼 간의 충돌 무시를 해제합니다.
        /// </summary>
        private void EnableCollision(Collider2D playerCollider)
        {
            if (_boxCollider != null && playerCollider != null)
            {
                // 플레이어와 플랫폼 간의 충돌 무시 해제
                Physics2D.IgnoreCollision(_boxCollider, playerCollider, false);
            }

            // 상태 초기화
            _isCollisionDisabled = false;
            _remainingDisableTime = 0f;
            _ignoredCollider = null;
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
}