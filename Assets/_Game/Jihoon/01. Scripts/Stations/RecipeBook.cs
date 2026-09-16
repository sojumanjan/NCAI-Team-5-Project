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
    /// 만들 수 있는 것 중 무작위로 하나. 손님이 주문하는 메뉴판이며, 따로 적지 않고
    /// 레시피에서 끌어내기 때문에 둘이 어긋날 수가 없다.
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
            if (recipe != null && recipe.Output != null)
            {
                usable++;
            }
        }

        if (usable == 0)
        {
            Debug.LogError($"{name}: Output이 지정된 레시피가 하나도 없어 손님이 주문할 수 없습니다.", this);
            return null;
        }

        int chosen = Random.Range(0, usable);
        foreach (RecipeData recipe in recipes)
        {
            if (recipe == null || recipe.Output == null)
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
}
