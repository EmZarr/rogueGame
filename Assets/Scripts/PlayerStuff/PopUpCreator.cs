using TMPro;
using UnityEngine;



public class PopUpCreator : MonoBehaviour
{
    [SerializeField] GameObject damagePopupPrefab;
    
    public void CreatePopUp(float damage, Vector3 position, int type)
    {
        var offset = Random.Range(0.3f,2.5f);
        var pos = new Vector3(position.x+offset,position.y+0.5f, position.z);
        GameObject popup = Instantiate(damagePopupPrefab, pos, Quaternion.identity);

        popup.GetComponent<DamagePopup>().Setup(damage, type);
    }

}