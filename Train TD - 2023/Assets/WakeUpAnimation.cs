using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WakeUpAnimation : MonoBehaviour {

    public static WakeUpAnimation s;

    public Animator wakeUpAnimator;
    private static readonly int engage = Animator.StringToHash("engage");

    private void Awake() {
        s = this;
    }

    public void Engage() {
        wakeUpAnimator.SetTrigger(engage);
    }

    public void PlaySound() {
        GetComponentInChildren<RandomPitchAtStart>(true).Play();
    }
}
