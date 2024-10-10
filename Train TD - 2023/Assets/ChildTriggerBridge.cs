using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildTriggerBridge : MonoBehaviour {
    public Projectile _projectile;

    private void OnTriggerEnter(Collider other) {
        _projectile.ChildTriggerEnter(other);
    }
}
