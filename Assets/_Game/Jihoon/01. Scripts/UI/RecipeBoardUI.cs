using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 벽에 붙은 레시피판. 칸마다 완성품 하나와 그 재료를 채운다.
///
/// 칸은 씬에 미리 만들어 둔 것을 쓰고 코드는 내용만 채운다. 배치·크기·폰트를 전부 씬에서
/// 잡아두었기 때문에, 여기서 오브젝트를 만들면 그 손질이 전부 무너진다.
///
/// 어느 슬롯이 무엇인지는 <b>자식 순서</b>로 정한다. 칸마다 이름이 제각각이라
/// ("재료 이미지2", "재료 이미지1 (1)") 이름으로 찾으면 칸 하나만 어긋나도 조용히 빈다.
/// 순서는 여덟 칸이 전부 같다 — 이미지는 완성품 다음 재료, 텍스트도 마찬가지.
/// </summary>
public class RecipeBoardUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("레시피를 읽어올 곳. 씬의 기구들과 같은 것을 쓰세요.")]
    [SerializeField] private RecipeBook recipeBook;

    [Tooltip("칸들이 들어 있는 부모. 비워두면 이 오브젝트의 자식에서 찾습니다.")]
    [SerializeField] private Transform columnsRoot;

    [Header("차례")]
    [Tooltip("왼쪽부터 어떤 메뉴를 보여줄지. 칸 수와 같아야 합니다.")]
    [SerializeField] private ItemData[] menuOrder;

    [Header("빈 칸")]
    [Tooltip("재료가 칸보다 적을 때 남는 자리를 끕니다. 끄지 않으면 앞 재료가 그대로 남습니다.")]
    [SerializeField] private bool hideUnusedSlots = true;

    private readonly List<Image> _images = new();
    private readonly List<TMP_Text> _texts = new();

    private void Awake()
    {
        if (recipeBook == null)
        {
            Debug.LogError($"{nameof(RecipeBoardUI)} on '{name}': 레시피북이 없습니다.", this);
            enabled = false;
            return;
        }

        Rebuild();
    }

    /// <summary>판 전체를 다시 채운다. 레시피를 고친 뒤 확인할 때도 쓴다.</summary>
    [ContextMenu("다시 채우기")]
    public void Rebuild()
    {
        Transform root = columnsRoot != null ? columnsRoot : transform;
        List<Transform> columns = FindColumns(root);

        if (menuOrder == null)
        {
            return;
        }

        for (int i = 0; i < columns.Count; i++)
        {
            // 차례에 적어두지 않은 칸은 통째로 끈다. 예시로 남겨둔 칸이 그대로 보이는 것보다 낫다.
            if (i >= menuOrder.Length || menuOrder[i] == null)
            {
                columns[i].gameObject.SetActive(false);
                continue;
            }

            columns[i].gameObject.SetActive(true);
            Fill(columns[i], menuOrder[i]);
        }

        for (int i = columns.Count; i < menuOrder.Length; i++)
        {
            if (menuOrder[i] != null)
            {
                Debug.LogWarning($"{name}: '{menuOrder[i].DisplayName}'을 넣을 칸이 없습니다. " +
                                 $"칸은 {columns.Count}개인데 차례는 {menuOrder.Length}개입니다.", this);
            }
        }
    }

    /// <summary>
    /// 칸으로 볼 자식들. 구분선 뭉치나 배경처럼 이미지 한 장뿐인 자식은 칸이 아니다.
    /// </summary>
    private static List<Transform> FindColumns(Transform root)
    {
        var columns = new List<Transform>();

        foreach (Transform child in root)
        {
            int images = 0;
            int texts = 0;

            foreach (Transform slot in child)
            {
                if (slot.GetComponent<Image>() != null)
                {
                    images++;
                }

                if (slot.GetComponent<TMP_Text>() != null)
                {
                    texts++;
                }
            }

            if (images >= 2 && texts >= 1)
            {
                columns.Add(child);
            }
        }

        return columns;
    }

    private void Fill(Transform column, ItemData menu)
    {
        Collect(column);

        RecipeData recipe = recipeBook.FindByOutput(menu);
        IReadOnlyList<ItemData> inputs = recipe != null ? recipe.Inputs : null;

        if (recipe == null)
        {
            Debug.LogWarning($"{name}: '{menu.DisplayName}'을 만드는 레시피가 없습니다.", this);
        }

        // 첫 이미지와 첫 글자가 완성품, 나머지가 재료. 여덟 칸이 전부 같은 차례로 놓여 있다.
        SetImage(0, menu);
        SetText(0, menu != null ? menu.DisplayName : string.Empty);

        int slots = Mathf.Min(_images.Count, _texts.Count) - 1;
        for (int i = 0; i < slots; i++)
        {
            bool used = inputs != null && i < inputs.Count;
            ItemData input = used ? inputs[i] : null;

            if (!used && hideUnusedSlots)
            {
                SetActive(i + 1, false);
                continue;
            }

            SetActive(i + 1, true);
            SetImage(i + 1, input);
            SetText(i + 1, input != null ? input.DisplayName : string.Empty);
        }
    }

    private void Collect(Transform column)
    {
        _images.Clear();
        _texts.Clear();

        foreach (Transform slot in column)
        {
            Image image = slot.GetComponent<Image>();
            if (image != null)
            {
                _images.Add(image);
            }

            TMP_Text text = slot.GetComponent<TMP_Text>();
            if (text != null)
            {
                _texts.Add(text);
            }
        }
    }

    private void SetImage(int index, ItemData item)
    {
        if (index >= _images.Count)
        {
            return;
        }

        Sprite icon = item != null ? item.Icon : null;
        _images[index].sprite = icon;

        // 아이콘이 아직 없는 재료는 칸을 비워 둔다. 앞 칸 그림이 남아 있으면 거짓말이 된다.
        _images[index].enabled = icon != null;

        if (icon == null && item != null)
        {
            Debug.LogWarning($"{name}: '{item.DisplayName}'에 아이콘이 없습니다.", this);
        }
    }

    private void SetText(int index, string value)
    {
        if (index < _texts.Count)
        {
            _texts[index].text = value;
        }
    }

    private void SetActive(int index, bool active)
    {
        if (index < _images.Count)
        {
            _images[index].gameObject.SetActive(active);
        }

        if (index < _texts.Count)
        {
            _texts[index].gameObject.SetActive(active);
        }
    }
}
