using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidInTheDistancePositionController : MonoBehaviour
{
    
    void Update() {
        // a float between 0-4
        var currentAct = WorldDifficultyController.s.playerAct;
        var currentActDistanceModifier = (currentAct - 1) * 50;
        var actDistance = WorldDifficultyController.s.playerStar + SpeedController.s.currentDistance / SpeedController.s.missionDistance;
        var actDistanceModifier = actDistance * (40f / 4f);
        transform.position = Vector3.forward * (250 - (currentActDistanceModifier + actDistanceModifier));
    }
}
