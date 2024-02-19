using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableIfTooFar : MonoBehaviour {

    public GameObject target;
    public float distance = 30;

    // Update is called once per frame
    void Update()
    {
        target.SetActive(target.transform.position.magnitude < distance);
    }
}
