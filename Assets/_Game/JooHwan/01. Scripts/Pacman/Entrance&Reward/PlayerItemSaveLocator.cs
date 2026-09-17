using UnityEngine;

/// <summary>
/// 다른 팀원이 만들 예정인 PlayerData(IPlayerItemSave 구현체)를 찾아 연결하는 접근자.
/// 씬에 아직 구현체가 없어도(팀원 스크립트 작업 전) 에러 없이 동작하도록,
/// 못 찾으면 항상 미획득으로 취급하고 저장은 무시하는 더미로 대체한다.
/// </summary>
public static class PlayerItemSaveLocator
{
    private class NullItemSave : IPlayerItemSave
    {
        public bool HasItem(string itemId) => false;
        public void SetItemGain(string itemId) { }
    }

    private static IPlayerItemSave cached;

    public static IPlayerItemSave Get()
    {
        if (cached != null)
        {
            return cached;
        }

        foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (behaviour is IPlayerItemSave save)
            {
                cached = save;
                return cached;
            }
        }

        Debug.LogWarning("[PlayerItemSaveLocator] IPlayerItemSave 구현체를 찾지 못해 임시로 미저장 상태로 동작합니다.");
        cached = new NullItemSave();
        return cached;
    }
}
