using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugFollowPathDistance : MonoBehaviour
{
    public float myDistance;

    public void Update() {
        transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(myDistance-SpeedController.s.currentDistance) ;
        transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(myDistance-SpeedController.s.currentDistance) /** Quaternion.Euler(0,180,0)*/;
    }
}
