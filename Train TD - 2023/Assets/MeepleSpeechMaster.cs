using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeepleSpeechMaster : MonoBehaviour {

    public static MeepleSpeechMaster s;

    private void Awake() {
        s = this;
    }

    public GameObject bubblePrefab;

    public void Speak(Meeple source, string text) {
        var speechBubble = Instantiate(bubblePrefab, transform).GetComponent<MeepleSpeechBubble>();
        source.myPrevBubble = speechBubble;
        
        speechBubble.ShowText(source.transform, text, source.basePitch);
    }

    public bool RemoveBubble(Meeple source) {
        if (source.myPrevBubble != null) {
            if (source.myPrevBubble.isDone) {
                Destroy(source.myPrevBubble.gameObject);
                source.myPrevBubble = null;
                return true;
            } else {
                source.myPrevBubble.InstantComplete();
                return false;
            }
        } else {
            return true;
        }
    }
}
