using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightShellController : MonoBehaviour {

    public Light[] lights;
    public float lightYOffset = 1.22f;
    public Transform[] outerBones;
    public Vector2 outerStartEndOffsets;
    public Transform[] innerBones;
    public Vector2 innerStartEndOffsets;
    public float yOffset = 0.658f;

    void LateUpdate() {
        if (Train.s.carts.Count > 0) {
            var trainEngine = Train.s.carts[0];
            transform.position = trainEngine.transform.position;
        }

        {
            var spanLength = Train.s.GetTrainLength() + 1f;
            var startDist = spanLength / 2f;

            var stepDistance = spanLength / (lights.Length-1);
            for (int i = 0; i < lights.Length; i++) {
                lights[i].transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(startDist - i*stepDistance) + Vector3.up*lightYOffset;
            }
        }
        
        
        {
            var spanLength = Train.s.GetTrainLength();
            var startDist = spanLength / 2f;
            startDist += innerStartEndOffsets.x;
            spanLength += innerStartEndOffsets.x + innerStartEndOffsets.y;
            var stepDistance = spanLength / (innerBones.Length-1);
            for (int i = 0; i < innerBones.Length; i++) {
                var dist = startDist - i * stepDistance;
                innerBones[i].transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(dist) + Vector3.up*yOffset;
                innerBones[i].transform.rotation = Quaternion.Euler(180, 0, 0) * Quaternion.Inverse( PathAndTerrainGenerator.s.GetRotationOnActivePath(dist));
            }
        }

        {
            var spanLength = Train.s.GetTrainLength();
            var startDist = spanLength / 2f;
            startDist += outerStartEndOffsets.x;
            spanLength += outerStartEndOffsets.x + outerStartEndOffsets.y;
            var stepDistance = spanLength / (outerBones.Length - 1);
            for (int i = 0; i < outerBones.Length; i++) {
                var dist = startDist - i * stepDistance;
                outerBones[i].transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(dist) + Vector3.up * yOffset;
                outerBones[i].transform.rotation = Quaternion.Euler(180, 0, 0) * Quaternion.Inverse(PathAndTerrainGenerator.s.GetRotationOnActivePath(dist));
            }
        }
    }
}
