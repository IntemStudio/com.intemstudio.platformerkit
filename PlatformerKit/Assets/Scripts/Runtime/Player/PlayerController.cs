using UnityEngine;

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

        // PlayerInput이 없으면 추가
        if (_input == null)
        {
            _input = gameObject.AddComponent<PlayerInput>();
        }

        _stateMachine = GetComponent<PlayerStateMachine>();

        // PlayerStateMachine이 없으면 추가
        if (_stateMachine == null)
        {
            _stateMachine = gameObject.AddComponent<PlayerStateMachine>();
        }

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
        // 1. 입력 업데이트 (가장 먼저)
        if (_input != null)
        {
            _input.LogicUpdate();
        }

        // 2. 상태 머신 업데이트 (입력 처리 및 상태 전환)
        if (_stateMachine != null)
        {
            _stateMachine.LogicUpdate();
        }

        // 3. 점프 입력 감지 (아래 점프만 처리, 일반 점프는 상태 머신에서 처리)
        if (_input != null && _input.IsJumpPressed && _input.IsDownInputPressed)
        {
            // 아래 점프는 상태 머신을 거치지 않고 직접 처리
            if (_physics != null)
            {
                _physics.RequestDownJump();
            }
        }

        // 4. 점프 키를 떼면 가변 점프 처리 (아래 점프가 아닐 때만)
        if (_input != null && _input.IsJumpReleased && !_input.IsDownInputPressed)
        {
            if (_physics != null)
            {
                _physics.ReleaseJump();
            }
        }

        // 5. Model 스프라이트 방향 반전
        UpdateModelDirection();
    }

    private void UpdateModelDirection()
    {
        if (_modelTransform == null) return;

        // 입력값이 0이 아니면 방향 변경, 0이면 이전 방향 유지
        if (Mathf.Abs(_input.HorizontalInput) > 0.01f)
        {
            Vector3 scale = _modelTransform.localScale;
            scale.x = _input.HorizontalInput > 0 ? 1f : -1f;
            _modelTransform.localScale = scale;
        }
    }

    private void FixedUpdate()
    {
        // 1. 물리 시스템 업데이트 (바닥 감지, 대시 처리 등)
        if (_physics != null)
        {
            _physics.PhysisUpdate();
        }

        // 2. 상태 머신 FixedUpdate
        if (_stateMachine != null)
        {
            _stateMachine.PhysisUpdate();
        }

        // 3. 이동 속도 적용 (대시 중일 때는 일반 이동 입력 무시)
        if (_physics != null && _input != null)
        {
            if (!_physics.IsDashing)
            {
                // 즉각적인 반응: 입력에 바로 속도 적용 (가속/감속 없음)
                float targetVelocityX = _input.HorizontalInput * _moveSpeed;

                // PlayerPhysics를 통해 수평 속도 적용 (Y축 속도는 자동으로 유지됨)
                _physics.ApplyHorizontalVelocity(targetVelocityX);
            }
        }
    }

}

