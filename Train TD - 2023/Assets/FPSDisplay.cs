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

    struct FpsAndTime {
        public float fps;
        public float time;
    }

    public float deltaTime;
     List<FpsAndTime> values = new List<FpsAndTime>();

     private float updateDelay = 0.5f;

     void Update() {
         deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
         float fps = 1.0f / deltaTime;
         values.Add(new FpsAndTime() { fps = fps, time = Time.timeSinceLevelLoad });
         while (values.Count > 0 && values[0].time < Time.timeSinceLevelLoad - 1f) {
             values.RemoveAt(0);
         }


         var average = 0f;
         var max = 0f;
         var min = 10000f;

         for (int i = 0; i < values.Count; i++) {
             average += values[i].fps;
             max = Mathf.Max(max, values[i].fps);
             min = Mathf.Min(min, values[i].fps);
         }

         average /= values.Count;

         if (updateDelay <= 0) {
             fpsText.text = $"max: {max:F1} - min: {min:F1} - avg: {average:F1}";
             updateDelay = 1f;
         } else {
             updateDelay -= Time.deltaTime;
         }
     }
}
