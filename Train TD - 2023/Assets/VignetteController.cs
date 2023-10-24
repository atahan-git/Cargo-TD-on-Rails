using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteController : MonoBehaviour {
    public static VignetteController s;

    private Volume _volume;
    private Vignette _vignette;
    private void Awake() {
        s = this;
        _volume = GetComponent<Volume>();
        _volume.profile.TryGet(out _vignette);
    }


    public void SetVignette(float percent) {
        _vignette.intensity.value = percent.Remap(0, 1, 0, 0.5f);
    }
    
    public void ResetVignette() {
        SetVignette(0);
    }
}
