using TMPro;
using UnityEngine;



public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;

    float moveSpeed = 3f;
    float fadeSpeed = 3f;
    float lifetime = 1f;

    Color textColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(float damage)
    {
        textMesh.SetText(damage.ToString());
        textColor = textMesh.color;
        Debug.Log(damage.ToString());
    }

    void Update()
    {
        // move upward
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);

        // fade out
        lifetime -= Time.deltaTime;
        float alpha = lifetime;

        textColor.a = alpha;
        textMesh.color = textColor;

        if (lifetime <= 0)
        {
            Destroy(gameObject);
        }
    }

    void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }
}