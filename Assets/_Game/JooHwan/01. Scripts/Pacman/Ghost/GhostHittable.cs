using UnityEngine;

/// <summary>
/// 투척된 파워펠릿에 맞을 수 있는 대상이 구현한다 (실제 고스트 스크립트가 상속/구현 예정).
/// 타이밍(발광 상태) 유효 여부 판정은 이 메서드를 구현하는 쪽의 책임이다.
/// </summary>
public abstract class GhostHittable : MonoBehaviour
{
    public abstract void HandlePelletHit();
}
