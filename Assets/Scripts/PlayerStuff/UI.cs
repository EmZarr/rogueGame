using TMPro;
using UnityEngine;
using TMPro;

public class UI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI healthText;
    [SerializeField] TextMeshProUGUI buffsText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void updateScore(float score)
    {
        scoreText.text = score.ToString("0");
    }
    public void updateHealth(float score)
    {
        healthText.text = score.ToString("0");
    }

    public void updateBuffs(float attackSpeed, float moveSpeed, float damage)
    {
        buffsText.text = " ATTACK SPEED: x" + attackSpeed.ToString()
            + "\n MOVEMENT SPEED: x" + moveSpeed.ToString()
            + "\n DAMAGE : x" + damage.ToString();
    }
}
