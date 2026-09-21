using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임의 모든 레시피를 담은 에셋 하나. 스테이션들은 같은 책을 참조하고 자기
/// <see cref="StationKind"/>로 걸러 쓴다. 손님 주문도 여기서 뽑으므로, 레시피를 만드는
/// 순간 그 메뉴는 주문 가능해진다 — 따로 관리할 메뉴 에셋이 없다.
///
/// 대안이었던 "스테이션마다 레시피 배열"은 커피머신 두 대에 같은 레시피를 넣다가 한쪽을
/// 빠뜨리는 길이다. 공용 책이면 메뉴 추가는 RecipeData 만들어 여기 끌어다 놓으면 끝이다.
///
/// 생성: Assets > Create > Cooking > Recipe Book
/// </summary>
[CreateAssetMenu(fileName = "RecipeBook", menuName = "Cooking/Recipe Book")]
public class RecipeBook : ScriptableObject
{
    [Tooltip("게임의 모든 레시피. 스테이션은 자기 종류에 맞는 것만 골라 씁니다.")]
    [SerializeField] private RecipeData[] recipes;

    public IReadOnlyList<RecipeData> Recipes => recipes;

    // 손님 한 명당 한 번만 쓰는 임시 버퍼. 매번 새로 할당하지 않으려고 들고 있는다.
    private readonly List<ItemData> _poolItems = new();
    private readonly List<float> _poolWeights = new();
    private readonly List<ItemData> _single = new();

    /// <summary>담긴 재료와 정확히 맞아떨어지는 레시피. 없으면 null.</summary>
    public RecipeData FindMatch(StationKind kind, IReadOnlyList<ItemData> loaded)
    {
        if (recipes == null || loaded == null || loaded.Count == 0)
        {
            return null;
        }

        foreach (RecipeData recipe in recipes)
        {
            if (recipe != null && recipe.Station == kind && recipe.Matches(loaded))
            {
                return recipe;
            }
        }

        return null;
    }

    /// <summary>
    /// 이 스테이션의 레시피 중 하나라도 지금 담긴 것 위에 후보 재료를 더 쓸 수 있는지.
    /// 어디에도 쓰이지 않을 재료를 거절하는 데 쓴다.
    /// </summary>
    public bool AnyAccepts(StationKind kind, ItemData candidate, IReadOnlyList<ItemData> loaded)
    {
        if (recipes == null || candidate == null)
        {
            return false;
        }

        foreach (RecipeData recipe in recipes)
        {
            if (recipe != null && recipe.Station == kind && recipe.CouldAccept(candidate, loaded))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 이 음식을 만드는 레시피. 없으면 null. 메뉴판이 손님 주문의 재료 구성을 보여줄 때
    /// 쓴다 — 덕분에 그 목록을 어디에도 두 번 적지 않아도 된다.
    /// </summary>
    public RecipeData FindByOutput(ItemData dish)
    {
        if (recipes == null || dish == null)
        {
            return null;
        }

        foreach (RecipeData recipe in recipes)
        {
            if (recipe != null && recipe.Output == dish)
            {
                return recipe;
            }
        }

        return null;
    }

    /// <summary>
    /// 손님이 주문할 메뉴 하나. 가중치를 따르며, 없으면 null.
    /// 메뉴판을 따로 적지 않고 레시피에서 끌어내므로 둘이 어긋날 수가 없다.
    /// </summary>
    public ItemData GetRandomOutput()
    {
        GetRandomOutputs(1, _single);
        return _single.Count > 0 ? _single[0] : null;
    }

    /// <summary>
    /// 서로 다른 메뉴 <paramref name="count"/>개. 같은 메뉴를 두 번 시키지 않는다.
    /// 만들 수 있는 메뉴가 모자라면 있는 만큼만 담는다 — 메뉴가 두 종류뿐인데 세 개를
    /// 시키면 주문이 영영 완성되지 않기 때문이다.
    ///
    /// 뽑기는 <see cref="RecipeData.OrderWeight"/>를 따른다. 손이 많이 가는 메뉴는 가중치를
    /// 낮춰 덜 나오게 할 수 있고, 0으로 두면 레시피는 남겨둔 채 주문에서만 뺄 수 있다.
    /// </summary>
    public void GetRandomOutputs(int count, List<ItemData> into)
    {
        if (into == null)
        {
            return;
        }

        into.Clear();

        if (recipes == null || count <= 0)
        {
            return;
        }

        BuildPool();

        if (_poolItems.Count == 0)
        {
            Debug.LogError($"{name}: 주문 가능한 메뉴가 없습니다. 완성 요리(Dish) 레시피가 있는지 " +
                           "확인하세요.", this);
            return;
        }

        int take = Mathf.Min(count, _poolItems.Count);
        for (int i = 0; i < take; i++)
        {
            int pick = PickWeighted();

            into.Add(_poolItems[pick]);

            // 뽑은 것은 후보에서 뺀다. 한 주문에 같은 메뉴를 두 번 넣지 않기 위해서.
            _poolItems.RemoveAt(pick);
            _poolWeights.RemoveAt(pick);
        }
    }

    /// <summary>
    /// 주문 가능한 메뉴와 가중치를 모은다. 같은 메뉴를 내는 레시피가 둘이면 큰 쪽을 쓴다.
    /// </summary>
    private void BuildPool()
    {
        _poolItems.Clear();
        _poolWeights.Clear();

        foreach (RecipeData recipe in recipes)
        {
            if (!IsOrderable(recipe))
            {
                continue;
            }

            int existing = _poolItems.IndexOf(recipe.Output);
            if (existing >= 0)
            {
                _poolWeights[existing] = Mathf.Max(_poolWeights[existing], recipe.OrderWeight);
                continue;
            }

            _poolItems.Add(recipe.Output);
            _poolWeights.Add(recipe.OrderWeight);
        }
    }

    /// <summary>가중치 룰렛. 전부 0이면 균등하게 뽑는다 — 아무도 안 나오는 것보다는 낫다.</summary>
    private int PickWeighted()
    {
        float total = 0f;
        foreach (float weight in _poolWeights)
        {
            total += weight;
        }

        if (total <= 0f)
        {
            return Random.Range(0, _poolItems.Count);
        }

        float roll = Random.value * total;
        for (int i = 0; i < _poolWeights.Count; i++)
        {
            roll -= _poolWeights[i];
            if (roll <= 0f)
            {
                return i;
            }
        }

        return _poolItems.Count - 1;
    }


    /// <summary>손님이 시킬 수 있는 레시피인지. 중간 산출물은 메뉴가 아니다.</summary>
    private static bool IsOrderable(RecipeData recipe)
    {
        return recipe != null
               && recipe.Output != null
               && recipe.Output.Category == ItemCategory.Dish;
    }
}
