using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기구 위에 지금 무엇이 담겨 있는지 아이콘으로 보여준다.
///
/// 색 변화로는 "뭔가 들어있다"까지밖에 못 알린다. 재료 하나만 틀려도 주문 전체가 실패하는
/// 규칙이라, 플레이어에게 정말 필요한 정보는 "무엇이" 들어갔는지다. 게다가 실수는 기구를
/// 안 보고 있을 때 생기는데, E 프롬프트는 조준해야만 뜬다.
///
/// 손님 인내 게이지·벽 메뉴판과 같은 월드 스페이스 캔버스 방식이다. 기구 모양과 무관하게
/// 균일하게 읽히는 것이 장점 — 커피머신처럼 속이 안 보이는 기구에도 똑같이 붙는다.
/// </summary>
public class StationIngredientsUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("표시할 기구. 비워두면 부모에서 찾습니다.")]
    [SerializeField] private StationBase station;

    [Tooltip("끄고 켤 Canvas. 비워두면 이 오브젝트의 Canvas를 씁니다.")]
    [SerializeField] private Canvas canvas;

    [Header("아이콘")]
    [Tooltip("재료 아이콘 칸. 담긴 수만큼 왼쪽부터 켜고 나머지는 끕니다.")]
    [SerializeField] private Image[] iconSlots;

    [Tooltip("ItemData에 Icon이 아직 없을 때 칠할 색. 자리는 보이게 해둡니다.")]
    [SerializeField] private Color placeholderColor = new Color(1f, 1f, 1f, 0.35f);

    [Tooltip("담긴 게 없을 때 이름을 대신 보여줄 텍스트. (선택)")]
    [SerializeField] private TMP_Text overflowText;

    [Header("배치")]
    [Tooltip("기구 원점 기준 높이 (m).")]
    [SerializeField] private float heightOffset = 0.9f;

    [Tooltip("항상 카메라를 향하게 합니다.")]
    [SerializeField] private bool billboard = true;

    private Camera _camera;
    private Transform _anchor;

    private void Awake()
    {
        if (station == null)
        {
            station = GetComponentInParent<StationBase>();
        }

        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        if (station == null || canvas == null)
        {
            Debug.LogError($"{nameof(StationIngredientsUI)} on '{name}': 부모에 {nameof(StationBase)}가, " +
                           "이 오브젝트에 Canvas가 있어야 합니다.", this);
            enabled = false;
            return;
        }

        // 기구를 기준으로 위치를 잡는다. 자기 자신은 아래에서 위치를 덮어쓰므로 기준이 못 된다.
        _anchor = station.transform;

        // 구독을 Awake에 두는 이유: 담긴 게 없으면 캔버스를 끄는데, 그게 이 오브젝트를
        // 비활성화하는 설정이면 OnDisable에서 구독이 끊겨 영영 다시 켜지지 못한다.
        station.LoadedChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (station != null)
        {
            station.LoadedChanged -= Refresh;
        }
    }

    // 카메라가 그 프레임의 이동을 끝낸 뒤에 각을 맞춰야 한다. Update에서 하면 한 프레임
    // 늦어 패널이 눈에 띄게 흔들린다.
    private void LateUpdate()
    {
        if (!canvas.enabled)
        {
            return;
        }

        UpdatePose();
    }

    // ---------------------------------------------------------------- 그리기

    /// <summary>담긴 재료를 다시 그린다. 기구가 알려줄 때마다 불린다.</summary>
    public void Refresh()
    {
        var loaded = station.Loaded;
        int count = loaded != null ? loaded.Count : 0;

        SetVisible(count > 0);

        if (count == 0)
        {
            return;
        }

        int shown = 0;

        if (iconSlots != null)
        {
            for (int i = 0; i < iconSlots.Length; i++)
            {
                Image slot = iconSlots[i];
                if (slot == null)
                {
                    continue;
                }

                bool used = i < count;
                slot.gameObject.SetActive(used);

                if (used)
                {
                    ApplyIcon(slot, loaded[i]);
                    shown++;
                }
            }
        }

        // 칸보다 많이 들어갔을 때. 기구의 Max Ingredients가 칸 수보다 크면 생길 수 있다.
        if (overflowText != null)
        {
            int hidden = count - shown;
            overflowText.gameObject.SetActive(hidden > 0);
            overflowText.text = hidden > 0 ? $"+{hidden}" : string.Empty;
        }
    }

    /// <summary>아이콘이 없으면 단색 블록으로 대신한다. 그림이 없어도 몇 개 들어갔는지는 보인다.</summary>
    private void ApplyIcon(Image image, ItemData item)
    {
        Sprite icon = item != null ? item.Icon : null;
        image.sprite = icon;
        image.color = icon != null ? Color.white : placeholderColor;
    }

    private void UpdatePose()
    {
        if (_anchor == null)
        {
            return;
        }

        transform.position = _anchor.position + Vector3.up * heightOffset;

        if (!billboard)
        {
            return;
        }

        Camera cam = ResolveCamera();
        if (cam == null)
        {
            return;
        }

        // 카메라를 바라보게(LookAt) 하지 않고 카메라의 방향을 따라간다. 가까이서 올려다볼 때
        // LookAt은 패널을 기울여서 1인칭에서는 고장난 것처럼 보인다.
        transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
    }

    private Camera ResolveCamera()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        return _camera;
    }

    /// <summary>
    /// GameObject가 아니라 Canvas를 끈다. 이 오브젝트를 비활성화하면 LateUpdate가 멈춰
    /// 다시 켤 방법이 없어진다.
    /// </summary>
    private void SetVisible(bool visible)
    {
        if (canvas != null && canvas.enabled != visible)
        {
            canvas.enabled = visible;
        }
    }

    private void OnValidate()
    {
        heightOffset = Mathf.Max(0f, heightOffset);
    }
}
