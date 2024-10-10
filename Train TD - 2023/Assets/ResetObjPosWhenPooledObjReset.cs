using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetObjPosWhenPooledObjReset : MonoBehaviour {

    public GameObject toReset;
    public Vector3 offset;
    public void ResetObject() {
        toReset.transform.localPosition = offset;
    }
}
