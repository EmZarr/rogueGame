using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class LoadoutBase : MonoBehaviour
{

    [Header("Left Click")]
    protected float _lightWindup = 0.1f;
    protected float _lightDamage = 10f;
    protected float _heavyDamage = 30f;
    protected float _attackSpeed = 0.5f;

    [Header("Right Click")]
    protected float _defCD = 2f;


    [Header("Space")]
    protected float _heavyDashCD = 5f;
    protected float _lightDashCD = 1f;

    public virtual IEnumerator LightAttack(Vector2 mousePos)
    {
        yield return new WaitForSeconds(_lightWindup);
        Debug.Log("Do light attack");
        yield return new WaitForSeconds(_attackSpeed);
    }

    public virtual IEnumerator HeavyAttack(Vector2 mousePos) {
        Debug.Log("Do heavy attack");
        yield return new WaitForSeconds(_attackSpeed);
    }

    public virtual IEnumerator LightDash(Vector2 direction, Transform transform)
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
    public virtual IEnumerator HeavyDash(Vector2 direction, Transform transform)
    {
        float dashDistance = 12f;
        float dashDuration = 0.2f;

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

    public virtual IEnumerator Defense(Vector2 mousePos)
    {
        Debug.Log("Do defense");
        yield return new WaitForSeconds(0.1f);
    }

    public float getLightDashCD()
    {
        return _lightDashCD;
    }

    public float getHeavyDashCD()
    {
        return _heavyDashCD;
    }

    public float getDefenseCD()
    {
        return _defCD;
    }
}
