using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class Meeple : MonoBehaviour, IPlayerHoldable {

    public MeepleZone myZone;

    public float staticMeepleChance = 0.1f;
    public Vector2 moveTimes = new Vector2(1, 10);
    public Vector2 moveSpeed = new Vector2(0.1f, 0.2f);

    private float curSpeed;
    private Vector3 myPos;
    private Vector3 target;
    private bool isMoving = false;
    private Quaternion lookRotation;

    public bool isBeingHandled = false;

    public MeepleSound heroSound;
    [ReadOnly]
    public MeepleSpeechBubble myPrevBubble;


    public bool isSpecialMeeple = false;
    public float basePitch = 1.4f;


    private void Start() {
        if (isSpecialMeeple) {
            targetActualPos = transform.localPosition;
            targetActualRot = transform.rotation;
            myPos = targetActualPos;
            target = targetActualPos;
            lookRotation = targetActualRot;
        }

        lastSpeech = Random.Range(0, speech.Length);
    }

    public void SetUp(MeepleZone zone) {
        myZone = zone;
        if (Random.value > staticMeepleChance) {
            Invoke(nameof(PickNewTarget), Random.Range(moveTimes.x, moveTimes.y));
        }
        
        
        // instant go to a location
        myPos = myZone.GetPointInZone();
        transform.localPosition = GetGroundPosition(myPos);
        targetActualPos = transform.localPosition;
        targetActualRot = transform.rotation;
    }

    void PickNewTarget() {
        if (isBeingHandled) {
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            isBeingHandled = false;
            myPos = transform.localPosition;
            myPos.y = 0;
            targetActualPos = transform.localPosition;
            targetActualRot = transform.rotation;
        }


        if (!isMoving) {
            curSpeed = Random.Range(moveSpeed.x, moveSpeed.y);
            if (!isSpecialMeeple) {
                target = myZone.GetPointInZone();
                lookRotation = Quaternion.LookRotation(target - myPos);
            }

            if (Vector3.Distance(targetActualPos, target) > 0.5f) {
                isMoving = true;
            }
        }

        if (!isSpecialMeeple) {
            Invoke(nameof(PickNewTarget), Random.Range(moveTimes.x, moveTimes.y) * 4);
        }
    }

    private bool cycle;
    private bool lastDirection;
    private Vector3 targetActualPos;
    private Quaternion targetActualRot;
    void Update() {
        if (!isBeingHandled) {
            if (isMoving) {
                var animTime = Mathf.Sin(Time.time * 200 * curSpeed);
                if (cycle) {
                    if (animTime > 0) {
                        cycle = false;

                        targetActualPos = GetGroundPosition(myPos);
                        targetActualRot = lookRotation;
                    }
                } else {
                    if (animTime < 0) {
                        cycle = true;

                        targetActualRot = lookRotation * Quaternion.Euler(Vector3.forward * Random.Range(10, 15) * (lastDirection ? 1 : -1));
                        lastDirection = !lastDirection;
                        targetActualPos = GetGroundPosition(myPos) + transform.up * Random.Range(0.07f, 0.1f);
                    }
                }

                myPos = Vector3.MoveTowards(myPos, target, curSpeed * Time.deltaTime * 4);

                if (Vector3.Distance(myPos, target) <= 0.001f) {
                    isMoving = false;
                    targetActualPos = GetGroundPosition(myPos);
                    targetActualRot = lookRotation;
                }
            }

            transform.localPosition = Vector3.Lerp(transform.localPosition, targetActualPos, 5 * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetActualRot, 20 * Time.deltaTime);
        }
    }

    private bool stopBeingHandledWhenHitGround = false;

    private float fallDelta;
    public void SetHoldingState(bool state) {
        if (state) {
            isBeingHandled = true;
            CancelInvoke();
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Rigidbody>().useGravity = false;

            GetClicked();
        } else {
            GetComponent<Rigidbody>().isKinematic = false;
            GetComponent<Rigidbody>().useGravity = true;
            stopBeingHandledWhenHitGround = true;

            bool fall = Random.value < 0.1f;
            fallDelta = (fall) ? Random.Range(.9f,2f) : Random.Range(0.2f,0.5f);
            /*if (fall) {
                GetComponent<Rigidbody>().AddTorque(Random.onUnitSphere * Random.Range(0,5));
            }*/

            Invoke(nameof(PickNewTarget), 20);
        }
    }

    public bool CanDrag() {
        return PlayStateMaster.s.isShopOrEndGame();
    }

    private void OnCollisionEnter(Collision collision) {
        if (stopBeingHandledWhenHitGround) {
            CancelInvoke();
            if (fallDelta > 0.5f) {
                GetComponent<Rigidbody>().AddTorque(Random.onUnitSphere * Random.Range(0,5));
            }
            Invoke(nameof(PickNewTarget), fallDelta);
        }
    }


    //in local pos, out local pos
    Vector3 GetGroundPosition(Vector3 pos) {
        pos = transform.parent.position + pos;

        var outPos = Vector3.zero;
        if (Physics.Raycast(pos + Vector3.up * 3, Vector3.down, out RaycastHit hit, 10)) {
            outPos= hit.point;
        } else {
            outPos= pos + Vector3.up * MeepleZone.groundLevel;
        }

        return transform.parent.InverseTransformPoint(outPos);
    }

    Action callAfterClick;
    public void SpecialMeepleSpeak(string text, Action _callAfterClick) {
        MeepleSpeechMaster.s.Speak(this,text);
        ShowClickPrompt(true);
        callAfterClick = _callAfterClick;
    }

    public GameObject clickPromptPrefab;
    private GameObject currentClickPrompt;
    public void ShowClickPrompt(bool state) {
        if (currentClickPrompt == null) {
            currentClickPrompt = Instantiate(clickPromptPrefab, LevelReferences.s.uiDisplayParent);
            currentClickPrompt.GetComponent<UIElementFollowWorldTarget>().SetUp(transform);
        }
        
        currentClickPrompt.SetActive(state);
    }

    private string[] speech = new[] {
        "How's your day going?",
        "It's a long way to the meteor.",
        "Life after the meteor arrived is tough but worth living.",
        "I wish I had a train like yours.",
        "At least we have the city walls protecting us.",
        "Get out of the city already.",
    };

    private int lastSpeech = 0;
    public void GetClicked() {
        heroSound.PlayClip();
        if (isSpecialMeeple) {
            if (MeepleSpeechMaster.s.RemoveBubble(this)) {
                ShowClickPrompt(false);
                callAfterClick?.Invoke();
                callAfterClick = null;
            }
        } else {
            if (myPrevBubble != null) {
                MeepleSpeechMaster.s.RemoveBubble(this);
            } else {
                lastSpeech += 1;
                lastSpeech %= speech.Length;
                MeepleSpeechMaster.s.Speak(this, speech[lastSpeech]);
            }
        }
    }

    public Transform GetUITargetTransform() {
        return transform;
    }
    
}
