using System;

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Player : MonoBehaviour
{
    [SerializeField] float _health = 100;
    UI _ui;
    float _score = 0;
    public event Action<GameObject /*killer*/> OnDied;
    
    bool _isRespawning;

    private void Start()
    {
        _ui = FindAnyObjectByType<UI>();
    }
    public void TakeDamage(float damage, GameObject attacker)
    {
        _health -= damage;
        if (_health <= 0)
        {
            OnDied?.Invoke(attacker);
            //Destroy(gameObject);
            StartCoroutine(RespawnRoutine());
        }
        _ui.updateHealth(_health);
    }

    public void IncreaseScore(float scoreIncrease)
    {
        _score += scoreIncrease;
        _ui.updateScore(_score);
    }

    public void Heal(float health)
    {
        _health += health;
        _ui.updateHealth(_health);
    }

    public void TeleportTo(Vector3 worldPos)
    {
        transform.position = worldPos;
    }
    IEnumerator RespawnRoutine() {
        _isRespawning = true;
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        yield return new WaitForSeconds(0.2f);
        _health = 100;
        _ui.updateHealth(_health);

        if (col) col.enabled = true;
        _isRespawning = false;
    }



}
