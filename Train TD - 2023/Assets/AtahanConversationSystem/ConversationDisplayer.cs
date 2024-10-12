using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ConversationSystem;

public class ConversationDisplayer : MonoBehaviour {

	public static ConversationDisplayer s;

	public TMP_Text mainDisplayText;
	public Image portraitDisplayImage;
	public GameObject nextDisp;
	public GameObject holdToSkip;
	public GameObject waitOverlay;

	public GameObject conversationParent;

	public float conversationShowSpeed = 0.01f;
	public float tutorialSpeed = 0.03f;
	float curConversationShowSpeed = 0.03f; //used to set 0 to fastforward text
	[SerializeField]
	bool isShowing = false;

	public float timeSinceLastSkip = 0;
	public int skipCounter = 0;
	public bool canHold = false;

	private void Start() {
		Clear();
		conversationParent.SetActive (false);
	}

	public void StartConversation(Conversation _conversation) {
		StartConversation(_conversation, 0);
	}

	private bool blockHoldUntilUnclick = false;
	public bool conversationInProgress = false;
	public void StartConversation(Conversation _conversation, float initialDelay) {
		if (conversationInProgress) {
			Debug.LogError("Trying to start two conversations at the same time!");
		}

		conversationInProgress = true;
		blockHoldUntilUnclick = true;
		startSkipAllTimer = false;
		canHold = false;
		skipCounter = 0;
		DataSaver.s.GetCurrentSave().storyProgress.seenConversations.Add(_conversation.conversationUniqueID);
		StartCoroutine(_ShowConversation(_conversation, initialDelay));
	}

	public IEnumerator _ShowConversation(Conversation conversation, float initialDelay) {
		Debug.Log($"Conversation started: \"{conversation.conversationName}\"");
		Clear();

		while (initialDelay > 0) {
			if (Time.timeScale > 0) {
				initialDelay -= Time.deltaTime;
			} else {
				initialDelay -= Time.unscaledDeltaTime;
			}
			yield return null;
		}
		
		OnConversationStart();
		
		for (int i = 0; i < conversation.remarks.Length; i++) {
			var currentRemark = conversation.remarks[i];
			var isInstantRemark = currentRemark.text.Length == 0;

			if (isInstantRemark) {
				print($"instant remark {i}");
				SetRemarkSprites(currentRemark);
			} else {
				print($"showing remark {i}");
				yield return StartCoroutine(ShowRemark(currentRemark));

				print("wait until next remark");
				yield return new WaitUntil(() => nextRemark || skipAll);
				if (skipAll) {
					yield return new WaitForSecondsRealtime(0.1f);
				}
			}
		}
		
		OnConversationEnd();
		Debug.Log($"Conversation complete: \"{conversation.conversationName}\"");
	}

	void OnConversationStart() {
		conversationParent.SetActive (true);
		Pauser.s.Pause(false);
		Pauser.s.blockPauseStateChange = true;
	}

	void OnConversationEnd() {
		conversationInProgress = false;
		conversationParent.SetActive (false);
		Pauser.s.blockPauseStateChange = false;
		Pauser.s.Unpause();
	}
	
	void Awake () {
		s = this;
	}

	private void Update () {
		if (isShowing) {
			timeSinceLastSkip += Time.unscaledDeltaTime;

			if (timeSinceLastSkip > 1.5f) {
				timeSinceLastSkip = 0;
				skipCounter = 0;
			}

			if (skipCounter >= 3) {
				holdToSkip.SetActive(true);
				canHold = true;
			}
		}

		if (startSkipAllTimer && !blockHoldUntilUnclick) {
			skipAllTimer += Time.unscaledDeltaTime;
			if (skipAllTimer > 0.5f) {
				skipAll = true;
			}
		}
	}

	public Image[] bigSpriteSlots;

	private void DisplayBigSprite (BigSpriteAction action, BigSpriteSlots slot, Sprite img) {
		try {
			switch (action) {
			case BigSpriteAction.Show:
				bigSpriteSlots[(int)slot].sprite = img;
				bigSpriteSlots[(int)slot].color = new Color (1, 1, 1, 1);
				break;
			case BigSpriteAction.Hide:
				bigSpriteSlots[(int)slot].sprite = null;
				bigSpriteSlots[(int)slot].color = new Color (1, 1, 1, 0);
				break;
			default:

				break;
			}
		} catch (System.Exception e) {
			Debug.LogError (this.name + e);
		}
	}
	
