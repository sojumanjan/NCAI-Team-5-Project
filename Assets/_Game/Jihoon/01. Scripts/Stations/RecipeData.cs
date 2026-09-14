using System.Collections.Generic;
using UnityEngine;

/// <summary>Which appliance a recipe belongs to. Stations only match their own kind.</summary>
public enum StationKind
{
    CoffeeMachine,
    Oven,
    Blender,
    DoughTable,
}

/// <summary>
/// One recipe: put these ingredients into that station, wait this long, get that dish.
///
/// Ingredient order is deliberately ignored. The game rule is "하나라도 재료가 다르면"
/// — a rule about *which* ingredients, not the sequence — so matching is a multiset
/// comparison. Order sensitivity is easy to add later and hard to remove.
///
/// Create via: Assets > Create > Cooking > Recipe Data
/// </summary>
[CreateAssetMenu(fileName = "Recipe_", menuName = "Cooking/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Header("레시피")]
    [Tooltip("이 레시피를 처리할 스테이션 종류.")]
    [SerializeField] private StationKind station;

    [Tooltip("필요한 재료. 순서는 상관없고 개수만 맞으면 됩니다. 같은 재료 2개도 가능합니다.")]
    [SerializeField] private ItemData[] inputs;

    [Tooltip("조리에 걸리는 시간 (초).")]
    [SerializeField] private float duration = 3f;

    [Tooltip("완성품.")]
    [SerializeField] private ItemData output;

    public StationKind Station => station;

    public IReadOnlyList<ItemData> Inputs => inputs;

    public float Duration => Mathf.Max(0.1f, duration);

    public ItemData Output => output;

    public int InputCount => inputs != null ? inputs.Length : 0;

    /// <summary>
    /// True when the loaded ingredients are exactly this recipe's ingredients, in any
    /// order. Same count, same items, duplicates respected.
    /// </summary>
    public bool Matches(IReadOnlyList<ItemData> loaded)
    {
        if (inputs == null || loaded == null || loaded.Count != inputs.Length)
        {
            return false;
        }

        // Recipes have at most a handful of ingredients, so the naive pairing is fine.
        bool[] claimed = new bool[inputs.Length];

        foreach (ItemData candidate in loaded)
        {
            int found = -1;
            for (int i = 0; i < inputs.Length; i++)
            {
                if (!claimed[i] && inputs[i] == candidate)
                {
                    found = i;
                    break;
                }
            }

            if (found < 0)
            {
                return false;
            }

            claimed[found] = true;
        }

        return true;
    }

    /// <summary>
    /// True when adding <paramref name="candidate"/> to what is already loaded still
    /// leaves this recipe reachable. Lets a station refuse an ingredient it could never
    /// use instead of swallowing it.
    /// </summary>
    public bool CouldAccept(ItemData candidate, IReadOnlyList<ItemData> loaded)
    {
        if (inputs == null || candidate == null)
        {
            return false;
        }

        int alreadyLoaded = 0;
        if (loaded != null)
        {
            if (loaded.Count >= inputs.Length)
            {
                return false;
            }

            foreach (ItemData existing in loaded)
            {
                if (existing == candidate)
                {
                    alreadyLoaded++;
                }
            }
        }

        int required = 0;
        foreach (ItemData input in inputs)
        {
            if (input == candidate)
            {
                required++;
            }
        }

        return alreadyLoaded < required;
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.1f, duration);
    }
}
