using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
public class LoadoutState : MonoBehaviour
{

    [Header("Movement")]
    public float maxSpeed = 8f;
    public float accel = 100f;
    public float decel = 100f;

    Vector2 vel;
    bool blockedMovement = false;
    bool blockedActions = false;

    private float dashPressTime;
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


    Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }
    void Start()
    {
        loadout = new LoadoutBase();
    }

    void OnEnable()
    {
        attack.action.performed += OnAttack;
        dash.action.started += OnDashStarted;
        dash.action.canceled += OnDashCanceled;
        def.action.performed += OnDefense;

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

        move.action.Disable();
        point.action.Disable();
        def.action.Disable();
        attack.action.Disable();
        dash.action.Disable();
    }

    void OnAttack(InputAction.CallbackContext ctx)
    {
        if (blockedActions) return;
        blockedActions = true;

        if (ctx.interaction is HoldInteraction)
            StartCoroutine(DoAttack(true));
        else if (ctx.interaction is TapInteraction)
            StartCoroutine(DoAttack(false));
    }

    IEnumerator DoAttack(bool heavy)
    {
        if (heavy) yield return loadout.HeavyAttack(mousePos);
        else yield return loadout.LightAttack(mousePos);

        blockedActions = false;
    }

    void OnDashStarted(InputAction.CallbackContext ctx)
    {
        if (blockedActions) return;

        dashPressTime = Time.time;

        blockedActions = true;
        blockedMovement = true;
        vel = Vector2.zero;
    }

    void OnDashCanceled(InputAction.CallbackContext ctx)
    {
        bool heavy = (Time.time - dashPressTime) >= heavyDashHoldTime;
        StartCoroutine(DoDash(heavy));
    }


    IEnumerator DoDash(bool heavy)
    {
        if (heavy)
        {
            if (Time.time < nextHeavyDashTime)
            {
                blockedMovement = false;
                blockedActions = false;
                yield break;
            }

            nextHeavyDashTime = Time.time + loadout.getHeavyDashCD();
            yield return loadout.HeavyDash(vel, transform, mousePos);

            blockedMovement = false;
            blockedActions = false;
        }
        else
        {
            if (Time.time < nextDashTime)
            {
                blockedMovement = false; 
                blockedActions = false;
                yield break;
            }

            nextDashTime = Time.time + loadout.getLightDashCD();
            yield return loadout.LightDash(vel, transform, mousePos);
            blockedMovement = false;
            blockedActions = false;
        }
    }



    void OnDefense(InputAction.CallbackContext ctx)
    {
        if (blockedActions || Time.time < nextDefTime) return;
        blockedActions = true;

        DoDefense();
    }

    IEnumerator DoDefense()
    {
        nextDefTime = Time.time + loadout.getDefenseCD();
        yield return loadout.Defense(mousePos);
        blockedActions = false;
    }

    private void Update()
    {

        // Update Mouse Pos
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = transform.position.z; // lock to gameplay plane
        mousePos = mouseWorld;

        // Move player
        Vector2 input = move.action.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f) input.Normalize();
        Vector2 targetVel = input * maxSpeed;
        float rate = (input.sqrMagnitude > 0.0001f) ? accel : decel;
        vel = Vector2.MoveTowards(vel, targetVel, rate * Time.deltaTime);
        if (blockedMovement) return;
        transform.position += (Vector3)(vel * Time.deltaTime);
    }



}
