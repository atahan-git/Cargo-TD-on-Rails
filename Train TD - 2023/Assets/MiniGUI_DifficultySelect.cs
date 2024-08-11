using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_DifficultySelect : MonoBehaviour {
    public int currentDifficulty = 0;

    public DifficultyTweakableHolder[] difficultyTweakableHolders;

    public Button[] myButtons;

    public TMP_Text title;
    public TMP_Text description;
    
    // Start is called before the first frame update
    public void InitDiff() {
        if (DataSaver.s.GetCurrentSave().isInARun) {
            currentDifficulty = DataSaver.s.GetCurrentSave().currentRun.difficulty;
        } else {
            currentDifficulty = DataSaver.s.GetCurrentSave().lastDifficultySelected;
        }
        SelectDifficulty(currentDifficulty);
    }

    public void SelectDifficulty(int difficulty) {
        currentDifficulty = difficulty;
        currentDifficulty = Mathf.Clamp(currentDifficulty, 0, difficultyTweakableHolders.Length);

        var myHolder = difficultyTweakableHolders[currentDifficulty];
        
        TweakablesMaster.s.difficultyTweakables = myHolder.myTweakables.Copy();
        title.text =$"Current Difficulty: {myHolder.title}";
        description.text = myHolder.description;

        for (int i = 0; i < myButtons.Length; i++) {
            myButtons[i].interactable = true;
        }

        myButtons[currentDifficulty].interactable = false;

        DataSaver.s.GetCurrentSave().lastDifficultySelected = currentDifficulty;
        if (DataSaver.s.GetCurrentSave().isInARun) {
            currentDifficulty = DataSaver.s.GetCurrentSave().currentRun.difficulty;
        }
    }


    public void Cancel() {
        SetDisplayStatus(false);
    }

    public void BeginGame() {
        MainMenu.s.StartGameAfterDifficultySelectScreen();
    }

    public void SetDisplayStatus(bool isActive) {
        gameObject.SetActive(isActive);
        if (isActive) {
            InitDiff();
        }
    }
}
