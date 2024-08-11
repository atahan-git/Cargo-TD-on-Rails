using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemReward_CamSwitchBridge : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) {
        GetComponentInParent<GemRewardOnRoad>()?.OnTriggerEnterCameraSwitch(other);
        GetComponentInParent<CartRewardOnRoad>()?.OnTriggerEnterCameraSwitch(other);
    }
}
