using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGUI_ConfusedOverlay : MonoBehaviour {

    public static MiniGUI_ConfusedOverlay s;

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
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1, 1 * Time.deltaTime);
        } else {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0, 3 * Time.deltaTime);
        }
    }

    public void SetConfused(bool _isConfused) {
        isConfused = _isConfused;
    }
}
