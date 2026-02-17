using UnityEngine;

public class DashHit : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Player player;
    void OnTriggerEnter2D(Collider2D other)
    {
        player.OnHitboxTrigger(other);
    }
}
