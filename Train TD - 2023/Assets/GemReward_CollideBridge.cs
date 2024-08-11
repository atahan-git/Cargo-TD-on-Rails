using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemReward_CollideBridge : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) {
        GetComponentInParent<GemRewardOnRoad>()?.OnTriggerEnterCollision(other);
        GetComponentInParent<CartRewardOnRoad>()?.OnTriggerEnterCollision(other);
    }
}
