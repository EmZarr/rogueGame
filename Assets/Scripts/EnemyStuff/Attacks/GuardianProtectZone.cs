using System;
using System.Collections;
using UnityEngine;

public class GuardianProtectZone : DamageZone
{

    private GameObject _player;

    private bool _animating;

    private void Start()
    {
        _player = GameObject.FindWithTag("Player");
    }


    protected override void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player != null)
        {
            player.TakeDamage(_dmg, transform.parent.gameObject);

            if (knockBack)
            {
                var direction = other.transform.position - transform.position;
                player.GetKnockedBack(direction, 4.0f);
            }

            if (!_animating)
            {
                Vector2 hitDir = (other.transform.position - transform.position).normalized;
                StartCoroutine(ShieldPush(hitDir));
            }
        }

        var projectile = other.GetComponent<TwoXArrowLogic>();
        if (projectile != null)
        {
            projectile.Init(
                5f,
                (Vector2)_player.transform.position,
                false,
                true
            );
        }
    }


    private IEnumerator ShieldPush(Vector2 direction)
    {
        _animating = true;
        var startPos = transform.localPosition;
        Vector3 pushOffset = (Vector3)direction * 0.6f;
        Vector3 targetPos = startPos + pushOffset;

        float t = 0f;

        // Push forward
        while (t < 1f)
        {
            t += Time.deltaTime / 0.1f;
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        t = 0f;

        // Pull back
        while (t < 1f)
        {
            t += Time.deltaTime / 0.1f;
            transform.localPosition = Vector3.Lerp(targetPos, startPos, t);
            yield return null;
        }

        transform.localPosition = startPos;
        _animating = false;
    }

}