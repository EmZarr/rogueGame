using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Shield : MonoBehaviour
{
    public bool isParrying = false;
    public bool isBlocking = false;

    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D col;
    [SerializeField] private LoadoutState loadoutState;

    TelemetryManager telemetryManager;

    private void Update()
    {
        if (isParrying || isBlocking)
        {
            float shieldDistance = 0.5f;

            Vector2 playerPos = transform.parent.position;
            var dir = loadoutState.getMouseDir();
            // world-space placement
            Vector3 pos = playerPos + dir * shieldDistance;
            pos.z = transform.position.z;
            transform.position = pos;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public void Activate(float duration, Vector2 dir)
    {
        loadoutState.SetSpeed(0.5f);
        if (sr) sr.enabled = true;
        if (col) col.enabled = true;
        isBlocking = true;
        CancelInvoke();
        Invoke(nameof(Deactivate), duration);
        telemetryManager = FindFirstObjectByType<TelemetryManager>();
    }

    void Deactivate()
    {
        if (sr) sr.enabled = false;
        if (col) col.enabled = false;
        isBlocking = false;
        loadoutState.SetSpeed(1f);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        var projectile = other.GetComponent<Projectile>();
        if (projectile != null)
        {
            if (!isParrying)
            {
                Destroy(other.gameObject);
                telemetryManager.defenseToEnemy[1, 2]+=1;
            }
            else
            {
                projectile.Reflect(loadoutState.getMouseDir());
                telemetryManager.defenseToEnemy[2, 2]+=1;
            }
        }
        else if (isParrying)
        {
            var bomb = other.GetComponent<Bomb>();
            if (bomb != null)
            {
                bomb.Reflect((Vector2)transform.position + (10f*loadoutState.getMouseDir()));
                telemetryManager.defenseToEnemy[2, 3]+=1;
            }
        }
    }
}
