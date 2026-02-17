using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static UnityEditor.Experimental.GraphView.GraphView;

public class LoadoutUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text loadoutText;
    [SerializeField] private GameObject icon1;
    [SerializeField] private GameObject icon2;
    [SerializeField] private GameObject icon3;

    private LoadoutState state;

    private void OnEnable() => MapInstantiator.OnPlayerSpawned += HandlePlayerSpawned;
    private void OnDisable() => MapInstantiator.OnPlayerSpawned -= HandlePlayerSpawned;

    void HandlePlayerSpawned(Player p)
    {
        state = p.GetComponent<LoadoutState>();
    }

    // Update is called once per frame
    void Update()
    {
        var loadoutNumber = state.GetLoadoutNumber();
        loadoutText.text = loadoutNumber.ToString();
        switch (loadoutNumber)
        {
            case 1: 
                icon1.SetActive(true);
                icon2.SetActive(false);
                icon3.SetActive(false);
                break;
            case 2:
                icon1.SetActive(false);
                icon2.SetActive(true);
                icon3.SetActive(false);
                break;
            case 3:
                icon1.SetActive(false);
                icon2.SetActive(false);
                icon3.SetActive(true);
                break;
        }
        
    }
}
