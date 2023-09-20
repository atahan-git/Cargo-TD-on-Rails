using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RubbleFollowFloor : MonoBehaviour {
    public float constantForce = 10f;

    public float deathTime = 20f;


    public bool isAttachedToFloor = false;

    private void Start() {
        Invoke("DestroyNow", deathTime);
    }


    void DestroyNow() {
        Destroy(gameObject);
    }

    private void FixedUpdate() {
        GetComponent<Rigidbody>().AddForce(Vector3.back * constantForce);
    }
    
    
    private void OnCollisionEnter(Collision other) {
        if (!isAttachedToFloor) {
            if (other.transform.parent != null && other.transform.parent.CompareTag("Ground")) {
                AttachToFloor(other.gameObject);
            }
        }
    }

    private void OnCollisionStay(Collision other) {
        if (!isAttachedToFloor) {
            if (other.transform.parent != null && other.transform.parent.CompareTag("Ground")) {
                AttachToFloor(other.gameObject);
            }
        }
    }


    void AttachToFloor(GameObject target) {
        isAttachedToFloor = true;
        var hexChunk = target.GetComponentInParent<HexChunk>();
        if (hexChunk) {
            hexChunk.AddForeignObject(gameObject);
        } else {
            transform.SetParent(target.transform);
            Destroy(gameObject, 30);
        }
    }


    public void InstantAttachToFloor() {
        if (Physics.Raycast(transform.position+Vector3.up*5, Vector3.down, out RaycastHit hit, 6, LevelReferences.s.groundLayer)) {
            AttachToFloor(hit.collider.gameObject);
        }
    }
}
