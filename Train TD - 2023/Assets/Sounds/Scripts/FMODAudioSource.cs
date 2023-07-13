using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using FMOD;
using Sirenix.OdinInspector;

public class FMODAudioSource : MonoBehaviour
{
    [FoldoutGroup("Properties")]
    public EventReference clip;

    [FoldoutGroup("Play & Pause")]
    public bool pauseOnGamePause = true;

    [FoldoutGroup("Play & Pause")]
    public bool playOnStart = true;


    [FoldoutGroup("Properties")]
    [PropertyRange(0f, 3f)]
    public float volume = 1;

    [FoldoutGroup("Properties")]
    [PropertyRange(0f, 3f)]
    public float pitch = 1;

    private EventInstance soundInstance;

    private void Start()
    {
        if(!clip.Equals(default(EventReference)))
            soundInstance = AudioManager.CreateFmodEventInstance(clip);


        if (playOnStart)
            Play();
    }

    private VECTOR Vector3ToVector(Vector3 vec)
    {
        VECTOR tmp = new VECTOR();
        tmp.x = vec.x;
        tmp.y = vec.y;
        tmp.z = vec.z;
        return tmp;
    }

    private void Update()
    {
        soundInstance.setVolume(volume);
        soundInstance.setPitch(pitch);


        ATTRIBUTES_3D attributes = new ATTRIBUTES_3D();
        attributes.position = Vector3ToVector(transform.position);
        attributes.forward = Vector3ToVector(transform.forward);
        attributes.up = Vector3ToVector(transform.up);
        soundInstance.set3DAttributes(attributes);

    }

    private void OnEnable()
    {
        if(pauseOnGamePause)
            TimeController.PausedEvent.AddListener(OnPause);
    }

    private void OnDisable()
    {
        Stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        if(pauseOnGamePause)
            TimeController.PausedEvent.RemoveListener(OnPause);
    }


    #region Play/Stop/Pause Functions
    public int TimelinePosition()
    {
        soundInstance.getTimelinePosition(out int position);
        return position;
    }

    void OnPause(bool isPaused)
    {
        if (isPaused)
        {
            Pause();
        }
        else
        {
            UnPause();
        }
    }
    public void Pause()
    {
        soundInstance.setPaused(true);
        // Invoke("PauseDelay", 0.5f);
    }

    public void UnPause()
    {
        soundInstance.setPaused(false);
    }
    public void TogglePause()
    {
        soundInstance.getPaused(out bool isPaused);
        if (isPaused)
            UnPause();
        else
            Pause();
    }
    public bool IsPaused()
    {
        soundInstance.getPaused(out bool isPaused);
        return isPaused;
    }

    public void Play()
    {
        soundInstance.start();
    }

    public void Stop(FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
    {
        soundInstance.stop(stopMode);
    }
    #endregion

    public void SetParamByName(string paramName, float value)
    {
        soundInstance.setParameterByName(paramName, value);
    }
    public void SetParamByName(string paramName, string value)
    {
        soundInstance.setParameterByNameWithLabel(paramName, value);
    }

    public float GetParamByName(string paramName)
    {
        soundInstance.getParameterByName(paramName, out float value);
        return value;
    }

    public void LoadClip(EventReference clip, bool startPlaying = false)
    {
        Stop();
        soundInstance.release();
        this.clip = clip;
        soundInstance = AudioManager.CreateFmodEventInstance(clip);
        if (startPlaying)
            Play();
    }
}
