using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightShellController : MonoBehaviour {

    public Light[] lights;

    void LateUpdate() {
        if (Train.s.carts.Count > 0) {
            var trainEngine = Train.s.carts[0];
            transform.position = trainEngine.transform.position;
        }

        var trainLength = Train.s.GetTrainLength() + 1f;
        var startDist = -trainLength / 2f;
        
        for (int i = 0; i < lights.Length; i++) {
            lights[i].transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(startDist + trainLength / lights.Length);
        }
    }
}
