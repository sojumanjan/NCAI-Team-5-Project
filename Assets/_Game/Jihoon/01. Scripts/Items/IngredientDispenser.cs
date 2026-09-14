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

    [Tooltip("생성된 재료가 나타날 위치. 비워두면 이 오브젝트 위치를 씁니다.")]
    [SerializeField] private Transform spawnPoint;

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

        Transform origin = spawnPoint != null ? spawnPoint : transform;
        GameObject spawned = Instantiate(item.WorldPrefab, origin.position, origin.rotation);
        spawned.name = item.DisplayName;

        return spawned;
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = spawnPoint != null ? spawnPoint : transform;
        Gizmos.color = new Color(0.4f, 0.8f, 1f);
        Gizmos.DrawWireCube(origin.position, Vector3.one * 0.15f);
    }
}
