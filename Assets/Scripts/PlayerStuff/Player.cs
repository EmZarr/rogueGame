using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] float _health = 100;
    [SerializeField] UI _ui;
    float _score = 0;

    public void TakeDamage(float damage)
    {
        _health -= damage;
        if (_health <= 0)
        {
            Destroy(gameObject);
        }
        _ui.updateHealth(_health);
    }

    public void IncreaseScore(float scoreIncrease)
    {
        _score += scoreIncrease;
        _ui.updateScore(_score);
    }
}
