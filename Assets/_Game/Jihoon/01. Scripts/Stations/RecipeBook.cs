using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every recipe in the game, in one asset. Stations reference the same book and filter by
/// their own <see cref="StationKind"/>. Customers order from it too, so a dish becomes
/// orderable the moment its recipe exists — no separate menu asset to keep in sync.
///
/// The alternative — a recipe list per station — means maintaining the espresso recipe in
/// both coffee machines and forgetting one. With a shared book, adding a menu item is:
/// create the RecipeData, drag it in here, done.
///
/// Create via: Assets > Create > Cooking > Recipe Book
/// </summary>
[CreateAssetMenu(fileName = "RecipeBook", menuName = "Cooking/Recipe Book")]
public class RecipeBook : ScriptableObject
{
    [Tooltip("게임의 모든 레시피. 스테이션은 자기 종류에 맞는 것만 골라 씁니다.")]
    [SerializeField] private RecipeData[] recipes;

    public IReadOnlyList<RecipeData> Recipes => recipes;

    /// <summary>The recipe whose ingredients exactly match what is loaded, or null.</summary>
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
    /// True when at least one recipe for this station could still use the candidate on top
    /// of what is loaded. Used to refuse ingredients that lead nowhere.
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
    /// The recipe that produces this dish, or null. Lets the order board show what a
    /// customer's drink is made of without anyone authoring that list twice.
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
    /// One dish at random from everything that can be cooked. This is the menu customers
    /// order from, derived rather than authored so the two can never drift apart.
    /// </summary>
    public ItemData GetRandomOutput()
    {
        if (recipes == null || recipes.Length == 0)
        {
            return null;
        }

        // Count first so the pick is uniform without allocating a list every order.
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
            Debug.LogError($"{name}: no recipe has an Output assigned, so customers cannot order.", this);
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
