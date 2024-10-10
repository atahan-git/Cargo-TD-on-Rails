using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPSDisplay : MonoBehaviour {
    private TMP_Text fpsText;

    private void Start() {
        fpsText = GetComponent<TMP_Text>();

    }

    public List<float> last60dT = new List<float>();

    private float updateDelay = 0.5f;

    void Update() {
        if (Time.deltaTime > 0) {
            last60dT.Add(Time.deltaTime);
        }

        if (last60dT.Count > 120) {
            last60dT.RemoveAt(0);
        }

        var average = 0f;
        var max = 0f;
        var min = 9999f;

        for (int i = 0; i < last60dT.Count; i++) {
            var curVal = Mathf.Clamp(1f/last60dT[i], 0, 9999);
            average += curVal;
            max = Mathf.Max(max, curVal);
            min = Mathf.Min(min, curVal);
        }

        average /= last60dT.Count;

        if (updateDelay <= 0) {
            fpsText.text = $"max: {max:F1} - min: {min:F1} - avg: {average:F1}";
            updateDelay = 1f;
        } else {
            updateDelay -= Time.deltaTime;
        }
    }
}
