using UnityEngine;

/// <summary>
/// 꺼낸 뒤로 시간이 지나면 못 쓰게 되는 음식. 완성품 프리팹에 붙인다.
///
/// 미리 잔뜩 만들어두고 손님이 올 때마다 하나씩 꺼내 주는 전략을 막으려는 것이다. 그걸
/// 허용하면 이 게임의 왕복이 전부 사라지고 준비 시간 한 번으로 하루가 끝난다.
///
/// 시간은 <b>기구에서 꺼낸 순간</b>부터 흐른다. 기구 안에 있는 동안은 세지 않는다 —
/// 그건 이미 그 기구를 묶어두고 있어서 그 자체로 값을 치르고 있다.
///
/// 상한 뒤에도 집고 놓을 수는 있다. 아무 기능이 없는 물건이 될 뿐이라 그대로 두든
/// 쓰레기통에 버리든 플레이어 마음이다.
/// </summary>
[RequireComponent(typeof(WorldItem))]
public class PerishableDish : MonoBehaviour
{
    [Header("시간")]
    [Tooltip("꺼낸 뒤 못 쓰게 되기까지 (초).")]
    [SerializeField] private float lifeSeconds = 60f;

    [Header("상했을 때")]
    [Tooltip("상하면 이 색으로 덮습니다.")]
    [SerializeField] private Color spoiledColor = new Color(0.05f, 0.05f, 0.05f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _block;
    private Renderer[] _renderers;
    private float _left;
    private bool _counting;

    /// <summary>이미 못 쓰게 됐는지.</summary>
    public bool IsSpoiled { get; private set; }

    /// <summary>시간이 흐르고 있는지. 기구 안에 있는 동안은 거짓이다.</summary>
    public bool IsCounting => _counting && !IsSpoiled;

    /// <summary>남은 시간 (초). 아직 시작 전이면 전체 시간.</summary>
    public float Remaining => IsSpoiled ? 0f : (_counting ? Mathf.Max(0f, _left) : lifeSeconds);

    /// <summary>기구에서 꺼내질 때 불린다. 두 번째부터는 아무 일도 하지 않는다.</summary>
    public void Begin()
    {
        if (_counting || IsSpoiled)
        {
            return;
        }

        _counting = true;
        _left = lifeSeconds;
    }

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _block = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (!_counting || IsSpoiled)
        {
            return;
        }

        _left -= Time.deltaTime;

        if (_left <= 0f)
        {
            Spoil();
        }
    }

    /// <summary>지금 상하게 만든다. 디버그와 테스트용으로도 열어둔다.</summary>
    [ContextMenu("지금 상하게")]
    public void Spoil()
    {
        if (IsSpoiled)
        {
            return;
        }

        IsSpoiled = true;
        _left = 0f;

        Paint();
    }

    /// <summary>
    /// 머티리얼을 새로 만들지 않고 프로퍼티 블록으로 색만 덮는다. 요리마다 머티리얼이
    /// 제각각이라 검은 머티리얼 하나로 바꿔치면 모양까지 뭉개진다.
    /// </summary>
    private void Paint()
    {
        if (_renderers == null)
        {
            return;
        }

        foreach (Renderer renderer in _renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, spoiledColor);
            _block.SetColor(ColorId, spoiledColor);
            renderer.SetPropertyBlock(_block);
        }
    }

    private void OnValidate()
    {
        lifeSeconds = Mathf.Max(1f, lifeSeconds);
    }
}
