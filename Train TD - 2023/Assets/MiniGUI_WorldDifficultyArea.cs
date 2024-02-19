using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_WorldDifficultyArea : MonoBehaviour {
    public Slider trainPos;

    public GameObject act1Difficulty;
    public GameObject act2Difficulty;
    public GameObject act3Difficulty;
    
    public MiniGUI_ChangeImageTint[] act1Difficulties = new MiniGUI_ChangeImageTint[4];
    public MiniGUI_ChangeImageTint[] act2Difficulties = new MiniGUI_ChangeImageTint[4];
    public MiniGUI_ChangeImageTint[] act3Difficulties = new MiniGUI_ChangeImageTint[4];

    private void Start() {
        WorldDifficultyController.s.OnDifficultyChanged.AddListener(OnDifficultyChanged);
        if(WorldDifficultyController.s.playerAct > 0)
            OnDifficultyChanged();
    }

    void Update() {
        trainPos.value = WorldDifficultyController.s.playerStar /*+ SpeedController.s.currentDistance / SpeedController.s.missionDistance*/;
    }

    public void OnDifficultyChanged() {
        trainPos.maxValue = 0;
        
        act1Difficulty.SetActive(false);
        act2Difficulty.SetActive(false);
        act3Difficulty.SetActive(false);
        var playerStar = WorldDifficultyController.s.playerStar;
        
        switch (WorldDifficultyController.s.playerAct) {
            case 1:
                act1Difficulty.SetActive(true);
                for (int i = 0; i < act1Difficulties.Length; i++) {
                    act1Difficulties[i].SetDeactive(i == playerStar);
                }
                break;
            case 2:
                act2Difficulty.SetActive(true);
                for (int i = 0; i < act2Difficulties.Length; i++) {
                    act2Difficulties[i].SetDeactive(i == playerStar);
                }
                break;
            case 3:
                act3Difficulty.SetActive(true);
                for (int i = 0; i < act3Difficulties.Length; i++) {
                    act3Difficulties[i].SetDeactive(i == playerStar);
                }
                break;
        }
    }
}
