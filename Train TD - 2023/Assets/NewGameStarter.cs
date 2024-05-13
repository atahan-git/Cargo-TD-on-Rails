using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NewGameStarter : MonoBehaviour {
    public static NewGameStarter s;

    private void Awake() {
        s = this;
    }

    public CharacterDataScriptable[] defaultChars;
    public void CheckStartNewGame() {
        if (!DataSaver.s.GetCurrentSave().isInARun) {
            DataSaver.s.GetCurrentSave().currentRun = new DataSaver.RunState(VersionDisplay.s.GetVersionNumber());
            DataSaver.s.GetCurrentSave().currentRun.SetCharacter(defaultChars[Random.Range(0,defaultChars.Length)].myCharacter);
            DataSaver.s.GetCurrentSave().isInARun = true;
            DataSaver.s.SaveActiveGame();
        }
        
    }
}
