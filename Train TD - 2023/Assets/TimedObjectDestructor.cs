using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedObjectDestructor : MonoBehaviour {

    public float timeout = 2f;

    public bool onlyDisable = false;

    private void Start() {
        if(timeout > 0)
            Invoke("DestroyNow", timeout);
    }


    public void DestroyNow() {
        if (!onlyDisable) {
            Destroy(gameObject);
        } else {
            gameObject.SetActive(false);        
        }
        
    }
}
