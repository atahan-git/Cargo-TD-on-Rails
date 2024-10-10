using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatHazeController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Serializable]
    public class HeatHazeSettings {
        public float speed = 0.03f;
        public float angleSpeed = 5;
        public float angleOffset = 20;
        public float cellDensity = 50;
        public float strength = 0.005f;
        public float topFadeBegin = 0.27f;
        public float topFadeEnd = 0.87f;
    }
}
