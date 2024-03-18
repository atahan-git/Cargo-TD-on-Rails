using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicSpawnEnemies : MonoBehaviour
{
    public void SpawnEnemies(int count) {
        var enemy = transform.GetChild(0);
        
        for (int i = 0; i < count-1; i++) {
            Instantiate(enemy, transform);
        }
    }
}
