using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class PPController : MonoBehaviour
{
    public static PPController s;

    private Volume _volume;
    private ColorAdjustments _colorAdjustments;
    private void Awake() {
        s = this;
        _volume = GetComponent<Volume>();
        _volume.profile.TryGet(out _colorAdjustments);
    }
    
    public void SetPostExposure(float postExposure) {
        _colorAdjustments.postExposure.value = postExposure;
    }
}
