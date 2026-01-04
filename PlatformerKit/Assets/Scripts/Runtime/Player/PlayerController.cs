using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;

    private PlayerPhysics _physics;
    private float _horizontalInput;
    private Transform _modelTransform;

    private void Awake()
    {
        _physics = GetComponent<PlayerPhysics>();

        // PlayerPhysics가 없으면 추가
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
        else
        {
            Debug.LogWarning("PlayerController: 'Model' 자식 오브젝트를 찾을 수 없습니다.");
        }
    }


    private void Update()
    {
        // 입력은 Update에서 읽어서 저장
        _horizontalInput = Input.GetAxis("Horizontal");

        // 점프 입력 감지 - Physics에 요청만 전달
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _physics.RequestJump();
        }

        // 점프 키를 떼면 가변 점프 처리
        if (Input.GetKeyUp(KeyCode.Space))
        {
            _physics.ReleaseJump();
        }

        // 아래 점프 입력 감지 (S키 또는 아래 화살표)
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            _physics.RequestDownJump();
        }

        // 대시 입력 감지
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Vector2 dashDirection = Vector2.zero;

            // 대시 방향 계산
            if (Mathf.Abs(_horizontalInput) > 0.01f)
            {
                // 입력이 있는 경우: 좌/우 키 입력 기준
                dashDirection = _horizontalInput > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                // 입력이 없는 경우: _modelTransform의 스케일 x 값 기준
                if (_modelTransform != null)
                {
                    dashDirection = _modelTransform.localScale.x > 0 ? Vector2.right : Vector2.left;
                }
                else
                {
                    // Model이 없으면 기본값: 오른쪽
                    dashDirection = Vector2.right;
                }
            }

            _physics.RequestDash(dashDirection);
        }

        // Model 스프라이트 방향 반전
        UpdateModelDirection();
    }

    private void UpdateModelDirection()
    {
        if (_modelTransform == null) return;

        // 입력값이 0이 아니면 방향 변경, 0이면 이전 방향 유지
        if (Mathf.Abs(_horizontalInput) > 0.01f)
        {
            Vector3 scale = _modelTransform.localScale;
            scale.x = _horizontalInput > 0 ? 1f : -1f;
            _modelTransform.localScale = scale;
        }
    }

    private void FixedUpdate()
    {
        // 대시 중일 때는 일반 이동 입력 무시
        if (!_physics.IsDashing)
        {
            // 즉각적인 반응: 입력에 바로 속도 적용 (가속/감속 없음)
            float targetVelocityX = _horizontalInput * _moveSpeed;

            // PlayerPhysics를 통해 수평 속도 적용 (Y축 속도는 자동으로 유지됨)
            _physics.ApplyHorizontalVelocity(targetVelocityX);
        }
    }
}

