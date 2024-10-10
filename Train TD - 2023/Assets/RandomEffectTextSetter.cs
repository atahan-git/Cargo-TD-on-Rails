using System.Collections;
using System.Collections.Generic;
using CartoonFX;
using UnityEngine;

public class RandomEffectTextSetter : MonoBehaviour {
    public string[] myTexts = new string[] { "yeet" };
    
    void OnEnable()
    {
        GetComponent<CFXR_ParticleText>().UpdateText(myTexts[Random.Range(0,myTexts.Length)]);
    }

}
