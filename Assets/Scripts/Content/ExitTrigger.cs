using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    [SerializeField] MapGenerator mapGenerator;

    void Awake()
    {
        if (mapGenerator == null)
            mapGenerator = FindFirstObjectByType<MapGenerator>();

        // ensure collider is a trigger so OnTriggerEnter2D fires
        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        // Trigger new run
        mapGenerator?.RemakeMap();
    }
}