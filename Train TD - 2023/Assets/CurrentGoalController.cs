using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CurrentGoalController : MonoBehaviour {
    public static CurrentGoalController s;

    private void Awake() {
        s = this;
    }

    [SerializeField]
    TMP_Text currentGoalText;

    [SerializeField]
    GameObject currentGoalArea;

    private void Start() {
        SetText("Reach the Meteor");
    }

    public void SetText(string text) {
        currentGoalArea.SetActive(true);
        currentGoalText.text = text;
    }

    public void ClearText() {
        currentGoalArea.SetActive(false);
        currentGoalText.text = "";
    }
}
