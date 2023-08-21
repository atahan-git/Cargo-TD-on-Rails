using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalShieldBar : MonoBehaviour {


    public Color fullShieldMainColor;
    [ColorUsageAttribute(true, true)] 
    public Color fullShieldEmissionColor;
    
    
    /*public Color noShieldMainColor;
    [ColorUsageAttribute(true, true)] 
    public Color noShieldEmissionColor;*/
    
    
    public GameObject[] bars;

    public GameObject shieldBreakEffect;
    
    private IHealth myHp;
    private void Start() {
        myHp = GetComponentInParent<IHealth>();
        if (myHp == null)
            enabled = false;

        regularYSize = bars[0].transform.localScale.y;
    }

    
    
    public float shieldPercent;
    private void Update() {
        if (!myHp.IsShieldActive()) {
            shieldPercent = 0;
        } else {
            shieldPercent = Mathf.Lerp(shieldPercent, myHp.GetShieldPercent(), 10 * Time.deltaTime);
        }
        UpdateShield(shieldPercent);
    }

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int Emission = Shader.PropertyToID("_Emission");

    private float fullIntensity = 3.5f;
    private float regularYSize = 0.19567f;

    private float defaultSize = 0.19567f;
    
    public void UpdateShield(float percentage) {
        for (int i = 0; i < bars.Length; i++) {
            if (!myHp.IsShieldActive()) {
                if (bars[i].activeSelf) {
                    bars[i].SetActive(false);
                    Instantiate(shieldBreakEffect, bars[i].transform.position, bars[i].transform.rotation);
                }

                continue;
            } else {
                bars[i].SetActive(true);
            }

            var mainColor = fullShieldMainColor;
            var emissionColor = fullShieldEmissionColor;
            float factor = fullIntensity * (percentage.Remap(0, 1, 0, 0.05f));
            emissionColor = new Color(emissionColor.r*factor,emissionColor.g*factor,emissionColor.b*factor);

            bars[i].GetComponent<Renderer>().material.SetColor(BaseColor, mainColor);
            bars[i].GetComponent<Renderer>().material.SetColor(Emission, emissionColor);
            var scale = bars[i].transform.localScale;
            scale.y = Mathf.Lerp(regularYSize/3f, regularYSize, percentage);
            bars[i].transform.localScale = scale;
        }
    }

    public void SetSize(int size) { // 0 means covering 1 carts
        for (int i = 0; i < bars.Length; i++) {
            var scale =  bars[i].transform.localScale;
            scale.z = defaultSize * ((size*2)+1.1f);
        }
    }
}
