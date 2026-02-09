using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class SpecialCooldown : MonoBehaviour
{
    [SerializeField] private Image imageCooldown;
    [SerializeField] private TMP_Text textCooldown;
    [SerializeField] private Image imageEdge;

    private LoadoutState state;

    void OnEnable()
    {
        LevelManager.OnPlayerSpawned += AttachPlayer;
    }

    void OnDisable()
    {
        LevelManager.OnPlayerSpawned -= AttachPlayer;
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
