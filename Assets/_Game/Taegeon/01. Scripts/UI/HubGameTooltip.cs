using UnityEngine;

[DisallowMultipleComponent]
public sealed class HubGameTooltip : MonoBehaviour
{
    [SerializeField] private GameObject tooltipCanvas;
    [SerializeField] private MiniGameEntry entry;

    private void Awake()
    {
        Close();
    }

    public void Open()
    {
        if (tooltipCanvas == null) return;
        foreach (var tooltip in FindObjectsByType<HubGameTooltip>(FindObjectsSortMode.None))
        {
            tooltip.Close();
        }
        tooltipCanvas.SetActive(true);
    }

    public void Close()
    {
        if (tooltipCanvas != null) tooltipCanvas.SetActive(false);
    }

    public void StartGame()
    {
        if (entry == null || tooltipCanvas == null || !tooltipCanvas.activeInHierarchy) return;
        Close();
        entry.Enter();
    }
}
