using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarterTrainSelector : MonoBehaviour {
    public static StarterTrainSelector s;

    private void Awake() {
        s = this;
    }

    public GameObject sectionPrefab;
    
    public Transform sectionParent;

    public float sectionSeparation = 1;
    

    public void DrawSections() {
        sectionParent.DeleteAllChildren();
        var allChars = DataHolder.s.characters;
        var startPos = new Vector3(-((allChars.Length-1) * sectionSeparation / 2),0,0);
        MiniScript_StarterTrainSection firstPanel = null;
        for (int i = 0; i < allChars.Length; i++) {
            var panel = Instantiate(sectionPrefab, sectionParent).GetComponent<MiniScript_StarterTrainSection>();
            panel.Setup(allChars[i].myCharacter, !XPProgressionController.s.IsCharacterUnlocked(i));
            panel.transform.localPosition = startPos + new Vector3(i*sectionSeparation, 0, 0);

            if (i == 0)
                firstPanel = panel;
        }

        SelectSection(firstPanel);
    }


    public void SelectSection(MiniScript_StarterTrainSection section) {
        var pos = transform.localPosition;
        xOffset = -section.transform.localPosition.x;
        transform.localPosition = pos;
        CharacterSelector.s.SelectCharacter(section.myData);
    }


    private float xOffset = 0;
    private void Update() {
        var pos = transform.localPosition;
        pos.x = Mathf.Lerp(pos.x, xOffset, 10 * Time.deltaTime);
        transform.localPosition = pos;
    }
}
