using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;


public class TwoCrossbow : LoadoutBase {  

    public GameObject ArrowProjectile;
    public GameObject _knockBack;
    public KnockBackDefense _knockBackHitBox;

    public TwoCrossbow(Player player) : base(player)
    {
        ArrowProjectile = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<Projectile>().gameObject;

        _knockBack = GameObject.FindGameObjectWithTag("DefArrow");
        if (_knockBack != null)
        {
            _knockBackHitBox = _knockBack.GetComponent<KnockBackDefense>();
        }
        _lightDamage = 5f;
        _defenseDuration = 0.2f;
    }

    public override IEnumerator LightAttack(Vector2 direction)
    {
        yield return new WaitForSeconds(GetLightAttackWindup());
        yield return LightAttackAttack(direction);
        yield return new WaitForSeconds(GetLightAttackDuration() - GetLightAttackWindup());
    }

    public IEnumerator LightAttackAttack(Vector2 direction)
    {

        GameObject arrowObj = UnityEngine.GameObject.Instantiate(ArrowProjectile, _player.transform.position, Quaternion.identity);
        var _arrowRenderer = arrowObj.GetComponent<SpriteRenderer>();
        var _arrowCollider = arrowObj.GetComponent<Collider2D>();
        _arrowRenderer.enabled = true;
        _arrowCollider.enabled = true;
        var arrow = arrowObj.GetComponent<Projectile>();
        arrow.Init(getLightDamage(), direction, true);
        yield return null;
    }

    public override IEnumerator HeavyAttack(Vector2 direction)
    {
          for(int i = 0; i<20; i++){
          yield return LightAttackAttack(direction);
          yield return new WaitForSeconds(0.12f);
          }
    }

    public override IEnumerator LightDash(Vector2 direction, Transform transform, Vector2 mousePos)
    {
         yield return base.LightDash(direction, transform, mousePos);
         yield return LightAttackAttack(mousePos);
         yield return new WaitForSeconds(0.1f);
         yield return LightAttackAttack(mousePos);
    }

    public override IEnumerator HeavyDash(Vector2 direction, Transform transform)
    {
        Debug.Log("Heavy Dash");

        Vector2 origin = transform.position;

        //Debug.DrawLine(origin, origin + dir * 20f, Color.red, 2.5f);

        //int playerLayer = LayerMask.NameToLayer("default");
        int mask = LayerMask.GetMask("Wall");

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            direction,
            1000f,
            mask
        );

        if (hit.collider != null)
        {
            Debug.Log("Testing (2D hit): " + hit.collider.name);
            
            Vector3 start = transform.position;
            Vector3 end = hit.point;               // better than collider.transform.position
            float dashDuration = (float) Vector3.Distance(_player.transform.position, end)*0.03f;
            yield return new WaitForSeconds(Vector3.Distance(_player.transform.position, end)*0.01f);
            float t = 0f;
            float counter = 0f;
            
            while (t < 1f)
            {
                t += Time.deltaTime / dashDuration;
                transform.position = Vector3.Lerp(start, end, t);
                counter += Time.deltaTime/dashDuration;
                if (counter > 0.1f)
                {
                    yield return LightAttackAttack(-direction);
                    counter = 0f;
                }
                

                yield return null;
            }
        }

        yield break;
    }

    public override IEnumerator Defense(Vector2 direction)
    {
        yield return AreaAttack(direction, _knockBack, _knockBackHitBox, 1.3f, _defenseDuration, 0f);
    }
}