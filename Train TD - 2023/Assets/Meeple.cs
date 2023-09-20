using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Meeple : MonoBehaviour {

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
    public GameObject chatPrefab;

    public void SetUp(MeepleZone zone) {
        myZone = zone;
        if (Random.value > staticMeepleChance) {
            Invoke(nameof(PickNewTarget), Random.Range(moveTimes.x, moveTimes.y));
        }
        
        
        // instant go to a location
        myPos = myZone.GetPointInZone();
        transform.position = GetGroundPosition(myPos);
        targetActualPos = transform.position;
        targetActualRot = transform.rotation;
    }

    void PickNewTarget() {
        if (isBeingHandled) {
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            isBeingHandled = false;
            myPos = transform.localPosition;
            targetActualPos = transform.position;
            targetActualRot = transform.rotation;
            myPos.y = 0;
        }
        
        
        if (!isMoving) {
            curSpeed = Random.Range(moveSpeed.x, moveSpeed.y);
            target = myZone.GetPointInZone();
            isMoving = true;
            lookRotation = Quaternion.LookRotation(target - myPos);
        }

        Invoke(nameof(PickNewTarget), Random.Range(moveTimes.x, moveTimes.y)*4);
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

            transform.position = Vector3.Lerp(transform.position, targetActualPos, 5 * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetActualRot, 20 * Time.deltaTime);
        }
    }

    private bool stopBeingHandledWhenHitGround = false;

    private float fallDelta;
    public void SetHandlingState(bool state) {
        if (state) {
            isBeingHandled = true;
            CancelInvoke();
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Rigidbody>().useGravity = false;
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

    private void OnCollisionEnter(Collision collision) {
        if (stopBeingHandledWhenHitGround) {
            CancelInvoke();
            if (fallDelta > 0.5f) {
                GetComponent<Rigidbody>().AddTorque(Random.onUnitSphere * Random.Range(0,5));
            }
            Invoke(nameof(PickNewTarget), fallDelta);
        }
    }


    public void ShowChat() {
        heroSound.PlayClip();
    }


    //in local pos, out global pos
    Vector3 GetGroundPosition(Vector3 pos) {
        pos = myZone.transform.position + pos;
        
        if (Physics.Raycast(pos + Vector3.up * 3, Vector3.down, out RaycastHit hit, 10, LevelReferences.s.groundLayer | LevelReferences.s.buildingLayer)) {
            return hit.point;
        } else {
            return pos + Vector3.up * MeepleZone.groundLevel;
        }
    }
}
