using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class PlaybackController : MonoBehaviour
{

    public enum PlaybackMode {
        None, Record, Play
    }

    public PlaybackMode myMode;

    public int seed = 42;
    public string fileName = "inputRecording";
    
    private InputRecorder _inputRecorder;

    public PlayStateMaster playStateMaster;

    private void Awake() {
        playStateMaster.OnDrawWorld.AddListener(SetSeed);
        playStateMaster.OnShopEntered.AddListener(SetSeed);
        playStateMaster.OnCombatEntered.AddListener(SetSeed);
    }

    void Start() {
        Random.InitState(seed);
        
        _inputRecorder = GetComponent<InputRecorder>();
        switch (myMode) {
            case PlaybackMode.Record:
                _inputRecorder.StartCapture();
                break;
            case PlaybackMode.Play:
                _inputRecorder.LoadCaptureFromFile(fileName);
                _inputRecorder.StartReplay();
                break;
        }
        
    }

    void SetSeed() {
        Random.InitState(seed);
    }

    private void OnApplicationQuit() {
        if(myMode == PlaybackMode.Record)
            _inputRecorder.SaveCaptureToFile(fileName);
        print("yeet");
    }

}
