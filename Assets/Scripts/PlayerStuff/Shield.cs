using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Shield : MonoBehaviour
{
    public bool isParrying = false;
    public bool isBlocking = false;

    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D col;

    public void Activate(float duration, Vector2 dir)
    {
        if (sr) sr.enabled = true;
        if (col) col.enabled = true;
        isBlocking = true;

        float shieldDistance = 1f;

        Vector2 playerPos = transform.parent.position;

        // world-space placement
        Vector3 pos = playerPos + dir * shieldDistance;
        pos.z = transform.position.z;
        transform.position = pos;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        CancelInvoke();
        Invoke(nameof(Deactivate), duration);
    }

    void Deactivate()
    {
        if (sr) sr.enabled = false;
        if (col) col.enabled = false;
        isBlocking = false;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Projectile"))
        {
            if (!other.gameObject.GetComponent<Projectile>().GetUnblockable())
            {
                if (!isParrying)
                {
                    Destroy(other.gameObject);
                }
                else
                {
                    //Destroy(other.gameObject);
                    // TODO: Parry stuff
                }
            }
        }
    }
}
