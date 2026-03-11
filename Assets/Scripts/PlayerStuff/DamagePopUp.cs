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

    public void Setup(float damage, int type)
    {
        if (type == 1)
        {
            damage = -damage;
            textMesh.SetText(damage.ToString());
        }
        else if (type == 5)
        {
            textMesh.SetText("+" + damage.ToString());
        }
        else
        {
            textMesh.SetText(damage.ToString());
        }
    }

    void Update()
    {
        // move upward
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);

        // fade out
        lifetime -= Time.deltaTime;
        //textColor.a = alpha;
        //textMesh.color = textColor;

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