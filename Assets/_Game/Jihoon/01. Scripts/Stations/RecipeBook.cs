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
    /// 손님이 주문할 메뉴 하나를 무작위로. 따로 적지 않고 레시피에서 끌어내기 때문에
    /// 메뉴판과 실제로 만들 수 있는 것이 어긋날 수가 없다.
    ///
    /// Dish만 뽑는다. 반죽처럼 중간 산출물인 레시피가 생기면 그것도 '만들 수 있는 것'이라,
    /// 거르지 않으면 손님이 반죽을 주문한다.
    /// </summary>
    public ItemData GetRandomOutput()
    {
        if (recipes == null || recipes.Length == 0)
        {
            return null;
        }

        // 주문마다 리스트를 만들지 않으면서도 균등하게 뽑으려고 개수를 먼저 센다.
        int usable = 0;
        foreach (RecipeData recipe in recipes)
        {
            if (IsOrderable(recipe))
            {
                usable++;
            }
        }

        if (usable == 0)
        {
            Debug.LogError($"{name}: 완성 요리(Dish)를 만드는 레시피가 하나도 없어 손님이 주문할 수 없습니다.", this);
            return null;
        }

        int chosen = Random.Range(0, usable);
        foreach (RecipeData recipe in recipes)
        {
            if (!IsOrderable(recipe))
            {
                continue;
            }

            if (chosen == 0)
            {
                return recipe.Output;
            }

            chosen--;
        }

        return null;
    }

    /// <summary>
    /// 서로 다른 메뉴 <paramref name="count"/>개. 같은 메뉴를 두 번 시키지 않는다.
    /// 만들 수 있는 메뉴가 모자라면 있는 만큼만 담는다 — 메뉴가 두 종류뿐인데 세 개를
    /// 시키면 주문이 영영 완성되지 않기 때문이다.
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

        // 중복 레시피가 같은 메뉴를 내놓을 수 있으므로 메뉴 기준으로 한 번 걸러낸다.
        List<ItemData> pool = new List<ItemData>();
        foreach (RecipeData recipe in recipes)
        {
            if (IsOrderable(recipe) && !pool.Contains(recipe.Output))
            {
                pool.Add(recipe.Output);
            }
        }

        if (pool.Count == 0)
        {
            Debug.LogError($"{name}: 완성 요리(Dish)를 만드는 레시피가 하나도 없어 손님이 주문할 수 없습니다.", this);
            return;
        }

        int take = Mathf.Min(count, pool.Count);
        for (int i = 0; i < take; i++)
        {
            int pick = Random.Range(i, pool.Count);
            (pool[i], pool[pick]) = (pool[pick], pool[i]);
            into.Add(pool[i]);
        }
    }

    /// <summary>손님이 시킬 수 있는 레시피인지. 중간 산출물은 메뉴가 아니다.</summary>
    private static bool IsOrderable(RecipeData recipe)
    {
        return recipe != null
               && recipe.Output != null
               && recipe.Output.Category == ItemCategory.Dish;
    }
}
