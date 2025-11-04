using UnityEngine;

public class Shield : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Projectile"))
        {
            if (!other.gameObject.GetComponent<Projectile>().GetUnblockable())
            {
                Destroy(other.gameObject);
            }
        }
    }
}
