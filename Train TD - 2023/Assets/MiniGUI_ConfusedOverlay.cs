using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MiniGUI_ConfusedOverlay : MonoBehaviour {

    public static MiniGUI_ConfusedOverlay s;
    public TMP_Text myText;
    public TMP_Text timeRemaining;

    private void Awake() {
        s = this;
    }

    public bool isConfused = false;
     CanvasGroup canvasGroup;

    private void Start() {
        isConfused = false;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        if (isConfused) {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1, 2 * Time.deltaTime);
        } else {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0, 4 * Time.deltaTime);
        }
    }

    public void SetConfused(bool _isConfused, string confusedText) {
        isConfused = _isConfused;
        myText.text = "Confused!\n" + confusedText;
    }

    public void SetConfusedTime(float time) {
        timeRemaining.text = ExtensionMethods.FormatTimeSeconds(time);
    }
}
