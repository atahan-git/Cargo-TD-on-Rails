using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalHealthBar : MonoBehaviour {

    public Color fullColor = Color.green;
    public Color halfColor = Color.yellow;
    public Color emptyColor = Color.red;

    public GameObject[] bars;
    public float fullLength = 0.4980437f;

    private ModuleHealth myHp;
    private void Start() {
        myHp = GetComponentInParent<ModuleHealth>();
        if (myHp == null)
            enabled = false;
    }

    public float healthPercent;
    private void Update() {
        if (PlayStateMaster.s.isCombatInProgress()) {
            healthPercent = myHp.GetHealthPercent();
        } else {
            healthPercent = Mathf.Lerp(healthPercent, myHp.GetHealthPercent(), 10 * Time.deltaTime);
        }
        
        UpdateHealth(healthPercent);
    }

    public void UpdateHealth(float percentage) {
        for (int i = 0; i < bars.Length; i++) {
            var scale = bars[i].transform.localScale;
            var pos = bars[i].transform.localPosition;
            scale.z = fullLength * percentage;
            pos.z = (1 - percentage) * fullLength / 2f;
            bars[i].transform.localScale = scale;
            bars[i].transform.localPosition = pos;
            var color = GetHealthColor(percentage);

            bars[i].GetComponent<Renderer>().material.color = color;
        }
    }

    private Color GetHealthColor(float percentage) {
        Color color;
        if (percentage > 0.5f) {
            color = Color.Lerp(halfColor, fullColor, (percentage - 0.5f) * 2);
        } else {
            color = Color.Lerp(emptyColor, halfColor, (percentage) * 2);
        }

        return color;
    }
}
