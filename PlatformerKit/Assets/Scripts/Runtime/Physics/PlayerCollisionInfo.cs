using UnityEngine;

/// <summary>
/// 플레이어의 충돌 정보를 저장하는 구조체
/// 4방향(위, 아래, 왼쪽, 오른쪽) 충돌 상태를 추적
/// </summary>
[System.Serializable]
public struct PlayerCollisionInfo
{
    public bool above;
    public bool below;
    public bool left;
    public bool right;
    
    public int faceDir; // 이동 방향 (-1: 왼쪽, 1: 오른쪽)
    
    public void Reset()
    {
        above = below = false;
        left = right = false;
        faceDir = 1;
    }
}

