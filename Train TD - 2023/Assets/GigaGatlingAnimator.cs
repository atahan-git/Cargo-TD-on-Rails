using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GigaGatlingAnimator : MonoBehaviour
{
    private EnemyGunModule _gunModule;

    public Transform rotatingBit;
    public float rotationSpeedMultiplier = 1;
    // Start is called before the first frame update
    void Start() {
        _gunModule = GetComponentInParent<EnemyGunModule>();
    }

    private void Update() {
        var curSpeed = 1f/_gunModule.GetFireDelay();


        if (curSpeed > 0.1f) {
            rotatingBit.Rotate(0, curSpeed * Time.deltaTime * rotationSpeedMultiplier, 0);
        }
    }
}
