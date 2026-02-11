using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAnimDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Range(0.1f, 0.99f)]
    [SerializeField] private float forwardDotThreshold = 0.7071f;

    private Enemy enemy;
    private Vector2 lookDir = Vector2.down; // remembered facing
    private Vector3 lastPos;

    private static readonly int LookX = Animator.StringToHash("LookX");
    private static readonly int LookY = Animator.StringToHash("LookY");
    private static readonly int MoveType = Animator.StringToHash("MoveType");
    private static readonly int InAction = Animator.StringToHash("InAction");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Dash = Animator.StringToHash("Dash");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int Block = Animator.StringToHash("Block");

    public void TriggerAttack() => animator.SetTrigger(Attack);
    public void TriggerDash() => animator.SetTrigger(Dash);
    public void TriggerHurt() => animator.SetTrigger(Hurt);
    public void TriggerBlock() => animator.SetTrigger(Block);

    public void SetInAction(bool inAction) => animator.SetBool(InAction, inAction);
    public Animator GetAnimator() => animator;

    private GuardianProtectZone guardianShield;


    void Awake()
    {
        enemy = GetComponent<Enemy>();
        lastPos = transform.position;

        if (enemy._data.enemyType == EnemyType.Guardian)
        {
            guardianShield = GetComponentInChildren<GuardianProtectZone>();
        }
    }

    public void Tick()
    {
        Vector2 vel = GetVelocity2D();

        bool locked = enemy.IsStunned || animator.GetBool(InAction);

        if (!locked)
        {
            if (enemy._data.enemyType == EnemyType.Guardian && guardianShield != null)
            {
                //face shield
                Vector2 toShield = (Vector2)(guardianShield.transform.position - transform.position);
                if (toShield.sqrMagnitude > 0.0001f)
                    lookDir = toShield.normalized;
            }
            else
            {
                //face player
                var player = enemy.GetPlayer();
                if (player != null)
                {
                    Vector2 toPlayer = (Vector2)(player.transform.position - transform.position);
                    if (toPlayer.sqrMagnitude > 0.0001f)
                        lookDir = toPlayer.normalized;
                }
            }
        }

        animator.SetFloat(LookX, lookDir.x);
        animator.SetFloat(LookY, lookDir.y);
        animator.SetInteger(MoveType, ComputeMoveType(lookDir, vel));
    }

    Vector2 GetVelocity2D()
    {
        var agent = enemy.GetAgent();

        Vector2 vel;
        if (agent != null && agent.enabled)
        {
            vel = agent.velocity;
        }
        else
        {
            Vector3 dp = transform.position - lastPos;
            vel = (Vector2)(dp / Mathf.Max(Time.deltaTime, 0.0001f));
        }

        lastPos = transform.position;

        // deadzone to prevent flicker
        if (vel.sqrMagnitude < 0.05f * 0.05f) vel = Vector2.zero;
        return vel;
    }

    int ComputeMoveType(Vector2 look, Vector2 velocity)
    {
        if (velocity.sqrMagnitude < 0.0001f) return 0;

        Vector2 moveDir = velocity.normalized;
        float dot = Vector2.Dot(moveDir, look);

        if (dot >= forwardDotThreshold) return 1;
        if (dot <= -forwardDotThreshold) return 2;

        float crossZ = look.x * moveDir.y - look.y * moveDir.x;
        return crossZ > 0f ? 3 : 4;
    }

    public IEnumerator RunAction(float duration, int triggerHash)
    {
        SetInAction(true);

        float prevSpeed = animator.speed;

        animator.SetTrigger(triggerHash);

        yield return null; // allow transition to begin

        var infos = animator.GetCurrentAnimatorClipInfo(0);
        float clipLen = (infos != null && infos.Length > 0 && infos[0].clip != null)
            ? infos[0].clip.length
            : 0f;

        if (clipLen > 0.001f && duration > 0.001f)
            animator.speed = clipLen / duration;
        else
            animator.speed = 1f;

        yield return new WaitForSeconds(duration);

        animator.speed = prevSpeed;
        SetInAction(false);
    }


}
