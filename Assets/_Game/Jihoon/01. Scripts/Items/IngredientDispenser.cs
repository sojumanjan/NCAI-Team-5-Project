using UnityEngine;

/// <summary>
/// An endless source of one ingredient — the bean hopper by the coffee machine, the flour
/// on the dough table, each shelf slot in the fridge. Aim at it, left click, and a fresh
/// copy appears in your hands. The dispenser itself is never carried.
///
/// One dispenser holds exactly one ingredient, so the fridge is several of these rather
/// than one object with a menu. That keeps "재료 보관 위치" a matter of placing objects.
/// </summary>
[RequireComponent(typeof(Collider))]
public class IngredientDispenser : MonoBehaviour, IItemSource
{
    [Header("보관 재료")]
    [Tooltip("여기서 꺼낼 재료.")]
    [SerializeField] private ItemData item;

    public ItemData ProvidedItem => item;

    private void Awake()
    {
        if (item == null)
        {
            Debug.LogError($"{nameof(IngredientDispenser)} on '{name}' has no ItemData assigned.", this);
        }
        else if (item.WorldPrefab == null)
        {
            Debug.LogError($"ItemData '{item.name}' has no World Prefab, so '{name}' cannot dispense it.", this);
        }
    }

    public bool CanProvide(PlayerHands hands) => item != null && item.WorldPrefab != null;

    public GameObject Provide(PlayerHands hands)
    {
        if (!CanProvide(hands))
        {
            return null;
        }

        // Spawn straight at the hand. Anywhere else is pointless — the hands reparent and
        // reposition the item in the same frame — and spawning it inside the fridge
        // geometry gives its collider one live frame to register a bogus overlap.
        Transform at = hands != null && hands.HoldAnchor != null ? hands.HoldAnchor : transform;

        GameObject spawned = Instantiate(item.WorldPrefab, at.position, at.rotation);
        spawned.name = item.DisplayName;

        return spawned;
    }
}
