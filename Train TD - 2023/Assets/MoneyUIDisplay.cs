using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MoneyUIDisplay : MonoBehaviour {

    public GameObject[] moneySegments;
    public bool autoUpdateBasedOnSaveData;
    public static MoneyUIDisplay totalMoney;

    private void OnEnable() {
        if (autoUpdateBasedOnSaveData) {
            totalMoney = this;
        }
    }

    private float amount;
    void Update() {
        //var money = DataSaver.s.GetCurrentSave().metaProgress.money;
        if (autoUpdateBasedOnSaveData) {
            amount = Mathf.MoveTowards(amount, DataSaver.s.GetCurrentSave().metaProgress.money, 5 * Time.deltaTime);
            SetAmount(Mathf.CeilToInt(amount));
        }
    }


    public void SetAmount(int money) {
        for (int i = 0; i < moneySegments.Length-1; i++) {
            int digit = money / (int)Mathf.Pow(10, i) % 10;
            moneySegments[i].SetActive(digit > 0);
            moneySegments[i].GetComponentInChildren<TMP_Text>().text = $"{digit}x";
        }

        int lastSegment = money / (int)Mathf.Pow(10, moneySegments.Length - 1);
        moneySegments[moneySegments.Length-1].SetActive(lastSegment > 0);
        moneySegments[moneySegments.Length-1].GetComponentInChildren<TMP_Text>().text = $"{lastSegment}x";
    }
}
