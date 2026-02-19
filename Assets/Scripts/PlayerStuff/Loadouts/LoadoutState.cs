using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using static PlayerAnimDriver;
using UnityEngine.UIElements;

public class LoadoutState : MonoBehaviour
{

    [SerializeField] private Player player;

    [SerializeField] private PlayerAnimDriver anim;
    [SerializeField] private PlayerIndicator indicator;

    [SerializeField]private Rigidbody2D rb;
    private enum ActionType { AttackLight, AttackHeavy, DashLight, DashHeavy, Defense }

    private struct BufferedAction
    {
        public ActionType type;
        public float time;      // when it was queued
    }

    private ChargeUp chargeUpBar;

    TelemetryManager telemetryManager;

    [Header("Input Buffer")]
    [SerializeField] private float bufferWindow = 0.20f; // 200ms is typical
    private readonly Queue<BufferedAction> buffer = new Queue<BufferedAction>(4);
    private Coroutine actionRunner;
    private Coroutine dashLockRoutine;

    [Header("Movement")]
    public float accel = 100f;
    public float decel = 100f;

    private int loadoutNumber = 0;

    Vector2 vel;
    bool blockedMovement = false;
    bool blockedActions = false;
    bool doAnimationAnyway = false;
    bool showIndicator = false;


    private float currentSpeed;
    private float dashPressTime;
    private bool dashHeld;
    private float attackPressTime;
    private bool attackHeld;
    [SerializeField] private float heavyDashHoldTime = 0.4f;
    [SerializeField] private float heavyAttackHoldTime = 0.4f;

    private LoadoutBase loadout;
    private Vector2 mousePos;

    private float nextHeavyDashTime;
    private float nextDefTime;
    private float nextDashTime;
    private float nextAttackTime;

    private bool heavyAttackQueuedThisPress = false;

    [Header("Input (assign from your Controls asset)")]
    public InputActionReference move;   // Gameplay/Move (Vector2)
    public InputActionReference point;  // Gameplay/Look (Vector2) -> <Pointer>/position
    public InputActionReference def;  // Gameplay/Block (Button)
    public InputActionReference dash;   // Gameplay/Dash (Button)
    public InputActionReference attack;   // Gameplay/Dash (Button)

    public InputActionReference loadout1;
    public InputActionReference loadout2;
    public InputActionReference loadout3;  


    Camera cam;

    void Awake()
    {
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        cam = Camera.main;
    }
    void Start()
    {
        loadout = new SwordAndShield(player);
        currentSpeed = player.GetMoveSpeed();
        chargeUpBar = GameObject.FindWithTag("ChargeBar").GetComponent<ChargeUp>();
        nextHeavyDashTime = Time.time;
        telemetryManager = FindFirstObjectByType<TelemetryManager>();
        telemetryManager.SetLoadOut(2);

    }

    private void EnqueueAction(BufferedAction a)
    {
        // throw away stale requests if queue gets spammy
        if (buffer.Count >= 4) buffer.Dequeue();
        buffer.Enqueue(a);

        if (actionRunner == null)
            actionRunner = StartCoroutine(RunBufferedActions());
    }

    private IEnumerator RunBufferedActions()
    {
        while (buffer.Count > 0)
        {
            var a = buffer.Dequeue();

            // Expire old inputs (prevents "I pressed dash 2 seconds ago")
            if (Time.time - a.time > bufferWindow)
                continue;

            // Wait until we're free
            while (blockedActions) yield return null;



            float wait = 0f;
            switch (a.type)
            {
                case ActionType.AttackLight:
                case ActionType.AttackHeavy: wait = Mathf.Max(0f, nextAttackTime - Time.time); break;
                case ActionType.Defense: wait = Mathf.Max(0f, nextDefTime - Time.time); break;
                case ActionType.DashLight: wait = Mathf.Max(0f, nextDashTime - Time.time); break;
                case ActionType.DashHeavy: wait = Mathf.Max(0f, nextHeavyDashTime - Time.time); break;
            }

            if (wait > 0f)
            {
                if (wait > bufferWindow) continue;

                yield return new WaitForSeconds(wait);
            }

            // Execute one
            blockedActions = true;

            switch (a.type)
            {
                case ActionType.AttackLight:
                    yield return RunAction(loadout.GetLightAttackDuration(), loadout.LightAttack(getMouseDir()), ActionType.AttackLight, "Attack");
                    nextAttackTime = Time.time + loadout.GetLightAttackDuration();
                    telemetryManager.LightAttackCount(loadoutNumber);
                    break;

                case ActionType.AttackHeavy:
                    currentSpeed = player.GetMoveSpeed() * 0.0f;
                    heavyAttackQueuedThisPress = true;
                    yield return RunAction(loadout.GetHeavyAttackDuration(), loadout.HeavyAttack(getMouseDir()), ActionType.AttackHeavy, "Special");
                    nextAttackTime = Time.time + loadout.GetLightAttackDuration();
                    currentSpeed = player.GetMoveSpeed();
                    telemetryManager.HeavyAttackCount(loadoutNumber);
                    break;

                case ActionType.DashLight:
                    //yield return DoDash(heavy: false);
                    blockedMovement = true;
                    yield return RunAction(loadout.GetLightDashDuration(), loadout.LightDash(vel, transform, getMouseDir()), ActionType.DashLight, "Dash");
                    nextDashTime = Time.time + loadout.getLightDashCD();
                    SetMovementBlocked(false);
                    telemetryManager.LightDashCount(loadoutNumber);
                    break;

                case ActionType.DashHeavy:
                    SetMovementBlocked(true);
                    yield return RunAction(loadout.GetHeavyDashDuration(), loadout.HeavyDash(getMouseDir(), transform), ActionType.DashHeavy, "Dash");
                    nextHeavyDashTime = Time.time + loadout.getHeavyDashCD();
                    SetMovementBlocked(false);
                    telemetryManager.HeavyDashCount(loadoutNumber);
                    //yield return DoDash(heavy: true);
                    break;

                case ActionType.Defense:
                    doAnimationAnyway = true;
                    yield return RunAction(loadout.GetDefenseDuration(), loadout.Defense(getMouseDir()), ActionType.Defense, "Defense");
                    doAnimationAnyway = false;
                    nextDefTime = Time.time + loadout.getDefenseCD();
                    telemetryManager.DefenseCount(loadoutNumber);
                    break;
            }

            blockedActions = false;
            updatePlayerAni();
        }

        actionRunner = null;
    }

