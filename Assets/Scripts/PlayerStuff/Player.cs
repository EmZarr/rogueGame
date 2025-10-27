using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] float _health = 100;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void TakeDamage(float damage)
    {
        _health -= damage;
        if (_health < 0)
        {
            Destroy (gameObject);
        }
    }
}
