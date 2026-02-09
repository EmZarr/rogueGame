using UnityEngine;
using UnityEngine.UI;

public class ChargeUp : MonoBehaviour
{
    Image chargeBar;
    bool isCharging;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        chargeBar = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isCharging)
        {
            chargeBar.fillAmount += Time.deltaTime * 2.5f;
            if (chargeBar.fillAmount == 1)
            {
                chargeBar.color = Color.green;
                SetAlpha(0.5f);
            }
        }
        else
        {
            chargeBar.fillAmount = 0;
            chargeBar.color = Color.red;
            SetAlpha(0.5f);
        }
    }

    public void SetChargeBar(bool charging)
    {
        isCharging = charging;
    }

    private void SetAlpha(float a)
    {
        var c = chargeBar.color;
        c.a = a;
        chargeBar.color = c;
    }
}