	//Commands cheat sheet: <delay='waitSeconds'>, <wait='click/enabled'>
	// others <tutorial></tutorial>
	IEnumerator ShowRemark (Remark remark) {
		var text = remark.text;
		
		isShowing = true;
		nextRemark = false;
		nextDisp.SetActive (false);
		string _text = "";
		mainDisplayText.text = _text;
		
		SetRemarkSprites(remark);

		int n = 0;
		for (int i = 0; i < text.Length; i++) {
			if (text[i] == '<') {
				string command = "";
				while (text[i + 1] != '>') {
					i++;
					command += text[i];
				}
				i += 1;
				string[] values = command.Split ('=');
				if (values.Length > 1) {
					print ("Found custom command: " + values[0] + " = " + values[1]);
				} else {
					print ("Found custom decorator: " + values[0]);
				}


				switch (values[0]) {
					case "speed":
						float multiplier = 0f;
						try {
							multiplier = float.Parse(values[1]);
						} catch {
							Debug.LogError("Can't parse delay value: " + values[1]);
						}

						curConversationShowSpeed = conversationShowSpeed /multiplier;
						print($"yeet {curConversationShowSpeed} - {conversationShowSpeed} - {multiplier}");

						break;
					case "delay":
						float waitSeconds = 0f;
						try {
							waitSeconds = float.Parse(values[1]);
						} catch {
							Debug.LogError("Can't parse delay value: " + values[1]);
						}

						float timer = 0;
						while (timer < waitSeconds) {
							timer += Time.unscaledDeltaTime;
							if (curConversationShowSpeed <= 0f)
								break;
							yield return null;
						}
						
						break;
					case "wait":
						switch (values[1]) {
							case "click":
								waitOverlay.SetActive(true);
								nextDisp.SetActive(true);
								yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
								waitOverlay.SetActive(false);
								nextDisp.SetActive(false);
								break;
							case "enabled":
								yield return new WaitUntil(() => conversationParent.activeSelf);
								break;
							default:
								Debug.LogError("Unknown wait value: " + values[1]);
								break;
						}

						break;
					case "tutorial": {
						_text += "<b><color=#16a700>";
						curConversationShowSpeed = tutorialSpeed;
						skipAll = false;
						skipAllTimer = -0.5f;
						nextRemark = false;
						break;
					}
					case "/tutorial": {
						_text += "</b></color>";
						curConversationShowSpeed = conversationShowSpeed;
						break;
					}
					default:
						//this is not a command we recognize, but a command TextMeshPro recognizes, so do add it to the text
						_text += "<" + command + ">";
						break;
				}
			} else {
				if (i < text.Length) {
					_text += text[i];
					
					mainDisplayText.text = _text;

					if (i + 5 < text.Length) { // dont put delay at the end
						if (text[i] == ',') {
							if (curConversationShowSpeed > 0f && !skipAll) {
								yield return new WaitForSecondsRealtime(0.4f);
							}
						}

						if (text[i] == '.') {
							if (curConversationShowSpeed > 0f && !skipAll) {
								yield return new WaitForSecondsRealtime(0.5f);
							}
						}
						
						if(text[i] == '!' || text[i] == '?') {
							if (curConversationShowSpeed > 0f && !skipAll) {
								yield return new WaitForSecondsRealtime(1f);
							}
						}
					}
				}

				if (curConversationShowSpeed > 0f && !skipAll) {
					yield return new WaitForSecondsRealtime(curConversationShowSpeed);
				}
			}
			
		}


		curConversationShowSpeed = conversationShowSpeed;
		isShowing = false;
		nextDisp.SetActive (true);
	}

	private void SetRemarkSprites(Remark remark) {
		portraitDisplayImage.sprite = ConversationEnumToSprite.s.GetPortrait(remark.portrait);

		for (int i = 0; i < remark.bigSpriteAction.Length; i++) {
			DisplayBigSprite(remark.bigSpriteAction[i], remark.bigSpriteSlot[i], ConversationEnumToSprite.s.GetBigImage(remark.bigSprite[i]));
		}
	}

	private void Clear () {
		nextDisp.SetActive (false);
		mainDisplayText.text = "";
		
		portraitDisplayImage.sprite = null;

		DisplayBigSprite (BigSpriteAction.Hide, BigSpriteSlots.left, null);
		DisplayBigSprite (BigSpriteAction.Hide, BigSpriteSlots.middle, null);
		DisplayBigSprite (BigSpriteAction.Hide, BigSpriteSlots.right, null);

		holdToSkip.SetActive (false);

		waitOverlay.SetActive (false);
	}

	private bool nextRemark;
	private bool skipAll;
	private float skipAllTimer = 0;
	private bool startSkipAllTimer = false;
	public void NextRemark () {
		if (isShowing) {
			curConversationShowSpeed = -1;
			skipCounter++;
			timeSinceLastSkip = 0;
		} else {
			nextRemark = true;
		}
	}

	public void PointerDown () {
		startSkipAllTimer = true;
		skipAllTimer = 0;
	}

	public void PointerUp () {
		blockHoldUntilUnclick = false;
		startSkipAllTimer = false;
		skipAllTimer = 0;
		skipAll = false;
	}
}
