using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGUI_MapTransitionAnimator : MonoBehaviour {
    public static MiniGUI_MapTransitionAnimator s;

    private void Awake() {
        s = this;
    }

    public AnimationCurve goInCurve = AnimationCurve.Linear(0,0,1,1);
    public AnimationCurve goOutCurve = AnimationCurve.Linear(0,0,1,1);

    public GameObject gfx;
    
    public float outsideOffset = 1822;
    public float duration = 1f;

    public float transitionProgress = 0;
    public void Transition(bool isTransitionIn) {
        transitionProgress = 0;
        StartCoroutine(_Transition(isTransitionIn));
    }

    IEnumerator _Transition(bool isTransitionIn) {
        var rect = gfx.GetComponent<RectTransform>();
        var pos = rect.anchoredPosition;

        float startPos;
        float endPos;
        AnimationCurve curve;

        if (isTransitionIn) {
            startPos = outsideOffset;
            endPos = 0f;
            curve = goInCurve;
        } else {
            startPos = 0f;
            endPos = -outsideOffset;
            curve = goOutCurve;
        }

        pos.x = startPos;
        rect.anchoredPosition = pos;
        float time = 0;

        while (time < duration) {
            transitionProgress = time / duration;
            pos.x = Mathf.Lerp(startPos, endPos, curve.Evaluate(transitionProgress));
            rect.anchoredPosition = pos;
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        transitionProgress = 1f;
        pos.x = endPos;
        rect.anchoredPosition = pos;
        
        yield return null;
    }
}
