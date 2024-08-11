using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemHolderModule : MonoBehaviour {
    public int capacity = 2;
    public float holdRadius = 1f;


    public GameObject holdEffectPrefab;
    
    public List<Holder> currentlyHolding = new List<Holder>();


    [Serializable]
    public class Holder {
        public IPlayerHoldable item;
        public ItemHolderHoldEffect holdEffect;
        public Vector3 targetPos;
        public float lerpDelayTimer = 1f;
        public Vector3 smoothDamp;
        public float randomFloatOffset;
    }

    // Update is called once per frame
    void LateUpdate()
    {

        for (int i = currentlyHolding.Count-1; i >= 0; i--) {
            if (currentlyHolding[i].item == null || currentlyHolding[i].item as MonoBehaviour == null || (currentlyHolding[i].item as MonoBehaviour).gameObject == null) {
                StopHolding(i);
            }
        }
        
        for (int i = currentlyHolding.Count-1; i >= 0; i--) {
            if (!LevelReferences.s.combatHoldableThings.Contains(currentlyHolding[i].item)) {
                StopHolding(i);
            }
        }

        if (currentlyHolding.Count < capacity) {
            IPlayerHoldable closestThing = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < LevelReferences.s.combatHoldableThings.Count; i++) {
                var alreadyHolding = false;
                for (int j = 0; j < currentlyHolding.Count; j++) {
                    if (currentlyHolding[j].item == LevelReferences.s.combatHoldableThings[i]) {
                        alreadyHolding = true;
                        break;
                    }
                }

                if (alreadyHolding) {
                    continue;
                }
                
                var mono = LevelReferences.s.combatHoldableThings[i] as MonoBehaviour;
                var dist = Vector3.Distance(transform.position, mono.transform.position);
                if (dist < closestDistance) {
                    closestDistance = dist;
                    closestThing = LevelReferences.s.combatHoldableThings[i];
                }
            }

            if (closestThing != null) {
                BeginHolding(closestThing);
            }
        }
        
        WiggleAndBringCloserCurrentlyCaughtThings();

        /*for (int i = 0; i < 500; i++) {
            var randomPoint = transform.position + Random.onUnitSphere * 2;
            var randomAdjustedPoint = transform.TransformPoint( GetHoldPos(randomPoint));
            //Debug.DrawLine(transform.position, randomPoint, Color.red, 10f);
            Debug.DrawLine(transform.position, randomAdjustedPoint, Color.green);
        }*/
    }

    void WiggleAndBringCloserCurrentlyCaughtThings() {
        var trainForward = Train.s.GetTrainForward();
        
        for (int i = 0; i < currentlyHolding.Count; i++) {
            var currentHoldMono = currentlyHolding[i].item as MonoBehaviour;
            if (currentlyHolding[i].lerpDelayTimer > 0) {
                currentlyHolding[i].lerpDelayTimer -= Time.deltaTime;
                if (currentlyHolding[i].lerpDelayTimer <= 0) {
                    var rubbleFollowFloor = currentHoldMono.GetComponent<RubbleFollowFloor>();
                    if (rubbleFollowFloor) {
                        rubbleFollowFloor.UnAttachFromFloor();
                        rubbleFollowFloor.canAttachToFloor = false;
                    }
                    var rg = currentHoldMono.GetComponent<Rigidbody>();
                    currentlyHolding[i].smoothDamp = rg.velocity;
                    rg.isKinematic = true;
                    rg.useGravity = false;
                    
                    currentlyHolding[i].targetPos = GetHoldPos(currentHoldMono.transform.position);
                    currentlyHolding[i].holdEffect = Instantiate(holdEffectPrefab, transform).GetComponent<ItemHolderHoldEffect>();
                    currentlyHolding[i].randomFloatOffset = Random.Range(0, 100);
                }
                continue;
            }
            
            // move points slightly away from each other if they are too close
            
            Debug.DrawLine(transform.position, transform.TransformPoint(currentlyHolding[i].targetPos));

            for (int j = i+1; j < currentlyHolding.Count; j++) {
                PushPointsApartIfTooClose( (currentlyHolding[i].item as MonoBehaviour).transform.position,  (currentlyHolding[j].item as MonoBehaviour).transform.position,
                    ref currentlyHolding[i].targetPos, ref currentlyHolding[j].targetPos);
            }

            var targetHoldPos = currentlyHolding[i].targetPos;
            var floatAnimTime = (Time.time + currentlyHolding[i].randomFloatOffset);
            targetHoldPos.y += Mathf.Sin( floatAnimTime* 0.4f) * 0.1f;
            targetHoldPos.x += Mathf.Sin(floatAnimTime * 0.22f) * 0.1f;
            targetHoldPos.z += Mathf.Sin(floatAnimTime * 0.2f) * 0.1f;

            if (currentHoldMono is Cart) {
                targetHoldPos.y -= 0.7f;
            }

            currentHoldMono.transform.position += trainForward * LevelReferences.s.speed * Time.deltaTime;
            currentHoldMono.transform.position = Vector3.SmoothDamp(currentHoldMono.transform.position, transform.TransformPoint(targetHoldPos), ref currentlyHolding[i].smoothDamp, 1f, 2f);
            //currentHoldMono.transform.position = transform.TransformPoint(targetHoldPos);
            currentHoldMono.transform.rotation = Quaternion.Lerp(currentHoldMono.transform.rotation, Quaternion.identity, 5*Time.deltaTime);
            
            currentlyHolding[i].holdEffect.SetPositions(transform.position, currentlyHolding[i].item);
        }
    }


    public void BeginHolding(IPlayerHoldable thingToHold) {
        currentlyHolding.Add(new Holder(){item = thingToHold, lerpDelayTimer = 0.75f});
    }

    Vector3 GetHoldPos(Vector3 targetPos) {
        var fixedPos = transform.InverseTransformPoint(targetPos);
        return FindValidPointOnCircle(fixedPos);
    }
    
    
    Vector3 FindValidPointOnCircle(Vector3 direction) {
        var radius = 1f;

        if (currentlyHolding.Count > 5) {
            radius = 1 + (currentlyHolding.Count-5)*0.08f;
        }
        
        radius = Mathf.Clamp(radius, 1, 3f);
        
        direction.Normalize();

        var angle = Quaternion.LookRotation(direction).eulerAngles;

        var forwardBannedAngleRange = 45;
        if (angle.y < forwardBannedAngleRange) {
            angle.y = forwardBannedAngleRange;
        }

        if (angle.y > 360 - forwardBannedAngleRange) {
            angle.y = 360 - forwardBannedAngleRange;
        }

        if (angle.y > 180 - forwardBannedAngleRange && angle.y < 180 + forwardBannedAngleRange) {
            if (angle.y < 180) {
                angle.y = 180 - forwardBannedAngleRange;
            } else {
                angle.y = 180 + forwardBannedAngleRange;
            }
        }

        var heightAngleMin = 35;
        var heightAngleMax = 50;
        if (angle.x > 360 - heightAngleMin) {
            angle.x = 360 - heightAngleMin;
        }

        if (angle.x < 360 - heightAngleMax) {
            angle.x = 360 - heightAngleMax;
        }

        return Quaternion.Euler(angle) * Vector3.forward * radius;
    }
    

    private void PushPointsApartIfTooClose( Vector3 realPointI,  Vector3 realPointJ, ref Vector3 pointI, ref Vector3 pointJ) {
        float distance = Vector3.Distance(realPointI, realPointJ);

        var minDistance = 0.6f;
        if (distance < minDistance) {
            var ItoJ = realPointJ-realPointI;

            if (distance <= 0) {
                ItoJ = Random.onUnitSphere;
            }
            
            // Find new positions by pushing away from the midpoint
            Vector3 newPointI = pointI - ItoJ;
            Vector3 newPointJ = pointJ + ItoJ;

            
            // Recalculate valid points based on constraints
            var newPointi = FindValidPointOnCircle(newPointI);
            var newPointj = FindValidPointOnCircle(newPointJ);
            
            pointI = Vector3.MoveTowards(pointI, newPointi, 0.25f * Time.deltaTime);
            pointJ = Vector3.MoveTowards(pointJ, newPointj, 0.25f * Time.deltaTime);
        }
    }

    void StopHolding(int i) {
        // we dont modify rigidbody status and such because the only way to stop holding something is if PlayerWorldInteractionController has grabbed it and it will do its own thing
        var toRemove = currentlyHolding[i];
        currentlyHolding.RemoveAt(i);
        if (toRemove.holdEffect != null) {
            Destroy(toRemove.holdEffect.gameObject);
        }
    }
}
