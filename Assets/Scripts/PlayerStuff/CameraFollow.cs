using System.Collections;
using UnityEngine;

public class CameraFollowCinematic : MonoBehaviour
{
    [Header("Player Tag")]
    [SerializeField] private string playerTag = "Player";

    [Header("Normal Follow")]
    [SerializeField] private float followSmoothTime = 0.2f;
    [SerializeField] private float cameraZOffset = -10f;

    [Header("Death Cinematic")]
    [SerializeField] private float deathPauseDuration = 0.8f;
    [SerializeField] private float travelDuration = 1.2f;
    [SerializeField] private float deathZoomTarget = 4f;
    [SerializeField] private float easingPower = 3f;

    private Transform currentTarget;
    private Vector3 velocity;
    private Vector3 lastPosition;
    private Player subscribedPlayer;
    private Coroutine cinematicCo;
    private new Camera camera;
    private float defaultOrthoSize;
    private float defaultFov;

    void Awake()
    {
        camera = GetComponent<Camera>() ?? Camera.main;
        if (camera != null)
        {
            if (camera.orthographic) defaultOrthoSize = camera.orthographicSize;
            else defaultFov = camera.fieldOfView;
        }
    }

    void Start()
    {
        TryFindPlayer();
        SubscribeToPlayerDeath();
        lastPosition = transform.position;
    }

    void OnDisable()
    {
        UnsubscribeFromPlayerDeath();
        if (cinematicCo != null) StopCoroutine(cinematicCo);
    }

    void LateUpdate()
    {
        TryReacquirePlayerAndInterruptCinematic();

        if (!IsValid(currentTarget) && cinematicCo == null)
        {
            currentTarget = null;
            TryFindPlayer();
            SubscribeToPlayerDeath();

            if (camera != null && subscribedPlayer != null)
            {
                if (camera.orthographic) camera.orthographicSize = defaultOrthoSize;
                else camera.fieldOfView = defaultFov;
            }
        }

        if (IsValid(currentTarget))
        {
            Vector3 goal = currentTarget.position;
            goal.z = cameraZOffset;
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, followSmoothTime);
            lastPosition = transform.position;
        }
        else transform.position = lastPosition;
    }

    private void OnPlayerDied(GameObject killerGO)
    {
        if (cinematicCo != null) StopCoroutine(cinematicCo);
        cinematicCo = StartCoroutine(DeathCinematic(killerGO ? killerGO.transform : null));
    }

    private IEnumerator DeathCinematic(Transform killer)
    {
        currentTarget = null;
        Vector3 freezePos = transform.position;
        float startZoom = camera.orthographic ? camera.orthographicSize : camera.fieldOfView;

        float t = 0f;
        while (t < deathPauseDuration)
        {
            t += Time.unscaledDeltaTime;
            transform.position = freezePos;
            yield return null;
        }

        if (!IsValid(killer))
        {
            cinematicCo = null;
            yield break;
        }

        t = 0f;
        while (t < travelDuration)
        {
            t += Time.deltaTime;
            float a = EaseOut(Mathf.Clamp01(t / travelDuration), easingPower);
            Vector3 targetPos = killer.position; targetPos.z = cameraZOffset;
            Vector3 p = Vector3.Lerp(freezePos, targetPos, a);
            transform.position = p;
            lastPosition = p;

            float z = Mathf.Lerp(startZoom, deathZoomTarget, a);
            if (camera.orthographic) camera.orthographicSize = z;
            else camera.fieldOfView = z;

            if (!IsValid(killer)) break;
            yield return null;
        }

        if (IsValid(killer)) currentTarget = killer;
        else currentTarget = null;

        cinematicCo = null;
    }

    private void TryFindPlayer()
    {
        var playerGO = GameObject.FindWithTag(playerTag);
        if (playerGO != null) currentTarget = playerGO.transform;
    }

    private void SubscribeToPlayerDeath()
    {
        UnsubscribeFromPlayerDeath();
        var playerGO = GameObject.FindWithTag(playerTag);
        if (playerGO != null && playerGO.TryGetComponent<Player>(out var p))
        {
            subscribedPlayer = p;
            subscribedPlayer.OnDied += OnPlayerDied;
            if (!IsValid(currentTarget)) currentTarget = p.transform;
        }
    }

    private void UnsubscribeFromPlayerDeath()
    {
        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -= OnPlayerDied;
            subscribedPlayer = null;
        }
    }

    private static bool IsValid(Transform t) =>
        t != null && t.gameObject != null && t.gameObject.activeInHierarchy;

    private static float EaseOut(float x, float power) =>
        1f - Mathf.Pow(1f - Mathf.Clamp01(x), power);

    private void TryReacquirePlayerAndInterruptCinematic()
    {
        var playerGO = GameObject.FindWithTag(playerTag);
        if (playerGO == null) return;

        // If we're already following this player, nothing to do
        if (currentTarget != null && currentTarget.gameObject == playerGO) return;

        // Interrupt cinematic immediately
        if (cinematicCo != null)
        {
            StopCoroutine(cinematicCo);
            cinematicCo = null;
        }

        // Reset zoom
        if (camera != null)
        {
            if (camera.orthographic) camera.orthographicSize = defaultOrthoSize;
            else camera.fieldOfView = defaultFov;
        }

        // Follow new player + resubscribe to death
        currentTarget = playerGO.transform;
        SubscribeToPlayerDeath();
    }

}
