using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversationEnumToSprite : MonoBehaviour {
    public static ConversationEnumToSprite s;

    private void Awake() {
        s = this;
    }

    public enum Characters {
        None=-1, NuclearDude=0
    }

    public Sprite[] bigImages;
    public Sprite[] portraits;


    public Sprite GetPortrait(Characters character) {
        if (character == Characters.None) {
            return null;
        }

        var index = Mathf.Clamp((int)character, 0, portraits.Length);
        return portraits[index];
    }

    public Sprite GetBigImage(Characters character) {
        if (character == Characters.None) {
            return null;
        }
        
        var index = Mathf.Clamp((int)character, 0, bigImages.Length);
        return bigImages[index];
    }
}
