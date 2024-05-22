using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class EnemyTargetPicker : MonoBehaviour
{
   private IComponentWithTarget targeter;

    public float range = 5f;

    private Transform origin;

    private PossibleTarget mySelfTarget;


    [NonSerialized]
    public UnityEvent<Transform> OnTargetChanged = new UnityEvent<Transform>();
    [NonSerialized]
    public UnityEvent OnTargetUnset = new UnityEvent();

    private void Start() {
        targeter = GetComponent<IComponentWithTarget>();
        origin = targeter.GetRangeOrigin();
        mySelfTarget = GetComponentInParent<PossibleTarget>();
    }

    public float delay;
    private void Update() {
        if (delay > 0) {
            delay -= Time.deltaTime;
            return;
        } else {
            delay = Random.Range(1f, 3f);
        }
        
        if (targeter.SearchingForTargets()) {
            var closestTargetNotAvoided = ClosestTarget(true);

            if (closestTargetNotAvoided != null) {
                if (closestTargetNotAvoided != targeter.GetActiveTarget()) {
                    targeter.SetTarget(closestTargetNotAvoided);
                    OnTargetChanged?.Invoke(closestTargetNotAvoided);
                }
            } else {
                var closestTarget = ClosestTarget(false);

                if (closestTarget != null) {
                    if (closestTarget != targeter.GetActiveTarget()) {
                        targeter.SetTarget(closestTarget);
                        OnTargetChanged?.Invoke(closestTargetNotAvoided);
                    }
                } else {
                    if (targeter.GetActiveTarget() != null) {
                        targeter.UnsetTarget();
                        OnTargetUnset?.Invoke();
                    }
                }
            }
        } else {
            targeter.UnsetTarget();
        }
    }

    private Transform ClosestTarget(bool doAvoidCheck) {
        if (LevelReferences.s.targetsDirty) {
            return null;
        }
        
        var closestTargetDistance = range + 1;
        Transform closestTarget = null;
        var allTargets = LevelReferences.s.allTargetValues;
        var allTargetsReal = LevelReferences.s.allTargets;
        var myId = -1;
        if (mySelfTarget != null) {
            myId = mySelfTarget.myId;
        }

        //var myDamage = targeter.GetDamage();
        var myPosition = origin.position;
        
        for (int i = 0; i < allTargets.Length; i++) {
            if (i != myId) {
                var target = allTargets[i];
                var targetActive = target.active;
                var canTarget = target.type == PossibleTarget.Type.player;
                var targetHasEnoughHealth = target.health > 0;
                var targetNotAvoided = !target.avoid || !doAvoidCheck;

                if (targetActive && canTarget && targetNotAvoided && targetHasEnoughHealth) {
                    if (IsPointInsideRange(allTargets[i].position, myPosition, range, out float distance)) {
                        if (distance < closestTargetDistance) {
                            closestTarget = allTargetsReal[i].targetTransform;
                            closestTargetDistance = distance;
                        }
                    }
                }
            }
        }

        return closestTarget;
    }
    
    
    private void OnDrawGizmosSelected() {
        targeter = GetComponent<IComponentWithTarget>();
        if (targeter != null) {
            origin = targeter.GetRangeOrigin();
            if (origin != null) {
                var radians = 2 * Mathf.PI;

                var scaleAdjustment = (1f / origin.lossyScale.x);

                var resolution = radians / 0.1f;
                var resolutionStep = (radians * 2 / resolution);
                for (int i = 0; i < resolution; i++) {
                    var start = -radians + i * resolutionStep;
                    var stop = -radians + (i + 1) * resolutionStep;

                    var leftEdgeNew = new Vector3(Mathf.Sin(start), 0, Mathf.Cos(start));
                    var rightEdgeNew = new Vector3(Mathf.Sin(stop), 0, Mathf.Cos(stop));
                    leftEdgeNew = origin.TransformPoint(leftEdgeNew * range* scaleAdjustment);
                    rightEdgeNew = origin.TransformPoint(rightEdgeNew * range* scaleAdjustment);
                    Gizmos.DrawLine(leftEdgeNew, rightEdgeNew);
                }
            }
        }
    }


    public static bool IsPointInsideRange ( Vector3 point, Vector3 coneOrigin, float maxDistance, out float distance)
    {
        distance = ( point - coneOrigin ).magnitude;
        if ( distance < maxDistance )
        {
            return true;
        }
        return false;
    }
    
    public void ActivateForCombat() {
        this.enabled = true;
    }

    public void Disable() {
        this.enabled = false;
    }
}
