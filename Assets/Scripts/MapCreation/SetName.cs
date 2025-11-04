using UnityEngine;

public class SetName : MonoBehaviour
{
[SerializeField] string name;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.name = name;
    }

    // Update is called once per frame
}
