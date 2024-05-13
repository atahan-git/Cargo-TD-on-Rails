using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
		visualText.maxVisibleCharacters = textTarget.Length;
		visualText.text = textTarget;
		visualText.ForceMeshUpdate();
		var parsedText = visualText.GetParsedText();
		var pauseChars =new []{'.','?','!',','};
		visualText.maxVisibleCharacters = 0;
		while (curIndex < parsedText.Length) {

			visualText.maxVisibleCharacters = curIndex+1;

			soundCounter += 1;
			if (soundCounter >= soundPerChar) {
				sounds.PlayClip(source.position, myPitch);
				soundCounter = 0;
			}


			var curChar = parsedText[curIndex];
			if (pauseChars.Contains(curChar)) {
				yield return new WaitForSeconds(speakSpeed*16);
			} else {
				yield return new WaitForSeconds(speakSpeed);
			}
			
			curIndex += 1;
		}
		isDone = true;
		
		yield return new WaitForSeconds(visualText.GetParsedText().Length/15f);
		Destroy(gameObject);
	}

	public void InstantComplete() {
		StopAllCoroutines();
		visualText.text = textTarget;
		visualText.maxVisibleCharacters = textTarget.Length;
		isDone = true;
		Invoke(nameof(DestroySelf), visualText.GetParsedText().Length/15f);
	}

	void DestroySelf() {
		Destroy(gameObject);
	}
}
