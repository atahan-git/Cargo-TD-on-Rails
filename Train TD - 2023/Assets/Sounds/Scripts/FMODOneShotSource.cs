using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class FMODOneShotSource : MonoBehaviour
{
    public EventReference clip;

    public bool playOnStart = true;


    private void Start()
    {
        if (playOnStart)
            Play();
    }
    public void Play()
    {
        AudioManager.PlayOneShot(clip);
    }
}
