using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_ShowRepairDroneChargePercent : MonoBehaviour
{
    [ReadOnly]
    public Image myImg;

    private float baseAlpha;
    void Start() {
        myImg = GetComponent<Image>();
        baseAlpha = myImg.color.a;
    }

    //private float flashPercent;
    public void SetPercent(float chargePercent, float noChargePercent = 0.05f) {
        if(myImg == null)
            return;
        if (chargePercent > noChargePercent) {
            myImg.fillAmount = Mathf.MoveTowards(myImg.fillAmount, chargePercent, 1*Time.deltaTime);

            var color = Color.green;
            if (chargePercent < 0.10f) {
                color = Color.red;
            } else if (chargePercent < 0.25f) {
                color = Color.yellow;
            } else {
                color = Color.green;
            }

            color.a = baseAlpha;

            myImg.color = Color.Lerp(myImg.color, color, 5 * Time.deltaTime);
        } else {
            myImg.fillAmount = Mathf.MoveTowards(myImg.fillAmount, 1, 1*Time.deltaTime);
            /*flashPercent += 0.5f*Time.deltaTime;
            flashPercent %= 1;*/
            /*var red = new Color(0.7f,0,0);
            var grey = new Color(0.2f, 0, 0);
            var color = red;
            if (flashPercent < 0.5f) {
                var colorPercent = flashPercent * 2;
                color = Color.Lerp(grey, red, colorPercent);
            } else {
                
                var colorPercent = (flashPercent-0.5f) * 2;
                color = Color.Lerp(red, grey, colorPercent);
            }*/
            var color = Color.black;
            color.a = baseAlpha;
            myImg.color = Color.Lerp(myImg.color, color, 5 * Time.deltaTime);
        }
    }
}
