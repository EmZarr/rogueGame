using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;


public class TwoCrossbow : LoadoutBase {  

public GameObject ArrowProjectile;
public GameObject DefenseProjectile;
GameObject playerObj;


    public TwoCrossbow(Player player) : base(player)
    {
        ArrowProjectile = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<TwoXArrowLogic>().gameObject;
        DefenseProjectile = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<KnockBackDefense>().gameObject;
        playerObj = GameObject.FindGameObjectWithTag("Player");
        _attackSpeed = _attackSpeed * 0.5f;
    }

    public override IEnumerator LightAttack(Vector2 MousePos)
    {
        GameObject arrowObj = Instantiate(ArrowProjectile, player.transform.position, Quaternion.identity);
        var _arrowRenderer = arrowObj.GetComponent<SpriteRenderer>();
        var _arrowCollider = arrowObj.GetComponent<Collider2D>();
        _arrowRenderer.enabled = true;
        _arrowCollider.enabled = true;
        var arrow = arrowObj.GetComponent<TwoXArrowLogic>();
        arrow.Init(10f, MousePos, false);

        yield return new WaitForSeconds(0);
    }

    public override IEnumerator HeavyAttack(Vector2 MousePos)
    {
          for(int i = 0; i<40; i++){
          GameObject arrowObj = Instantiate(ArrowProjectile, player.transform.position, Quaternion.identity);
            var _arrowRenderer = arrowObj.GetComponent<SpriteRenderer>();
            var _arrowCollider = arrowObj.GetComponent<Collider2D>();
            _arrowRenderer.enabled = true;
            _arrowCollider.enabled = true;
            var arrow = arrowObj.GetComponent<TwoXArrowLogic>();
            arrow.Init(10f, MousePos, true);

          yield return new WaitForSeconds(0.12f);
          }
          yield return new WaitForSeconds(0);
    }

    public override IEnumerator LightDash(Vector2 direction, Transform transform, Vector2 mousePos)
    {
         Debug.Log("hello?");
         yield return base.LightDash(direction, transform, mousePos);
         Debug.Log("here?");
         yield return LightAttack(mousePos);
         yield return new WaitForSeconds(0.1f);
         yield return LightAttack(mousePos);
    }

    public override IEnumerator HeavyDash(Transform transform, Vector2 mousePos)
    {
          Debug.Log("Heavy");
          Debug.DrawLine(transform.position, (Vector3)mousePos*1000, Color.black, 2.5f);
          

          RaycastHit hit;
          if (Physics.Raycast(transform.position, (Vector3)mousePos.normalized, out hit, 1000))
          {
               var pos = hit.collider.transform.position;
               float dashDuration = 0.2f;

               direction.Normalize();
               Debug.Log("Testing");
               Vector3 start = transform.position;
               Vector3 end = pos;

               float t = 0f;
               while (transform.position != end)
               {
                    t += Time.deltaTime / dashDuration;
                    transform.position = Vector3.Lerp(start, end, t);
                    yield return LightAttack(mousePos);
                    //yield return null;
               }
          }
          
          yield return new WaitForSeconds(0);
    }

    public override IEnumerator Defense(Vector2 mousePos)
    {
          GameObject defenseBlast = Instantiate(DefenseProjectile, player.transform.position, Quaternion.identity);
          var _arrowRenderer = defenseBlast.GetComponent<SpriteRenderer>();
          var _arrowCollider = defenseBlast.GetComponent<BoxCollider2D>();

          _arrowRenderer.enabled = true;
          _arrowCollider.enabled = true;
          var defenseArrow = defenseBlast.GetComponent<KnockBackDefense>();
          defenseArrow.Init(mousePos);
          yield return new WaitForSeconds(0);
    }
}