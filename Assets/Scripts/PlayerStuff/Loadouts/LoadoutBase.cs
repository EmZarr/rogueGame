using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class LoadoutBase : MonoBehaviour
{

    [Header("Left Click")]
    protected float _lightWindup = 0.1f;
    protected float _lightDamage = 5f;
    protected float _heavyDamage = 10f;
    protected float _attackSpeed = 1f;

    [Header("Right Click")]
    protected float _defCost = 50f;


    [Header("Space")]
    protected float _heavyDashCost = 100f;

    public virtual IEnumerator lightAttack(Vector2 MousePos)
    {
        yield return new WaitForSeconds(_lightWindup);
        Debug.Log("Do light attack");
        yield return new WaitForSeconds(_attackSpeed);
    }

    public virtual IEnumerator heavyAttack(Vector2 MousePos) {
        Debug.Log("Do heavy attack");
        yield return new WaitForSeconds(_attackSpeed);
    }

    public virtual IEnumerator lightDash(Vector2 direction)
    {
        float dashDistance = 4f;
        float dashDuration = 0.15f;

        direction.Normalize();

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction * dashDistance);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dashDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }
    public virtual IEnumerator heavyDash(Vector2 Direction)
    {
        Debug.Log("Do heavy Dash");
        yield return new WaitForSeconds(0.1f);
    }
}
