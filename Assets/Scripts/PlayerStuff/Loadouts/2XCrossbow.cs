using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;


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
        UnityEngine.Object.Instantiate(ArrowProjectile, playerObj.transform.position, Quaternion.identity);
        var arrow = ArrowProjectile.GetComponent<TwoXArrowLogic>();
        arrow.Init(10f, MousePos);

        yield return new WaitForSeconds(0);
    }

    public override IEnumerator HeavyAttack(Vector2 MousePos)
    {
          for(int i = 0; i<20; i++){
          UnityEngine.Object.Instantiate(ArrowProjectile, playerObj.transform.position, Quaternion.identity);
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

    public override IEnumerator HeavyDash(Transform transform, Vector2 mousePos)
    {
          var direction = getMouseDir(mousePos);
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
          UnityEngine.Object.Instantiate(DefenseProjectile, playerObj.transform.position, Quaternion.identity);
          var defenseArrow = DefenseProjectile.GetComponent<KnockBackDefense>();
          defenseArrow.Init(mousePos);
          yield return new WaitForSeconds(0);
    }
}