    void OnEnable()
    {
        attack.action.started += OnAttackStarted;
        attack.action.canceled += OnAttackCanceled;
        dash.action.started += OnDashStarted;
        dash.action.canceled += OnDashCanceled;
        def.action.performed += OnDefense;

        loadout1.action.performed += OnLoadout1;
        loadout2.action.performed += OnLoadout2;
        loadout3.action.performed += OnLoadout3;

        loadout1.action.Enable();
        loadout2.action.Enable();
        loadout3.action.Enable();

        move.action.Enable();
        point.action.Enable();
        def.action.Enable();
        attack.action.Enable();
        dash.action.Enable();

    }

    void OnDisable()
    {
        attack.action.started -= OnAttackStarted;
        attack.action.canceled -= OnAttackCanceled;
        dash.action.started -= OnDashStarted;
        dash.action.canceled -= OnDashCanceled;
        def.action.performed -= OnDefense;

        loadout1.action.performed -= OnLoadout1;
        loadout2.action.performed -= OnLoadout2;
        loadout3.action.performed -= OnLoadout3;

        loadout1.action.Disable();
        loadout2.action.Disable();
        loadout3.action.Disable();

        move.action.Disable();
        point.action.Disable();
        def.action.Disable();
        attack.action.Disable();
        dash.action.Disable();
    }

    void OnLoadout1(InputAction.CallbackContext ctx)
    {
        anim.EquipWeapon(WeaponId.Bow);
        loadout = new TwoCrossbow(player);
        loadoutNumber = 1;
        telemetryManager.SetLoadOut(loadoutNumber);
    }

    void OnLoadout2(InputAction.CallbackContext ctx)
    {
        anim.EquipWeapon(WeaponId.Shield);
        loadout = new SwordAndShield(player);
        loadoutNumber = 2;
        telemetryManager.SetLoadOut(loadoutNumber);
    }

    void OnLoadout3(InputAction.CallbackContext ctx)
    {
        anim.EquipWeapon(WeaponId.Dual);
        loadout = new DualSwords(player);
        loadoutNumber = 3;
        telemetryManager.SetLoadOut(loadoutNumber);
    }

    void OnAttackStarted(InputAction.CallbackContext ctx)
    {
        attackHeld = true;
        attackPressTime = Time.time;
        heavyAttackQueuedThisPress = false;
    }

    void OnAttackCanceled(InputAction.CallbackContext ctx)
    {
        if ((Time.time - attackPressTime) < heavyAttackHoldTime)
        {
            EnqueueAction(new BufferedAction
            {
                type = ActionType.AttackLight,
                time = Time.time
            });
        }
        /*else
        {
            EnqueueAction(new BufferedAction
            {
                type = ActionType.AttackHeavy,
                time = Time.time
            });
        }*/
        attackHeld = false;
    }

    void OnDashStarted(InputAction.CallbackContext ctx)
    {
        dashHeld = true;
        dashPressTime = Time.time;
        if (Time.time > nextHeavyDashTime)
        {
            chargeUpBar.SetChargeBar(true);
            showIndicator = true;
        }
        dashLockRoutine = StartCoroutine(LockMovementAfterDelay());
    }

    IEnumerator LockMovementAfterDelay()
    {
        yield return new WaitForSeconds(heavyDashHoldTime);

        if (Time.time < nextHeavyDashTime)
        {
            yield return new WaitForSeconds(nextHeavyDashTime-Time.time);
        }

        blockedMovement = true;
    }

