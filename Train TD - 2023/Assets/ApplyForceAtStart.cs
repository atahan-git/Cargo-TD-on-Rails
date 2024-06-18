using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyForceAtStart : MonoBehaviour {

    public GameObject obj;
    public Transform forceSource;
    public Vector2 forceAmount = new Vector2(400, 1000);

    public float deathTime = 40f;

    void Start() {
        obj.transform.SetParent(null);
        var force = Random.Range(forceAmount.x, forceAmount.y);

        obj.GetComponent<Rigidbody>().AddForceAtPosition(forceSource.forward * force, forceSource.position);

        if (deathTime > 0) {
            Destroy(obj, deathTime);
        }
    }
}
