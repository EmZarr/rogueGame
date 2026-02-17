using UnityEngine;
using UnityEngine.UIElements;

public class PlayerIndicator : MonoBehaviour
{

    [SerializeField] private SpriteRenderer _dashIndicator;
    int mask;
    void Start()
    {
        mask = LayerMask.GetMask("Wall");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Activate(Vector2 direction, float maxDistance)
    {

        Vector2 start = transform.parent.position;
        Vector2 end = ComputeEnd(start, direction, maxDistance);
        float distance = Vector2.Distance(start, end);
        direction = direction.normalized;

        _dashIndicator.enabled = true;
        _dashIndicator.gameObject.transform.position = start + direction * (distance * 0.5f);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle+90f);
        _dashIndicator.gameObject.transform.rotation = rotation;

        _dashIndicator.transform.localScale = new Vector3(1f, distance, 1f);


    }

    public void Deactivate() {
        _dashIndicator.enabled = false;
    }

    Vector2 ComputeEnd(Vector2 start, Vector2 dir, float maxDistance)
    {
        dir.Normalize();

        RaycastHit2D hit = Physics2D.Raycast(start, dir, maxDistance, mask);

        if (hit.collider != null)
        {
            return hit.point - dir * 0.05f;
        }

        return start + dir * maxDistance;
    }
}
