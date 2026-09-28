using UnityEngine;

/// <summary>
/// 게임 전체에서 쓸 마우스 커서 그림. <see cref="CursorKeeper"/>가 읽어 필요할 때마다 다시 입힌다.
///
/// Player Settings의 Default Cursor와 같은 그림을 여기에도 넣어둔다. 그 설정은 실행 중에 코드로
/// 읽을 수 없어서, 커서가 기본 화살표로 돌아갔을 때 되돌릴 그림을 따로 들고 있어야 한다.
///
/// 에셋은 반드시 Resources 폴더 안에 CursorSettings.asset 이름으로 두어야 자동으로 찾는다.
/// </summary>
[CreateAssetMenu(fileName = "CursorSettings", menuName = "Hub/Cursor Settings")]
public class CursorSettings : ScriptableObject
{
    private const string RESOURCE_NAME = "CursorSettings";

    [Tooltip("커서 그림. 텍스처 임포트 설정의 Texture Type이 Cursor여야 합니다.")]
    [SerializeField] private Texture2D cursorTexture;

    [Tooltip("그림에서 실제로 클릭되는 지점 (왼쪽 위 기준 픽셀).")]
    [SerializeField] private Vector2 hotspot = Vector2.zero;

    public Texture2D Texture => cursorTexture;

    public Vector2 Hotspot => hotspot;

    /// <summary>Resources에 둔 설정을 불러온다. 없으면 null.</summary>
    public static CursorSettings Load() => Resources.Load<CursorSettings>(RESOURCE_NAME);
}
