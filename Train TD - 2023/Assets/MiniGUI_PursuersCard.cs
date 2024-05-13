using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class MiniGUI_PursuersCard : MonoBehaviour {


	[ReadOnly]
    public GameObject followTarget;

    public GameObject gfx;
    public TMP_Text timeText;
    void Start() {
	    followTarget = new GameObject("Pursuer Warning UI World Position (dynamic)");
	    var followScript =  gameObject.AddComponent<UIElementFollowWorldTarget>();
	    followScript.SetUp(followTarget.transform);
	    gfx.SetActive(false);
	    PlayStateMaster.s.OnCombatEntered.AddListener(OnCombatStart);
	    PlayStateMaster.s.OnCombatFinished.AddListener(OnCombatEnd);
    }

    public bool isActive = false;

    void OnCombatStart() {
	    gfx.SetActive(true);
	    isActive = true;
    }

    void OnCombatEnd(bool isReal) {
	    gfx.SetActive(false);
	    isActive = false;
    }

    private void Update() {
	    if (isActive) {
		    followTarget.transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(-8);
		    timeText.text = ExtensionMethods.FormatTime(EnemyWavesController.s.curMiniWaveTime);
	    }
    }
}
