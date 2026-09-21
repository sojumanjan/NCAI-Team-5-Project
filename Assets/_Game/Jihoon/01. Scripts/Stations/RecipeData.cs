using System.Collections.Generic;
using UnityEngine;

/// <summary>어느 기구의 레시피인지. 스테이션은 자기 종류만 골라 본다.</summary>
public enum StationKind
{
    CoffeeMachine,
    Oven,
    Blender,
    DoughTable,
    Mixer,
}

/// <summary>
/// 레시피 하나: 이 재료들을 저 스테이션에 넣고, 이만큼 기다리면, 저 음식이 나온다.
///
/// 재료 순서는 일부러 무시한다. 게임 규칙이 "하나라도 재료가 다르면"이라 *무엇을* 넣었는지의
/// 문제이지 순서의 문제가 아니기 때문에, 판정은 다중집합 비교다. 순서를 나중에 보게
/// 만드는 건 쉽지만, 이미 본 걸 걷어내는 건 어렵다.
///
/// 생성: Assets > Create > Cooking > Recipe Data
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

    [Header("주문 빈도")]
    [Tooltip("손님이 이 메뉴를 시킬 상대 확률. 1이 기본이고 0.3이면 대략 3분의 1 빈도로 " +
             "나옵니다. 0이면 주문에 아예 나오지 않습니다 (레시피는 그대로 만들 수 있습니다). " +
             "반죽처럼 손님이 시키지 않는 중간 산출물에는 영향이 없습니다.")]
    [SerializeField] private float orderWeight = 1f;

    public StationKind Station => station;

    public IReadOnlyList<ItemData> Inputs => inputs;

    public float Duration => Mathf.Max(0.1f, duration);

    public ItemData Output => output;

    /// <summary>손님 주문에서 뽑힐 상대 가중치. 0이면 메뉴판에서 빠진다.</summary>
    public float OrderWeight => Mathf.Max(0f, orderWeight);

    public int InputCount => inputs != null ? inputs.Length : 0;

    /// <summary>
    /// 담긴 재료가 이 레시피의 재료와 정확히 같은지. 순서는 무관하고, 개수와 종류가 같아야
    /// 하며 중복도 개수까지 맞아야 한다.
    /// </summary>
    public bool Matches(IReadOnlyList<ItemData> loaded)
    {
        if (inputs == null || loaded == null || loaded.Count != inputs.Length)
        {
            return false;
        }

        // 레시피 재료는 많아야 서너 개라 단순 짝짓기로 충분하다.
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
    /// 이미 담긴 것 위에 <paramref name="candidate"/>를 더해도 이 레시피에 아직 도달할 수
    /// 있는지. 스테이션이 영영 쓸 수 없는 재료를 삼키지 않고 거절할 수 있게 해준다.
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
        orderWeight = Mathf.Max(0f, orderWeight);
    }
}
