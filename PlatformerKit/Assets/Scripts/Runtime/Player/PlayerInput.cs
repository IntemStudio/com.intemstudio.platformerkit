using UnityEngine;

namespace IntemStudio
{
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

        public void LogicUpdate()
        {
            // 입력 읽기만 수행
            HorizontalInput = Input.GetAxis("Horizontal");
            VerticalInput = Input.GetAxis("Vertical");
            IsDownInputPressed = Input.GetKey(KeyCode.DownArrow);
            IsJumpPressed = Input.GetKeyDown(KeyCode.Z);
            IsJumpReleased = Input.GetKeyUp(KeyCode.Z);
            IsDashPressed = Input.GetKeyDown(KeyCode.C);
        }
    }
}