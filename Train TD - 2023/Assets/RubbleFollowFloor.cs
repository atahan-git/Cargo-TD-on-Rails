using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RubbleFollowFloor : MonoBehaviour {
    public float constantForce = 10f;

    public float deathTime = 20f;


    public bool isAttachedToFloor = false;


    public List<TrainTerrainData> addedChunks = new List<TrainTerrainData>();

    private void Start() {
        Invoke("DestroyNow", deathTime);
    }


    void DestroyNow() {
        Destroy(gameObject);
    }

    private void FixedUpdate() {
        deathTime -= Time.fixedDeltaTime;
        GetComponent<Rigidbody>().AddForce(Vector3.back * constantForce);
    }
    
    
    private void OnCollisionEnter(Collision other) {
        //print(other);
        //print(other.transform);
        
        if (!isAttachedToFloor) {
            var hexChunk = other.gameObject.GetComponent<TrainTerrainData>();
            if (hexChunk != null) {
                AttachToFloor(other.gameObject);
            }
        }
    }

    private void OnCollisionStay(Collision other) {
        if (!isAttachedToFloor) {
            var hexChunk = other.gameObject.GetComponent<TrainTerrainData>();
            if (hexChunk != null) {
                AttachToFloor(other.gameObject);
            }
        }
    }


    void AttachToFloor(GameObject target) {
        if (canAttachToFloor) {
            isAttachedToFloor = true;
            var hexChunk = target.GetComponent<TrainTerrainData>();
            if (hexChunk) {
                hexChunk.AddForeignObject(gameObject);
                addedChunks.Add(hexChunk);
            } else {
                transform.SetParent(target.transform);
                Invoke(nameof(_DestroySelf), 30);
            }
        }
    }


    public void InstantAttachToFloor() {
        if (Physics.Raycast(transform.position+Vector3.up*5, Vector3.down, out RaycastHit hit, 6, LevelReferences.s.groundLayer)) {
            AttachToFloor(hit.collider.gameObject);
        }
    }

    public bool canAttachToFloor = true;
    public void UnAttachFromFloor() {
        for (int i = 0; i < addedChunks.Count; i++) {
            addedChunks[i].RemoveForeignObject(gameObject);
        }
        addedChunks.Clear();
        
        transform.SetParent(VisualEffectsController.s.transform);
        
        CancelInvoke();
    }

    void _DestroySelf() {
        Destroy(gameObject);
    }
}
