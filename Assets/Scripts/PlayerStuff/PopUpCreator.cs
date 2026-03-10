using TMPro;
using UnityEngine;



public class PopUpCreator : MonoBehaviour
{
    [SerializeField] GameObject damagePopupPrefab;
    
    public void CreatePopUp(float damage, Vector3 position)
    {
        GameObject popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);

        popup.GetComponent<DamagePopup>().Setup(damage);
        Debug.Log(position);
    }

}