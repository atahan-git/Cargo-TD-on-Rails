using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomMeepleSkin : MonoBehaviour {

    public Color[] possibleColors;

    private static readonly int Property = Shader.PropertyToID("_MainColor ");

    // Start is called before the first frame update
    void Start() {
        Color randomColor = possibleColors[0];
        for (int i = 1; i < possibleColors.Length; i++) {
            randomColor = Color.Lerp(randomColor, possibleColors[i], Random.value);
        }
        
        GetComponent<MeshRenderer>().material.color = randomColor;
    }
}
