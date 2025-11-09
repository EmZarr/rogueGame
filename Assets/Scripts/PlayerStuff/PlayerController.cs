using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Input (assign from your Controls asset)")]
    public InputActionReference move;   // Gameplay/Move (Vector2)
    public InputActionReference point;  // Gameplay/Look (Vector2) -> <Pointer>/position
    public InputActionReference block;  // Gameplay/Block (Button)
    public InputActionReference dash;   // Gameplay/Dash (Button)
    public InputActionReference attack;   // Gameplay/Dash (Button)

    [Header("Movement")]
    public float maxSpeed = 8f;
    public float acceleration = 100f;
    public float deceleration = 100f;

    [Header("Dash")]
    public float dashSpeed = 40f;
    public float dashLength = 0.15f;
    public float dashCooldown = 2f;

    [Header("Shield")]
    public GameObject shield;   // the shield object to move/rotate
    public float shieldSpeed = 20f; // units per second; <= 0 means snap
    public float fallbackRadius = 1.25f; // used if shield starts exactly on player

    [Header("Attack")]
    public float attackSpeed = 0.1f;
    public float attackCooldown = 0.5f;
    public float attackRange = 0.5f;
    public float damage = 10f;
    [SerializeField] PlayerMeleeAttackZone attackZone;

    private bool isDashing;
    private bool isAttacking = false;
    private Vector2 mousePos;
    private float dashEndTime;
    private float nextDashTime;
    private Vector2 dashDir;
    float nextAttackTime;

    Rigidbody2D rb;
    Vector2 vel;
    Camera cam;

    // Shield state
    float shieldRadius;
    Vector2 lastAimDir = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        cam = Camera.main;
    }

    void Start()
    {
        // Determine constant orbit radius from current placement
        if (shield != null)
        {
            shieldRadius = Vector2.Distance(shield.transform.position, transform.position);
            if (shieldRadius <= 0.0001f) shieldRadius = fallbackRadius;
        }
    }

    void OnEnable()
    {
        move.action.Enable();
        point.action.Enable();
        block.action.Enable();
        attack.action.Enable();
        dash.action.Enable();

        attack.action.performed += OnAttackPerformed;
        dash.action.performed += OnDashPerformed;
    }

    void OnDisable()
    {
        attack.action.performed -= OnAttackPerformed;
        dash.action.performed -= OnDashPerformed;

        move.action.Disable();
        point.action.Disable();
        block.action.Disable();
        attack.action.Disable();
        dash.action.Disable();
    }

    void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        StartCoroutine(BasicAttack(ctx));
    }

    void OnDashPerformed(InputAction.CallbackContext ctx)
    {
        StartCoroutine(PerformDash(ctx));
    }


    private IEnumerator BasicAttack(InputAction.CallbackContext ctx)
    {
        if (Time.time < nextAttackTime || shield.activeInHierarchy) yield break; // attack cooldown
        isAttacking = true;
        attackZone.Activate(mousePos);
        yield return new WaitForSeconds(attackSpeed);
        yield return attackZone.DealDamage(damage);
        attackZone.Deactivate();    
        nextAttackTime = Time.time+attackCooldown;
        isAttacking = false;
    }

    private IEnumerator PerformDash(InputAction.CallbackContext ctx)
    {
       if (isDashing || Time.time < nextDashTime) yield break; // dash cooldown
        Vector2 input = move.action.ReadValue<Vector2>();
        if (input.sqrMagnitude > 0.01f)
        {
            StartDash(input.normalized);
            yield return new WaitForSeconds(dashLength);
            isDashing = false;
            nextDashTime = Time.time + dashCooldown;
        }
    }

    void Update()
    {
        // World-space mouse
        Vector2 mouseScreen = point.action.ReadValue<Vector2>();
        float zDist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 mouseWorld3 = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, zDist));
        mouseWorld3.z = transform.position.z; // lock to 2D plane
        mousePos = mouseWorld3;

        // Toggle shield on block
        if (shield != null && !isAttacking)
            shield.SetActive(block.action.IsPressed());

        // --- Aim & orbit the shield ---
        if (shield != null && shield.activeInHierarchy)
        {
            Vector2 playerPos = rb.position;
            Vector2 toMouse = mousePos - playerPos;

            Vector2 aimDir = toMouse.sqrMagnitude > 0.000001f ? toMouse.normalized : lastAimDir;
            if (aimDir == Vector2.zero) aimDir = Vector2.right;
            lastAimDir = aimDir;

            Vector2 targetPos = playerPos + aimDir * shieldRadius;

            if (shieldSpeed > 0f)
            {
                float maxStep = shieldSpeed * Time.deltaTime;
                shield.transform.position = Vector2.MoveTowards((Vector2)shield.transform.position, targetPos, maxStep);
            }
            else
            {
                shield.transform.position = targetPos; // instant snap
            }

            // Face the mouse: +X (right) points at the mouse.
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            shield.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            // If your sprite's "forward" is up instead of right, use angle - 90f
            // shield.transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (isAttacking) return;
        if (isDashing)
        {
            rb.MovePosition(rb.position + dashDir * dashSpeed * dt);
            return;
        }

        Vector2 input = move.action.ReadValue<Vector2>();
        input = Vector2.ClampMagnitude(input, 1f);

        Vector2 target = input * maxSpeed;

        vel.x = Mathf.MoveTowards(vel.x, target.x, (Mathf.Abs(target.x) > 0.01f ? acceleration : deceleration) * dt);
        vel.y = Mathf.MoveTowards(vel.y, target.y, (Mathf.Abs(target.y) > 0.01f ? acceleration : deceleration) * dt);

        rb.MovePosition(rb.position + vel * dt);
    }

    void StartDash(Vector2 direction)
    {
        isDashing = true;
        dashDir = direction;
        dashEndTime = Time.time + dashLength;
    }
}
