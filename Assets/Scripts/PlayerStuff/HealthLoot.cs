using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] float healthAmount;
    [SerializeField] Animator _barrelAnimator;
    Player player;
    Collider2D _collider;

    void Start()
    {
        player = FindFirstObjectByType<Player>();
        _collider = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player.Heal(healthAmount);

            // prevent re-triggering
            _collider.enabled = false;

            // play explosion animation
            _barrelAnimator.SetTrigger("Explode");
        }
    }

    // This will be called by the animation
    public void DestroyBarrel()
    {
        Destroy(gameObject);
    }
}
