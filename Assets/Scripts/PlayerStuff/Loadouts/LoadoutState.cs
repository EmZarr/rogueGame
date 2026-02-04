using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using static PlayerAnimDriver;
public class LoadoutState : MonoBehaviour
{

    [SerializeField] private Player player;

    [SerializeField] private PlayerAnimDriver anim;

    private enum ActionType { AttackLight, AttackHeavy, DashLight, DashHeavy, Defense }

    private struct BufferedAction
    {
        public ActionType type;
        public Vector2 mousePos;
        public Vector2 vel;     // for dash light
        public float time;      // when it was queued
    }

    [Header("Input Buffer")]
    [SerializeField] private float bufferWindow = 0.20f; // 200ms is typical
    private readonly Queue<BufferedAction> buffer = new Queue<BufferedAction>(4);
    private Coroutine actionRunner;
    private Coroutine dashLockRoutine;

    [Header("Movement")]
    public float maxSpeed = 8f;
    public float accel = 100f;
    public float decel = 100f;

    Vector2 vel;
    bool blockedMovement = false;
    bool blockedActions = false;



    private float dashPressTime;
    private bool dashHeld;
    [SerializeField] private float heavyDashHoldTime = 0.4f;

    private LoadoutBase loadout;
    private Vector2 mousePos;

    private float nextHeavyDashTime;
    private float nextDefTime;
    private float nextDashTime;


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
        cam = Camera.main;
    }
    void Start()
    {
        loadout = new LoadoutBase(player);
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

            // Execute one
            blockedActions = true;

            switch (a.type)
            {
                case ActionType.AttackLight:
                    yield return RunAction(loadout.GetLightAttackDuration(), loadout.LightAttack(mousePos), ActionType.AttackLight, "Attack");
                    break;

                case ActionType.AttackHeavy:
                    yield return RunAction(loadout.GetHeavyAttackDuration(), loadout.HeavyAttack(mousePos), ActionType.AttackHeavy, "Special");
                    break;

                case ActionType.DashLight:
                    yield return DoDash(heavy: false, a.vel, a.mousePos);
                    break;

                case ActionType.DashHeavy:
                    yield return DoDash(heavy: true, a.vel, a.mousePos);
                    break;

                case ActionType.Defense:
                    yield return RunAction(loadout.GetDefenseDuration(), loadout.Defense(mousePos), ActionType.Defense, "Defense");
                    break;
            }

            blockedActions = false;
        }

        actionRunner = null;
    }

    void OnEnable()
    {
        attack.action.performed += OnAttack;
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
        attack.action.performed -= OnAttack;
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
    }

    void OnLoadout2(InputAction.CallbackContext ctx)
    {
        anim.EquipWeapon(WeaponId.Shield);
        loadout = new SwordAndShield(player);
    }

    void OnLoadout3(InputAction.CallbackContext ctx)
    {
        anim.EquipWeapon(WeaponId.Dual);
        loadout = new DualSwords(player);
    }

    void OnAttack(InputAction.CallbackContext ctx)
    {
        // Decide heavy/light immediately, queue it with the mousePos snapshot
        bool heavy = ctx.interaction is HoldInteraction;

        EnqueueAction(new BufferedAction
        {
            type = heavy ? ActionType.AttackHeavy : ActionType.AttackLight,
            mousePos = mousePos,
            vel = vel,
            time = Time.time
        });
    }

    void OnDashStarted(InputAction.CallbackContext ctx)
    {
        dashHeld = true;
        dashPressTime = Time.time;
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
        bool heavy = (Time.time - dashPressTime) >= heavyDashHoldTime;

        EnqueueAction(new BufferedAction
        {
            type = heavy ? ActionType.DashHeavy : ActionType.DashLight,
            mousePos = mousePos,
            vel = vel,       // snapshot velocity for light dash
            time = Time.time
        });
    }


    IEnumerator DoDash(bool heavy, Vector2 velAtTime, Vector2 mousePosAtTime)
    {
        try
        {
            // Cooldown checks
            if (heavy)
            {
                if (Time.time < nextHeavyDashTime)
                    yield break;

                nextHeavyDashTime = Time.time + loadout.getHeavyDashCD();
                yield return RunAction(loadout.GetHeavyDashDuration(), loadout.HeavyDash(transform, mousePos) , ActionType.DashHeavy, "Dash");
            }
            else
            {
                if (Time.time < nextDashTime)
                    yield break;

                nextDashTime = Time.time + loadout.getLightDashCD();
                yield return RunAction(loadout.GetLightDashDuration(), loadout.LightDash(vel, transform, mousePos), ActionType.DashLight, "Dash");
            }
        }
        finally
        {
            blockedMovement = false;

        }
    }



    void OnDefense(InputAction.CallbackContext ctx)
    {
        if (blockedActions || Time.time < nextDefTime) return;
        EnqueueAction(new BufferedAction
        {
            type = ActionType.Defense,
            mousePos = mousePos,
            vel = vel,
            time = Time.time
        });
        nextDefTime = Time.time + loadout.getDefenseCD();
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
        while (!animator.GetCurrentAnimatorStateInfo(0).IsTag(stateTag))
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

        // Ensure gameplay finished too
        if (gameplay != null) yield return gameplay;

        animator.speed = prevSpeed;
        anim.SetInAction(false);
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
        Vector2 targetVel = input * maxSpeed;
        float rate = (input.sqrMagnitude > 0.0001f) ? accel : decel;
        vel = Vector2.MoveTowards(vel, targetVel, rate * Time.deltaTime);

        // Facing direction (mouse -> player)
        Vector2 lookDir = mousePos - (Vector2)transform.position;
        if (lookDir.sqrMagnitude > 0.0001f) lookDir.Normalize();
        else lookDir = Vector2.down;

        Vector2 animVel = blockedMovement ? Vector2.zero : vel; //make shit zero so standing still works
        // update animation
        anim.UpdateLocomotion(lookDir, animVel);

        if (blockedMovement) return;
        transform.position += (Vector3)(vel * Time.deltaTime);



    }



}
