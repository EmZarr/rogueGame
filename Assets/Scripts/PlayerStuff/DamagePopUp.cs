using TMPro;
using UnityEngine;



public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;

    float moveSpeed = 3f;
    float fadeSpeed = 3f;
    float lifetime = 1.5f;

    Color textColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(float damage, int type)
    {
        switch (type)
        {
            //player damage
            case 1:
                textMesh.color = Color.red;
                damage = -damage;
                break;
            //enemy damage
            case 2:
                textMesh.color = Color.red;
                break;
            //player heal
            case 3:
                textMesh.color = Color.green;
                break;
            //player buff
            case 4:
                textMesh.color = Color.white;
                break;

        }
        textMesh.SetText(damage.ToString());
        textColor = textMesh.color;
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