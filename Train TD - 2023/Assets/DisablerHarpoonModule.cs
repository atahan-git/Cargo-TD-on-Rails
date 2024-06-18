using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisablerHarpoonModule : MonoBehaviour, IComponentWithTarget {
    
    public GameObject harpoon;
    public Transform harpoonSpawnLocation;
    public Transform ropeEndPoint;

    public Transform harpoonRotatePoint;

    public Cart target;
    public Cart nextTarget;

    public LineRenderer _lineRenderer;

    public GameObject sparks;

    public enum HarpoonState {
        waitingForTargets, connected, disabling
    }
    

    public HarpoonState myState;
    public void UpdateColor(float percent) {
        percent = Mathf.Clamp01(percent);
        var gradient = new Gradient();

        
        var colors = new GradientColorKey[2];
        colors[0] = new GradientColorKey(Color.black, percent);
        colors[1] = new GradientColorKey(Color.white, percent+0.05f);

        var alphas = new GradientAlphaKey[2];
        alphas[0] = new GradientAlphaKey(1.0f, 1.0f);
        alphas[1] = new GradientAlphaKey(0.0f, 1.0f);

        gradient.SetKeys(colors, alphas);

        _lineRenderer.colorGradient = gradient;
        
        sparks.gameObject.SetActive(harpoonConnected);

        var pos = _lineRenderer.positionCount * percent;
        var prevPos = _lineRenderer.GetPosition(Mathf.Clamp(Mathf.FloorToInt(pos),0,_lineRenderer.positionCount-1));
        var nextPos = _lineRenderer.GetPosition(Mathf.Clamp(Mathf.CeilToInt(pos),0,_lineRenderer.positionCount-1));
        
        sparks.transform.position = Vector3.Lerp(prevPos,nextPos, pos%1);
    }

    public Gradient defaultLineColor;
    public Gradient zapFlipLineColor;
    
    //public rope
    public void SetTarget(Cart _target) {
        DisconnectHarpoon(target);
        target = _target;
    }

    private void Start() {
        _lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    void DisconnectHarpoon(Cart disconnectingTarget) {
        if (harpoonConnected) {
            if (!harpoonLerpInProgress) {
                Instantiate(harpoonDisengageEffect, harpoon.transform.position, harpoon.transform.rotation);
                
                harpoon = Instantiate(harpoon, harpoonSpawnLocation.position, harpoonSpawnLocation.rotation);
                
                harpoon.transform.SetParent(harpoonSpawnLocation);
                ropeLerpInProgress = true;
                StartCoroutine(RopeLerp());
            } else {
                StopAllCoroutines();
                harpoon.transform.position = harpoonSpawnLocation.position;
                harpoon.transform.rotation = harpoonSpawnLocation.rotation;
                
                var ropeTarget = harpoon.transform.GetChild(0);
                ropeEndPoint.transform.position = ropeTarget.transform.position;
                ropeEndPoint.SetParent(ropeTarget);
                ropeLerpInProgress = false;
                harpoonLerpInProgress = false;
            }

            harpoonConnected = false;
            SetIfCartIsActive(disconnectingTarget,true);

            harpoonReAttachTimer = 2f;
        }
    }

    public bool ropeLerpInProgress = false;

    public GameObject harpoonShootEffect;
    public GameObject harpoonDisengageEffect;
    public GameObject harpoonActiveEffect;
    IEnumerator RopeLerp() {
        ropeEndPoint.SetParent(harpoon.transform.GetChild(0));
        var ropeTarget = harpoon.transform.GetChild(0);
        while (ropeEndPoint.transform.position.y < 1) {
            ropeEndPoint.transform.position += Vector3.up * Time.deltaTime * 3;
            yield return null;
        }
        
        while (Vector3.Distance(ropeEndPoint.transform.position, ropeTarget.transform.position) > 0.01f) {
            ropeEndPoint.transform.position = Vector3.MoveTowards(ropeEndPoint.transform.position, ropeTarget.transform.position, 3 * Time.deltaTime);
            yield return null;
        }

        ropeEndPoint.transform.position = ropeTarget.transform.position;
        ropeEndPoint.SetParent(ropeTarget);

        yield return new WaitForSeconds(2);
        ropeLerpInProgress = false;
    }
    
    public bool harpoonLerpInProgress = false;
    IEnumerator HarpoonLerp() {
        var harpoonEngagePoint = new GameObject("HarpoonEngagePoint");

        if (Physics.Raycast(transform.position, target.transform.position - transform.position, out RaycastHit hit, 20, LevelReferences.s.buildingLayer)) {
            harpoonEngagePoint.transform.position = hit.point;
        } else {
            var targetCollider = target.GetHealthModule().GetMainCollider();
            harpoonEngagePoint.transform.position = targetCollider.ClosestPoint(transform.position);
        }

        harpoonEngagePoint.transform.SetParent(target.transform);


        while (Vector3.Distance(harpoon.transform.position, harpoonEngagePoint.transform.position) > 0.05f) {
            harpoon.transform.position = Vector3.MoveTowards(harpoon.transform.position, harpoonEngagePoint.transform.position, 6 * Time.deltaTime);
            yield return null;
        }

        harpoon.transform.position = harpoonEngagePoint.transform.position;
        harpoon.transform.SetParent(harpoonEngagePoint.transform);

        harpoonLerpInProgress = false;
        harpoonConnected = true;
        //SetIfCartIsActive(target,false);
    }


    public bool harpoonConnected = false;
    public bool canShoot;
    private float harpoonReAttachTimer;
    private float activeEffectTimer;
    private float waitUntilDisableTimer;
    private void Update() {
        if (target != null) {
            var lookAxis = target.shootingTargetTransform.position - harpoonRotatePoint.position;
            var lookRotation = Quaternion.LookRotation(lookAxis, Vector3.up);
            harpoonRotatePoint.rotation = Quaternion.Lerp(harpoonRotatePoint.rotation, lookRotation, 20 * Time.deltaTime);
            canShoot = Quaternion.Angle(harpoonRotatePoint.rotation, lookRotation) < 5;
        } else {
            canShoot = false;
        }

        switch (myState) {
            case HarpoonState.waitingForTargets:
                target = nextTarget;
                if (harpoonReAttachTimer > 0) {
                    harpoonReAttachTimer -= Time.deltaTime;
                } else {
                    if (!ropeLerpInProgress && !harpoonLerpInProgress && canShoot) {
                        harpoonLerpInProgress = true;
                        Instantiate(harpoonShootEffect, harpoon.transform.position, harpoon.transform.rotation);
                        StartCoroutine(HarpoonLerp());
                        myState = HarpoonState.connected;
                        waitUntilDisableTimer = 0;
                    }
                }
                
                break;
            case HarpoonState.connected:
                if (harpoonConnected) {
                    if (waitUntilDisableTimer > 0) {
                        waitUntilDisableTimer -= Time.deltaTime;
                    } else {
                        StartCoroutine(DisableTarget());
                        myState = HarpoonState.disabling;
                        return;
                    }
                    
                    if (nextTarget != null && nextTarget != target) {
                        DisconnectHarpoon(target);
                        myState = HarpoonState.waitingForTargets;
                        harpoonReAttachTimer = 2f+waitUntilDisableTimer;
                    }
                } else {
                    if (!ropeLerpInProgress && !harpoonLerpInProgress) {
                        myState = HarpoonState.waitingForTargets;
                    }
                }

                break;
            case HarpoonState.disabling:
                
                break;
        }
    }

    IEnumerator DisableTarget() {
        SetIfCartIsActive(target, false);

        var disableTime = 5f;
        var switchColorTime = 0f;
        var curColor = false;
        
        while (disableTime > 0) {

            if (switchColorTime > 0) {
                switchColorTime -= Time.deltaTime;
            } else {
                switchColorTime += 0.3f;
                curColor = !curColor;
                _lineRenderer.colorGradient = curColor ? zapFlipLineColor : defaultLineColor;
            }
            

            disableTime -= Time.deltaTime;
            yield return null;
        }
        
        
        SetIfCartIsActive(target, true);
        _lineRenderer.colorGradient = defaultLineColor;

        waitUntilDisableTimer = 5f;
        myState = HarpoonState.connected;
    }

    public GameObject activeCartDisableEffect;
    void SetIfCartIsActive(Cart cart, bool isActive) {
        if (activeCartDisableEffect != null) {
            var part = activeCartDisableEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (var p in part) {
                p.Stop();
            }

            Destroy(activeCartDisableEffect, 1f);
            activeCartDisableEffect = null;
        }
        
        if (cart != null && cart.gameObject != null) {
            if (isActive) {
                cart.isBeingDisabled = false;
                cart.SetDisabledState();
            } else {
                cart.isBeingDisabled = true;
                var ammoModule = cart.GetComponentInChildren < ModuleAmmo>();
                if (ammoModule) {
                    ammoModule.dontLoseAmmoInThisDisable = true;
                }
                cart.SetDisabledState();

                activeCartDisableEffect = VisualEffectsController.s.SmartInstantiate(LevelReferences.s.cartBeingDisabledEffect, cart.genericParticlesParent);
            }
        }
    }

    private void OnDestroy() {
        if (!gameObject.scene.isLoaded) //Was Deleted
        {
            return;
        }
        SetIfCartIsActive(target, true);
    }

    public void SetTarget(Transform target) {
        nextTarget = target.GetComponentInParent<Cart>();
    }

    public void UnsetTarget() {
        nextTarget = null;
    }

    public Transform GetRangeOrigin() {
        return transform;
    }

    public Transform GetActiveTarget() {
        if (nextTarget != null) {
            return nextTarget.transform;
        } 
        return null;
    }

    public bool SearchingForTargets() {
        return true;
    }
}
