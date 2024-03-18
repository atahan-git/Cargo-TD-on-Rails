using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidInTheDistancePositionController : MonoBehaviour
{
    
    void Update() {
        // a float between 0-4
        var missionTime = WorldDifficultyController.s.GetMissionTime();

        var asteroidGetCloserAmount = missionTime.Remap(0, 1200, 0, 230);
        asteroidGetCloserAmount = Mathf.Clamp(asteroidGetCloserAmount, 0, 230);
        
        transform.position = Vector3.forward * (250 - asteroidGetCloserAmount);
    }
}
