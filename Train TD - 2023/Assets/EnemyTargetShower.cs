using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTargetShower : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    public Transform myTarget;
    float shootWidth = 2;
    float targetChangeWidth = 4;
    float newlyEnteredWidth = 4f;
    public float currentWidth = 0;
    public float widthDecayPerSec = 2f; //decay per sec

    public float gunTargetLineSpace = 0.1f;
    
    // Start is called before the first frame update
    void Start() {
        _lineRenderer = GetComponent<LineRenderer>();
        GetComponentInParent<EnemyTargetPicker>().OnTargetChanged.AddListener(OnTargetChanged);
        GetComponentInParent<EnemyTargetPicker>().OnTargetUnset.AddListener(OnNoTarget);
        GetComponentInParent<GunModule>().onBulletFiredEvent.AddListener(OnShoot);
        GetComponentInParent<EnemyWave>().OnEnemyEnter.AddListener(OnEnter);
    }

    void OnTargetChanged(Transform newTarget) {
        if (myTarget != null) {
            var targeter = myTarget.GetComponentInParent<PossibleTarget>();
            targeter.enemiesTargetingMe.Remove(this);
        }
        
        myTarget = newTarget;
        myTarget.GetComponentInParent<PossibleTarget>().enemiesTargetingMe.Add(this);
        currentWidth = Mathf.Max(currentWidth, targetChangeWidth);
    }

    void OnNoTarget() {
        if (myTarget != null) {
            var targeter = myTarget.GetComponentInParent<PossibleTarget>();
            targeter.enemiesTargetingMe.Remove(this);
        }

        myTarget = null;
        currentWidth = 0;
    }

    void OnShoot() {
        currentWidth = Mathf.Max(currentWidth, shootWidth);
    }


    void OnEnter() {
        currentWidth = Mathf.Max(currentWidth, newlyEnteredWidth);
    }

    private void OnDestroy() {
        if (myTarget != null) {
            var targeter = myTarget.GetComponentInParent<PossibleTarget>();
            targeter.enemiesTargetingMe.Remove(this);
        }
    }

    private void Update() {
        var drawLine = currentWidth > 0 && myTarget != null;
        _lineRenderer.enabled = drawLine;
        if (drawLine) {
            var myPos = transform.position;
            var targetPos = myTarget.position;
            var direction =  targetPos -myPos;
            _lineRenderer.SetPosition(0, myPos + direction * gunTargetLineSpace);
            _lineRenderer.SetPosition(1, targetPos - direction * 0.1f);

            _lineRenderer.widthMultiplier = currentWidth*0.02f;
            currentWidth -= Time.deltaTime * widthDecayPerSec;
            if (currentWidth < 0.5f) {
                currentWidth = 0.5f;
            }
        }
    }
}
