using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class LightProbeMaker : MonoBehaviour {

    public Vector3 gridSize = new Vector3(10, 5, 10);
    public float density = 1;
    
    [Button]
    void MakeGridProbes() {
        List<Vector3> probeLocations = new List<Vector3>();


        Vector3Int counts = new Vector3Int(Mathf.CeilToInt(gridSize.x/density), Mathf.CeilToInt(gridSize.y/density),Mathf.CeilToInt(gridSize.z/density));

        var startLoc = transform.position - gridSize / 2f;
        var offset = new Vector3(gridSize.x / counts.x, gridSize.y / counts.y, gridSize.z / counts.z);
        
        for (int x = 0; x < counts.x; x++) {
            for (int y = 0; y < counts.y; y++) {
                for (int z = 0; z < counts.z; z++) {
                    probeLocations.Add(startLoc + new Vector3(x*offset.x, y*offset.y, z*offset.z));
                }
            }
        }
        
        
        GetComponent<LightProbeGroup>().probePositions = probeLocations.ToArray();
    }
}
