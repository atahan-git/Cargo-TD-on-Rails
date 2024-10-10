using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimatedText : MonoBehaviour {

	public string[] states;

	public int framesPerSecond = 6;
	float waitSeconds {
		get {
			return 1f / framesPerSecond;
		}
	}

	public bool isPlaying = true;

	[SerializeField]
	float index;

	[SerializeField] 
	TMP_Text myText;

	private void OnEnable() {
		index = 0;
		myText.text = states[(int)index];
	}

	void Update () {
		if (isPlaying) {
			if (myText != null) {
				index += Time.unscaledDeltaTime / waitSeconds;
				if (index >= states.Length) {
					index = 0;
				}
				myText.text = states[(int)index];
			}
		}
	}
}
