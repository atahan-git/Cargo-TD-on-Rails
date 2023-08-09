using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    private Transform lastTarget;
    private void Update() {
        var closestTargetNotAvoided = ClosestTarget(true);

        if (closestTargetNotAvoided!=null)  {
            if (closestTargetNotAvoided != lastTarget) {
                targeter.SetTarget(closestTargetNotAvoided);
                lastTarget = closestTargetNotAvoided;
                OnTargetChanged?.Invoke(lastTarget);
            }
        } else {
            var closestTarget = ClosestTarget(false);

            if (closestTarget != null) {
                if (closestTarget != lastTarget) {
                    targeter.SetTarget(closestTarget);
                    lastTarget = closestTarget;
                    OnTargetChanged?.Invoke(lastTarget);
                }
            } else {
                if (lastTarget != null) {
                    targeter.UnsetTarget();
                    OnTargetUnset?.Invoke();
                }
            }
        }
    }

    private Transform ClosestTarget(bool doAvoidCheck) {
        if (LevelReferences.targetsDirty) {
            return null;
        }
        
        var closestTargetDistance = range + 1;
        Transform closestTarget = null;
        var allTargets = LevelReferences.allTargetValues;
        var allTargetsReal = LevelReferences.allTargets;
        var myId = -1;
        if (mySelfTarget != null) {
            myId = mySelfTarget.myId;
        }

        //var myDamage = targeter.GetDamage();
        var myPosition = origin.position;
        
        for (int i = 0; i < allTargets.Length; i++) {
            if (i != myId) {
                var target = allTargets[i];
                var canTarget = target.type == PossibleTarget.Type.player;
                //var targetHasEnoughHealth = !doHealthCheck || (allTargets[i].health >= myDamage);
                var targetNotAvoided = !target.avoid || !doAvoidCheck;

                if (canTarget && targetNotAvoided) {
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
