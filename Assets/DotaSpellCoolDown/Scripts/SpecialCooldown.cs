using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using static UnityEditor.Experimental.GraphView.GraphView;

public class SpecialCooldown : MonoBehaviour
{
    [SerializeField] private Image imageCooldown;
    [SerializeField] private TMP_Text textCooldown;
    [SerializeField] private Image imageEdge;

    private LoadoutState state;

    private void OnEnable()
    {
        MapInstantiator.OnPlayerSpawned += HandlePlayerSpawned;

        var p = MapInstantiator.CurrentPlayer;
        if (p != null) HandlePlayerSpawned(p);
    }

    private void OnDisable() => MapInstantiator.OnPlayerSpawned -= HandlePlayerSpawned;

    void HandlePlayerSpawned(Player p) {
        if (p == null) return;
        state = p.GetComponent<LoadoutState>();
        HideUI();
    }


    private void AttachPlayer(Player player)
    {
        state = player.GetComponent<LoadoutState>();
        HideUI();
    }

    void Update()
    {
        if (state == null) return;

        float remaining = state.GetSpecialCD();
        float total = state.GetLoadout().GetHeavyDashCD2();

        if (remaining <= 0f || total <= 0f)
        {
            HideUI();
            return;
        }

        ShowUI();

        textCooldown.text = Mathf.CeilToInt(remaining).ToString();

        float normalized = remaining / total;
        imageCooldown.fillAmount = normalized;
        imageEdge.transform.localEulerAngles =
            new Vector3(0f, 0f, 360f * normalized);
    }

private void ShowUI()
    {
        if (!textCooldown.gameObject.activeSelf)
        {
            textCooldown.gameObject.SetActive(true);
            imageEdge.gameObject.SetActive(true);
        }
    }

    private void HideUI()
    {
        textCooldown.gameObject.SetActive(false);
        imageEdge.gameObject.SetActive(false);
        imageCooldown.fillAmount = 0f;
    }
}
