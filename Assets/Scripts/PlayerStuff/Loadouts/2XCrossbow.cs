using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;


public class TwoCrossbow : LoadoutBase {  

public GameObject ArrowProjectile;
public GameObject DefenseProjectile;

    public TwoCrossbow(Player player) : base(player)
    {
        ArrowProjectile = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<TwoXArrowLogic>().gameObject;
        DefenseProjectile = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<KnockBackDefense>().gameObject;
        _attackSpeed = 0.5f;
        _lightDamage = 5f;
    }

    public override IEnumerator LightAttack(Vector2 mousePos)
    {
       
        yield return LightAttackAttack(mousePos);
        yield return new WaitForSeconds(_attackSpeed);
    }

    public IEnumerator LightAttackAttack(Vector2 mousePos)
    {
        GameObject arrowObj = UnityEngine.GameObject.Instantiate(ArrowProjectile, _player.transform.position, Quaternion.identity);
        var _arrowRenderer = arrowObj.GetComponent<SpriteRenderer>();
        var _arrowCollider = arrowObj.GetComponent<Collider2D>();
        _arrowRenderer.enabled = true;
        _arrowCollider.enabled = true;
        var arrow = arrowObj.GetComponent<TwoXArrowLogic>();
        arrow.Init(_lightDamage, mousePos, false, false);
        yield return null; 
    }

    public override IEnumerator HeavyAttack(Vector2 mousePos)
    {
          for(int i = 0; i<20; i++){
          yield return LightAttackAttack(mousePos);

          yield return new WaitForSeconds(0.12f);
          }
          yield return new WaitForSeconds(_attackSpeed);
    }

    public override IEnumerator LightDash(Vector2 direction, Transform transform, Vector2 mousePos)
    {
         yield return base.LightDash(direction, transform, mousePos);
         yield return LightAttackAttack(mousePos);
         yield return new WaitForSeconds(0.1f);
         yield return LightAttackAttack(mousePos);
    }

    public override IEnumerator HeavyDash(Transform transform, Vector2 mousePos)
    {
        Debug.Log("Heavy Dash");

        Vector2 origin = transform.position;
        Vector2 dir = getMouseDir(mousePos);

        //Debug.DrawLine(origin, origin + dir * 20f, Color.red, 2.5f);

        int playerLayer = LayerMask.NameToLayer("default");
        int mask = LayerMask.GetMask("Terrain");

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            dir,
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
                    yield return LightAttackAttack(mousePos);
                    counter = 0f;
                }
                

                yield return null;
            }
        }

        yield break;
    }


    public override IEnumerator Defense(Vector2 mousePos)
    {
          GameObject defenseBlast = UnityEngine.GameObject.Instantiate(DefenseProjectile, _player.transform.position, Quaternion.identity);
          var _arrowRenderer = defenseBlast.GetComponent<SpriteRenderer>();
          var _arrowCollider = defenseBlast.GetComponent<BoxCollider2D>();

          _arrowRenderer.enabled = true;
          _arrowCollider.enabled = true;
          var defenseArrow = defenseBlast.GetComponent<KnockBackDefense>();
          defenseArrow.Init(mousePos);
          yield return new WaitForSeconds(0);
    }
}