using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;


public class TwoCrossbow : LoadoutBase {  

public GameObject ArrowProjectile;
public GameObject DefenseProjectile;
GameObject player;

    void Start()
    {
        ArrowProjectile = GameObject.FindGameObjectWithTag("player").GetComponentInChildren<TwoXArrowLogic>().gameObject;
        DefenseProjectile = GameObject.FindGameObjectWithTag("player").GetComponentInChildren<KnockBackDefense>().gameObject;
        player = GameObject.FindGameObjectWithTag("player");
        _attackSpeed = _attackSpeed * 0.5f;
    }
    public override IEnumerator LightAttack(Vector2 MousePos)
    {
        Instantiate(ArrowProjectile, player.transform.position, Quaternion.identity);
        var arrow = ArrowProjectile.GetComponent<TwoXArrowLogic>();
        arrow.Init(10f, MousePos);

        yield return new WaitForSeconds(0);
    }

    public override IEnumerator HeavyAttack(Vector2 MousePos)
    {
          for(int i = 0; i<20; i++){
          Instantiate(ArrowProjectile, player.transform.position, Quaternion.identity);
          var arrow = ArrowProjectile.GetComponent<TwoXArrowLogic>();
          arrow.Init(10f, MousePos);
          yield return new WaitForSeconds(0.01f);
          }
          yield return new WaitForSeconds(0);
    }

    public override IEnumerator LightDash(Vector2 direction, Transform transform, Vector2 mousePos)
    {
         base.LightDash(direction, transform, mousePos);
         LightAttack(mousePos);
         LightAttack(mousePos);
         yield return new WaitForSeconds(0);
    }

    public override IEnumerator HeavyDash(Vector2 direction, Transform transform, Vector2 mousePos)
    {
          RaycastHit hit;
          if (Physics.Raycast(transform.position, mousePos, out hit, 1000))
          {
               var pos = hit.collider.transform.position;
               float dashDuration = 0.2f;

               direction.Normalize();

               Vector3 start = transform.position;
               Vector3 end = pos;

               float t = 0f;
               while (transform.position != end)
               {
                    t += Time.deltaTime / dashDuration;
                    transform.position = Vector3.Lerp(start, end, t);
                    yield return null;
               }
          }
          
          yield return new WaitForSeconds(0);
    }

    public override IEnumerator Defense(Vector2 mousePos)
    {
          Instantiate(DefenseProjectile, player.transform.position, Quaternion.identity);
          var defenseArrow = DefenseProjectile.GetComponent<KnockBackDefense>();
          defenseArrow.Init(mousePos);
          yield return new WaitForSeconds(0);
    }
}