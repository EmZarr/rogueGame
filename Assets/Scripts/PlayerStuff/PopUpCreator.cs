using TMPro;
using UnityEngine;



public class PopUpCreator : MonoBehaviour
{
    [SerializeField] GameObject damagePopupPrefab;
    [SerializeField] GameObject healthPopupPrefab;
    [SerializeField] GameObject powerUpPopupPrefab;
    [SerializeField] GameObject scorePopupPrefab;
    
    public void CreatePopUp(float damage, Vector3 position, int type)
    {   
        float offset = 0f;
        Vector3 pos;
        GameObject popup;
        switch (type)
        {
            case 1:
                offset = Random.Range(-1.0f,0.0f);
                pos = new Vector3(position.x+offset,position.y+0.5f, position.z);
                popup = Instantiate(damagePopupPrefab, pos, Quaternion.identity);
                popup.GetComponent<DamagePopup>().Setup(damage, type);
                break;
            case 2:
                offset = Random.Range(-1.0f,1.0f);
                pos = new Vector3(position.x+offset,position.y+0.5f, position.z);
                popup = Instantiate(damagePopupPrefab, pos, Quaternion.identity);
                popup.GetComponent<DamagePopup>().Setup(damage, type);
                break;
            case 3:
                offset = Random.Range(0.0f, 0.5f);
                pos = new Vector3(position.x+offset,position.y+0.5f, position.z);
                popup = Instantiate(healthPopupPrefab, pos, Quaternion.identity);
                popup.GetComponent<DamagePopup>().Setup(damage, type);
                break;
            case 4:
                offset = Random.Range(0.5f, 1.5f);
                pos = new Vector3(position.x+offset,position.y+0.5f, position.z);
                popup = Instantiate(powerUpPopupPrefab, pos, Quaternion.identity);
                popup.GetComponent<DamagePopup>().Setup(damage, type);
                break;
            case 5:
                offset = 2.5f;
                pos = new Vector3(position.x+offset,position.y+0.5f, position.z);
                popup = Instantiate(scorePopupPrefab, pos, Quaternion.identity);
                popup.GetComponent<DamagePopup>().Setup(damage, type);
                break;
        }
    }

}