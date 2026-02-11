using UnityEngine;
using UnityEngine.AI;

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

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        lastPos = transform.position;
    }

    /// Call this EVERY FRAME
    public void Tick()
    {
        Vector2 vel = GetVelocity2D();

        bool locked = enemy.IsStunned || animator.GetBool(InAction);
        if (!locked)
            FacePlayerOrKeepDefault();

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

    void FacePlayerOrKeepDefault()
    {
        var player = enemy.GetPlayer();
        if (player == null) return;

        Vector2 toPlayer = (Vector2)(player.transform.position - transform.position);
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        lookDir = toPlayer.normalized;
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

    public void SetInAction(bool inAction) => animator.SetBool(InAction, inAction);
}
