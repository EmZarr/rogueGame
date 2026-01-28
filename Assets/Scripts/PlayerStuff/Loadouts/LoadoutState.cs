using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using static Unity.Collections.AllocatorManager;
// Has data that is need across all loadouts
public class LoadoutState : MonoBehaviour
{

    [Header("Movement")]
    public float maxSpeed = 8f;
    public float accel = 100f;
    public float decel = 100f;

    Vector2 vel;
    bool blockedMovement = false;
    bool blockedActions = false;

    private float heavyDashResource = 100f;
    private float defResource = 100f;

    private float chargeFinishTime;

    private LoadoutBase loadout;
    private Vector2 mousePos;


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
        dash.action.performed += OnDash;

        move.action.Enable();
        point.action.Enable();
        def.action.Enable();
        attack.action.Enable();
        dash.action.Enable();

    }

    void OnDisable()
    {
        attack.action.performed -= OnAttack;
        dash.action.performed -= OnDash;

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
        if (heavy) yield return loadout.heavyAttack(mousePos);
        else yield return loadout.lightAttack(mousePos);

        blockedActions = false;
    }

    void OnDash(InputAction.CallbackContext ctx)
    {
        if (blockedActions) return;
        blockedActions = true;

        if (ctx.interaction is HoldInteraction)
            StartCoroutine(DoDash(true));
        else if (ctx.interaction is TapInteraction)
            StartCoroutine(DoDash(false));
    }

    IEnumerator DoDash(bool heavy)
    {
        if (heavy) yield return loadout.heavyDash(vel);
        else yield return loadout.lightDash(vel);

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
        if (blockedMovement) return;
        Vector2 input = move.action.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f) input.Normalize();
        Vector2 targetVel = input * maxSpeed;
        float rate = (input.sqrMagnitude > 0.0001f) ? accel : decel;
        vel = Vector2.MoveTowards(vel, targetVel, rate * Time.deltaTime);
        transform.position += (Vector3)(vel * Time.deltaTime);


    }



}
