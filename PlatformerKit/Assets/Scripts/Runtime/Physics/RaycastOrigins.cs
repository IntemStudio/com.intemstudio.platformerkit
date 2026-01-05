using UnityEngine;

namespace IntemStudio
{
    /// <summary>
    /// Raycast 원점 정보를 저장하는 구조체
    /// 콜라이더의 경계에서 skinWidth만큼 안쪽으로 이동한 위치에 Raycast 원점을 설정
    /// </summary>
    [System.Serializable]
    public struct RaycastOrigins
    {
        public Vector2 topLeft;
        public Vector2 topRight;
        public Vector2 bottomLeft;
        public Vector2 bottomRight;
        public Vector2 top;
        public Vector2 bottom;
        public Vector2 left;
        public Vector2 right;
    }
}