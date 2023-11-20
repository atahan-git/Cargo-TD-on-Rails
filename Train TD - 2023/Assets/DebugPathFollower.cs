using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class DebugPathFollower : MonoBehaviour
{
    
    
    public float targetDistance;
    [Range(-1,1)]
    public float smallOffset;
    public PathAndTerrainGenerator pathAndTerrainGenerator;
    void Update() {
        if(pathAndTerrainGenerator == null)
            return;
        
        var paths = pathAndTerrainGenerator.myPaths;

        var currentDistance = targetDistance+smallOffset;
        var pathIndex = 0;
        while (currentDistance > paths[pathIndex].length && currentDistance > 0) {
            currentDistance -= paths[pathIndex].length;
            pathIndex += 1;

            if (pathIndex >= paths.Count) {
                pathIndex -= 1;
                currentDistance = paths[pathIndex].length;
                break;
            }
        }
        transform.position= PathGenerator.GetPointOnLine(paths[pathIndex], currentDistance);
    }
}