    void OnDashCanceled(InputAction.CallbackContext ctx)
    {
        if (!dashHeld) return;
        dashHeld = false;
        if (dashLockRoutine != null)
        {
            StopCoroutine(dashLockRoutine);
            dashLockRoutine = null;
        }
        bool heavy = false;
        if (dashPressTime > nextHeavyDashTime)
        {
            heavy = (Time.time - dashPressTime) >= heavyDashHoldTime;
        }
        
        EnqueueAction(new BufferedAction
        {
            type = heavy ? ActionType.DashHeavy : ActionType.DashLight,
            time = Time.time
        });
        chargeUpBar.SetChargeBar(false);
        showIndicator = false;
        indicator.Deactivate();
    }


    void OnDefense(InputAction.CallbackContext ctx)
    {
        
        if (blockedActions || Time.time < nextDefTime) return;
        EnqueueAction(new BufferedAction
        {
            type = ActionType.Defense,
            time = Time.time
        });
    }

    IEnumerator RunAction(float duration, IEnumerator gameplayRoutine, ActionType animToPlay, string stateTag)
    {
        anim.SetInAction(true);
        Animator animator = anim.GetAnimator();

        // Fire animation trigger
        switch (animToPlay)
        {
            case ActionType.AttackLight: anim.TriggerAttack(); break;
            case ActionType.AttackHeavy: anim.TriggerSpecial(); break;
            case ActionType.DashLight: anim.TriggerDash(); break;
            case ActionType.DashHeavy: anim.TriggerDash(); break;
            case ActionType.Defense: anim.TriggerDefense(); break;
        }

        // Wait until the animator
        while (animator.GetCurrentAnimatorStateInfo(0).IsTag(stateTag))
            yield return null;

        // read clip length
        float clipLen = anim.GetCurrentClipLengthSeconds(0);

        float prevSpeed = animator.speed;

        if (clipLen > 0.001f && duration > 0.001f)
            animator.speed = clipLen / duration;
        else
            animator.speed = 1f;

        //  parallel
        Coroutine gameplay = null;
        if (gameplayRoutine != null)
            gameplay = StartCoroutine(gameplayRoutine);

        // Lock for your gameplay duration
        yield return new WaitForSeconds(duration);
        anim.SetInAction(false);
        // Ensure gameplay finished too
        if (gameplay != null) yield return gameplay;

        animator.speed = prevSpeed;
    }





    private void Update()
    {
        // World-space mouse
        Vector2 mouseScreen = point.action.ReadValue<Vector2>();
        float zDist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 mouseWorld3 = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, zDist));
        mouseWorld3.z = transform.position.z; // lock to 2D plane
        mousePos = mouseWorld3;

        // Move player
        Vector2 input = move.action.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f) input.Normalize();
        Vector2 targetVel = input * currentSpeed;
        float rate = (input.sqrMagnitude > 0.0001f) ? accel : decel;
        vel = Vector2.MoveTowards(vel, targetVel, rate * Time.deltaTime);

        //if (!blockedMovement) transform.position += (Vector3)(vel * Time.deltaTime);
        if (showIndicator && (Time.time - dashPressTime) >= heavyDashHoldTime)
        {
            indicator.Activate(getMouseDir(), loadout.GetHeavyDashDistance());
        }
        if ((Time.time - attackPressTime) >= heavyAttackHoldTime && attackHeld && !heavyAttackQueuedThisPress)
        {
            EnqueueAction(new BufferedAction
            {
                type = ActionType.AttackHeavy,
                time = Time.time
            });
        }
        // Facing direction (mouse -> player)
        if (blockedActions && !doAnimationAnyway) return;
        updatePlayerAni();
    }

    void FixedUpdate()
    {
        if (!blockedMovement)
            rb.MovePosition(rb.position + vel * Time.fixedDeltaTime);
    }

    private void updatePlayerAni()
    {
        Vector2 lookDir = mousePos - (Vector2)transform.position;
        if (lookDir.sqrMagnitude > 0.0001f) lookDir.Normalize();
        else lookDir = Vector2.down;

        Vector2 animVel = blockedMovement ? Vector2.zero : vel; //make shit zero so standing still works
        // update animation
        anim.UpdateLocomotion(lookDir, animVel);
    }

    public Vector2 getMouseDir()
    {
        Transform player = transform;
        Vector2 playerPos = player.position;

        Vector2 toMouse = mousePos - playerPos;
        Vector2 dir = toMouse.sqrMagnitude > 0.000001f ? toMouse.normalized : Vector2.right;
        return dir;
    }

    public void SetSpeed(float speedProcent)
    {
        currentSpeed = speedProcent * player.GetMoveSpeed();
    }

    public LoadoutBase GetLoadout()
    {
        return loadout;
    }

    public int GetLoadoutNumber()
    {
        return loadoutNumber;
    }

    public float GetDefCD()
    {
        if (nextDefTime - Time.time <= 0.0f)
        {
            return 0.0f;
        }
        else return nextDefTime - Time.time;
    }

    public float GetSpecialCD()
    {
        if (nextHeavyDashTime - Time.time <= 0.0f)
        {
            return 0.0f;
        }
        else return nextHeavyDashTime - Time.time;
    }

    public void SetMovementBlocked(bool blocked)
    {
        blockedMovement = blocked;
        if (blocked) vel = Vector2.zero;
    }
}
