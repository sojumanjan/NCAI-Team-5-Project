using UnityEngine;

public class GameSelectUI : MonoBehaviour
{
    [Header("Description Popup")]
    [SerializeField] private GameObject descriptionPopupRoot;
    [SerializeField] private UnityEngine.UI.Text descriptionText;

    [TextArea]
    [SerializeField] private string tetrisDescription = "[테트리스]\n위에서 떨어지는 블록을 피하거나 밟고 올라가 EXIT에 도달하세요.\n블록에 깔리면 안 됩니다.";

    [TextArea]
    [SerializeField] private string pacmanDescription = "[팩맨]\n파워펠릿을 던져 고스트를 처치하세요.\n고스트가 강하게 발광할 때만 처치할 수 있습니다.";

    private void Awake()
    {
        descriptionPopupRoot.SetActive(false);
    }

    public void OnClickStart()
    {
        MiniGameFlowManager.Instance.StartTetris();
    }

    public void OnClickDescription()
    {
        descriptionText.text = tetrisDescription + "\n\n" + pacmanDescription;
        descriptionPopupRoot.SetActive(true);
    }

    public void OnClickCloseDescription()
    {
        descriptionPopupRoot.SetActive(false);
    }
}
