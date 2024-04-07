using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenFadeToWhiteController : MonoBehaviour
{
    public static ScreenFadeToWhiteController s;

    private Volume _volume;
    private ColorAdjustments _colorAdjustments;

    private float defaultPostExposure;
    private void Awake() {
        s = this;
        _volume = GetComponent<Volume>();
        _volume.profile.TryGet(out _colorAdjustments);
        defaultPostExposure = _colorAdjustments.postExposure.value;
    }


    public void SetFadeToWhite(float percent) {
        _colorAdjustments.postExposure.value = percent.Remap(0, 1, defaultPostExposure, 25);
    }
    
    public void ResetFadeToWhite() {
        SetFadeToWhite(0);
    }
}
