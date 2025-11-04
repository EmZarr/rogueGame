using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.UI;

public class RunTimeBake : MonoBehaviour
{

    [SerializeField] NavMeshSurface surface;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        surface.BuildNavMesh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
