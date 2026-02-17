using UnityEngine;
using System.Collections;

public class DamageFlash : MonoBehaviour
{ 
    [SerializeField] private SpriteRenderer spriteRenderer;
    private Material mat;
    private static readonly int FlashID = Shader.PropertyToID("_FlashAmount");

    void Awake()
    {
        mat = spriteRenderer.material;
    }

    public void Flash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        mat.SetFloat(FlashID, 1f);

        yield return new WaitForSeconds(0.18f);

        mat.SetFloat(FlashID, 0f);
    }
}
