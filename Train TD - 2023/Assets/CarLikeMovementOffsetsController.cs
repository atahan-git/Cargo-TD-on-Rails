using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CarLikeMovementOffsetsController : MonoBehaviour{

    
    public Transform[] wheels;

    public Vector2 randomBumpTimer = new Vector2(0.1f, 1f);
    public float curTime = 1f;
    public Vector2 randomBumpForce = new Vector2(200, 1000);
    
    public Vector2 randomSmallBumpTimer = new Vector2(0.05f, 0.2f);
    public float curSmallTime = 1f;
    public Vector2 randomSmallBumpForce = new Vector2(50, 400);

    public float positionDelta = 0.2f;
    public float lerpSpeed = 3f;
    public float targetMoveSpeed = 0.1f;
    public Vector3 target;
    public Vector3 targetTargetPosition;

    float lookRotationDelta = 20f;
    
    public Vector3 centerOfMass  = new Vector3(0, -0.1f, 0);

    public float forwardForce = 100f;

    private Rigidbody rg;
    
    private void Start() {
        rg = GetComponent<Rigidbody>();
        rg.centerOfMass = centerOfMass;
        transform.position += Vector3.up*1;
        rg.velocity = Vector3.zero;
    }

    private void FixedUpdate() {
        UpdatePosition();
        ApplyForces();
    }

    public bool stickToGround = false;

    private void UpdatePosition() {
        target.y = transform.localPosition.y;
        if (stickToGround) {
            if (Physics.Raycast(transform.position + Vector3.up * 20, Vector3.down, out RaycastHit hit, 100, LevelReferences.s.groundLayer)) {
                target.y = hit.point.y + 0.1f;
                //print(hit.point.y);
            }
        }


        transform.localPosition = Vector3.Lerp(transform.localPosition, target, lerpSpeed * Time.deltaTime);

        targetTargetPosition.y = target.y;
        target = Vector3.MoveTowards(target, targetTargetPosition, targetMoveSpeed * Time.deltaTime * GetRealMoveSpeed());

        if (Vector3.Distance(target, targetTargetPosition) < 0.01f) {
            var randomInCircle = Random.insideUnitCircle;
            randomInCircle *= positionDelta;
            targetTargetPosition = new Vector3(randomInCircle.x, 0, randomInCircle.y);
        }

        if (transform.position.y < -1) {
            transform.position += Vector3.up*1;
            rg.velocity = Vector3.zero;
        }


        var lookRotation = rg.velocity;
        lookRotation.z = 0.5f;
        lookRotation.y = 0;
        lookRotation.x = Mathf.Clamp(lookRotation.x, -0.2f, 0.2f);
        var realLookRotation = Quaternion.LookRotation(lookRotation, Vector3.up);
        var eulerAngles = transform.rotation.eulerAngles;
        /*eulerAngles.x = Mathf.Clamp(eulerAngles.x, -45, 45);
        eulerAngles.z = Mathf.Clamp(eulerAngles.x, -45, 45);*/
        var adjustedLookRotation = Quaternion.Euler(eulerAngles.x, realLookRotation.y, eulerAngles.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, adjustedLookRotation, lookRotationDelta * Time.deltaTime);
    }

    private void ApplyForces() {
        curTime -= Time.fixedDeltaTime;
        if (curTime <= 0) {
            var randomWheel = wheels[Random.Range(0, wheels.Length)];
            var force = Random.Range(randomBumpForce.x, randomBumpForce.y) * GetRealMoveSpeed();
            rg.AddForceAtPosition(Vector3.up * force, randomWheel.transform.position);

            curTime = Random.Range(randomBumpTimer.x, randomBumpTimer.y);
        }
        
        curSmallTime -= Time.fixedDeltaTime;
        if (curSmallTime <= 0) {
            var randomWheel = wheels[Random.Range(0, wheels.Length)];
            var force = Random.Range(randomSmallBumpForce.x, randomSmallBumpForce.y) * GetRealMoveSpeed();
            rg.AddForceAtPosition(Vector3.up * force, randomWheel.transform.position);

            curSmallTime = Random.Range(randomSmallBumpTimer.x, randomSmallBumpTimer.y);
        }
        
        //GetComponent<Rigidbody>().AddForce(transform.forward * forwardForce);
    }

    float GetRealMoveSpeed() {
        return Mathf.Clamp(LevelReferences.s.speed/3f, -2, 2);
    }
}
