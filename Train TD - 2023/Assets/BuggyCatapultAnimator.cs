using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuggyCatapultAnimator : MonoBehaviour
{
    public float baseRotation;
    public float shotRotation;

    public GameObject rotatingBit;

    public GameObject bullet;

    private BuggyCatapult myGun;

    public float currentCharge;
    public float chargeSpeed;

    private void Start() {
        myGun = GetComponentInParent<BuggyCatapult>();
        currentCharge = 1;
        myGun.onBulletFiredEvent.AddListener(OnShoot);
    }

    void OnShoot() {
        chargeSpeed = 1f / (myGun.GetFireDelay());
        currentCharge = 0;
        Update();
        currentCharge = 0;
    }

    private const float fullCharge = 0.9f;
    void Update() {
        var percent = Mathf.Clamp(currentCharge, 0, fullCharge) / fullCharge;

        bullet.SetActive(currentCharge > fullCharge);

        rotatingBit.transform.localRotation = Quaternion.Euler(Mathf.Lerp(shotRotation, baseRotation, percent),0,0);
        currentCharge += Time.deltaTime * chargeSpeed;
    }
}
