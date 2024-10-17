using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class EnemyTargetPickerAmmoTargeter : MonoBehaviour
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
            delay = Random.Range(0.1f, 0.2f);
        }
        
        if (targeter.SearchingForTargets()) {
            var closestTargetNotAvoided = ClosestTarget();

            if (closestTargetNotAvoided != null) {
                if (closestTargetNotAvoided != targeter.GetActiveTarget()) {
                    targeter.SetTarget(closestTargetNotAvoided);
                    OnTargetChanged?.Invoke(closestTargetNotAvoided);
                }
            } else {
                targeter.UnsetTarget();
            }
        } else {
            targeter.UnsetTarget();
        }
    }

    private Transform ClosestTarget() {
        var ammoTracker = Train.s.GetComponent<AmmoTracker>();

        var closestTargetDistance = range + 1;
        Transform closestTarget = null;
        var ammoProviders = ammoTracker.GetAmmoProviders();
        for (int i = 0; i < ammoProviders.Count; i++) {
            var provider = ammoProviders[i] as ModuleAmmo;
            if(provider == null)
                continue;
            
            if (provider.curAmmo > 0 && provider.myAmmoBar.allAmmoChunks.Count > 0) {
                var distance = Vector3.Distance(transform.position, provider.transform.position);
                if (distance < closestTargetDistance) {
                    closestTargetDistance = distance;
                    var pickOneFromTopCount = Mathf.Min(provider.myAmmoBar.allAmmoChunks.Count,5);
                    closestTarget = provider.myAmmoBar.allAmmoChunks[^(Random.Range(1,pickOneFromTopCount))].transform;
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
