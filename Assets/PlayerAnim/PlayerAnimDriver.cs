using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PlayerAnimDriver : MonoBehaviour
{
    public enum WeaponId { Shield = 0, Dual = 1, Bow = 2 }

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Controller / Overrides")]
    [Tooltip("Your base Animator Controller (the logic controller). If Shield is your base, this is the shield-looking default.")]
    [SerializeField] private RuntimeAnimatorController baseController;
    [SerializeField] private AnimatorOverrideController knightOverride; //if null uses base
    [SerializeField] private AnimatorOverrideController rogueOverride;
    [SerializeField] private AnimatorOverrideController rangerOverride;

    [Header("Weapon Layers")]
    [SerializeField] private string shieldLayerName = "KnightLayer";
    [SerializeField] private string dualLayerName = "RogueLayer";
    [SerializeField] private string bowLayerName = "RangerLayer";

    [Header("MoveType thresholds")]
    [Range(0.1f, 0.99f)]
    [SerializeField] private float forwardDotThreshold = 0.7071f;

    // Cached
    private int knightLayer, rogueLayer, rangerLayer;

    // Cached parameters
    private static readonly int LookXHash = Animator.StringToHash("LookX");
    private static readonly int LookYHash = Animator.StringToHash("LookY");
    private static readonly int MoveTypeHash = Animator.StringToHash("MoveType");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int DefenseHash = Animator.StringToHash("Defense");

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int SpecialHash = Animator.StringToHash("Special");
    private static readonly int DashHash = Animator.StringToHash("Dash");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    //private static readonly int ParryHash = Animator.StringToHash("Parry"); //might be needed later

    // getter if like we need to read current weapon, maybe like usefull for ui and shit
    public WeaponId CurrentWeapon { get; private set; } = WeaponId.Shield;

    void Awake()
    {
        knightLayer = animator.GetLayerIndex(shieldLayerName);
        rogueLayer = animator.GetLayerIndex(dualLayerName);
        rangerLayer = animator.GetLayerIndex(bowLayerName);
    }

    //
    public void UpdateLocomotion(Vector2 lookDir, Vector2 velocityWorld)
    {
        // stops tweaking issue when mouse is exactly at player position, thanks chat
        if (lookDir.sqrMagnitude < 0.0001f) lookDir = Vector2.down;
        else lookDir.Normalize();

        animator.SetFloat(LookXHash, lookDir.x);
        animator.SetFloat(LookYHash, lookDir.y);

        int moveType = ComputeMoveType(lookDir, velocityWorld);
        animator.SetInteger(MoveTypeHash, moveType);
    }

    //Animation
    public int ComputeMoveType(Vector2 lookDir, Vector2 velocityWorld)
    {
        if (velocityWorld.sqrMagnitude < 0.0001f) //make idle stable
            return 0;

        Vector2 moveDir = velocityWorld.normalized;

        float dot = Vector2.Dot(moveDir, lookDir);

        if (dot >= forwardDotThreshold) return 1;
        if (dot <= -forwardDotThreshold) return 2;

        float crossZ = lookDir.x * moveDir.y - lookDir.y * moveDir.x;

        return crossZ > 0f ? 3 : 4;
    }

    public void EquipWeapon(WeaponId weapon)
    {
        CurrentWeapon = weapon;

        // Swap overrides
        switch (weapon)
        {
            case WeaponId.Shield:
                animator.runtimeAnimatorController = knightOverride != null ? knightOverride : baseController;
                break;
            case WeaponId.Dual:
                animator.runtimeAnimatorController = rogueOverride != null ? rogueOverride : baseController;
                break;
            case WeaponId.Bow:
                animator.runtimeAnimatorController = rangerOverride != null ? rangerOverride : baseController;
                break;
        }

        // again might not be needed if layers arent
        animator.SetLayerWeight(knightLayer, weapon == WeaponId.Shield ? 1f : 0f);
        animator.SetLayerWeight(rogueLayer, weapon == WeaponId.Dual ? 1f : 0f);
        animator.SetLayerWeight(rangerLayer, weapon == WeaponId.Bow ? 1f : 0f);
    }

    public IEnumerator PlayTaggedStateForDuration(string tag, float desiredDuration)
    {
        AnimatorStateInfo st;
        do
        {
            st = animator.GetCurrentAnimatorStateInfo(0);
            yield return null;
        }
        while (!st.IsTag(tag));

        // Get the clip length
        var clips = animator.GetCurrentAnimatorClipInfo(0);
        float clipLen = (clips != null && clips.Length > 0) ? clips[0].clip.length : 1f;

        // Scale animation speed
        float prevSpeed = animator.speed;
        animator.speed = (desiredDuration > 0.001f) ? (clipLen / desiredDuration) : 1f;

        // Handle looping clips
        while (true)
        {
            st = animator.GetCurrentAnimatorStateInfo(0);
            if (!st.IsTag(tag)) break;                  // transitioned
            if (st.normalizedTime >= 1f) break;         // finished
            yield return null;
        }

        animator.speed = prevSpeed;
    }

    public IEnumerator WaitUntilTaggedNormalizedTime(string tag, float normalizedT)
    {
        AnimatorStateInfo st;
        do
        {
            st = animator.GetCurrentAnimatorStateInfo(0);
            yield return null;
        }
        while (!st.IsTag(tag));

        while (true)
        {
            st = animator.GetCurrentAnimatorStateInfo(0);
            if (!st.IsTag(tag)) break;
            if (st.normalizedTime >= normalizedT) break;
            yield return null;
        }
    }

    //Actions
    public void TriggerAttack() => animator.SetTrigger(AttackHash);
    public void TriggerSpecial() => animator.SetTrigger(SpecialHash);
    public void TriggerDash() => animator.SetTrigger(DashHash);
    public void TriggerHurt() => animator.SetTrigger(HurtHash);
    public void TriggerDefense() => animator.SetTrigger(DefenseHash);

    public void SetDead(bool dead) => animator.SetBool(DeadHash, dead);

}
