using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MeepleSpeechBubble : MonoBehaviour {

	public TMP_Text sizeText;
	public TMP_Text visualText;

	private string textTarget;
	private int curIndex;

	private float speakSpeed = 0.025f;
	private int soundPerChar = 4;
	private RandomClipPlayer sounds;

	private Transform source;
	private float myPitch = 1;
	public void ShowText(Transform _source, string text, float basePitch) {
		myPitch = basePitch;
		source = _source;
		GetComponent<UIElementFollowWorldTarget>().SetUp(source);
		sounds = GetComponentInChildren<RandomClipPlayer>();
		
		sizeText.text = text;

		textTarget = text;
		curIndex = 0;
		visualText.text = "";
		StopAllCoroutines();
		isDone = false;
		StartCoroutine(ShowSpeech());
	}


	private int soundCounter = 0;

	public bool isDone = false;
	IEnumerator ShowSpeech() {
		while (curIndex < textTarget.Length-1) {
			curIndex += 1;
			if (textTarget[curIndex] == '<') {
				while (textTarget[curIndex] != '>') {
					curIndex += 1;
				}
			}

			visualText.text = textTarget.Substring(0, curIndex + 1);

			soundCounter += 1;
			if (soundCounter >= soundPerChar) {
				sounds.PlayClip(source.position, myPitch);
				soundCounter = 0;
			}

			if (textTarget[curIndex] == '.' || textTarget[curIndex] == '?' || textTarget[curIndex] == '!' || textTarget[curIndex] == ',') {
				yield return new WaitForSeconds(speakSpeed*16);
			} else {
				yield return new WaitForSeconds(speakSpeed);
			}
		}

		isDone = true;
	}

	public void InstantComplete() {
		StopAllCoroutines();

		visualText.text = textTarget;
		isDone = true;
	}
}
