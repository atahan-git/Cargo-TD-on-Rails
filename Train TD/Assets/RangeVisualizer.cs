using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangeVisualizer : MonoBehaviour {
    public TargetPicker targetPicker;
    private IComponentWithTarget targeter;

    public LineRenderer rangeEdgeRenderer;
    public LineRenderer targetingRenderer;

    private TrainBuilding trainBuilding;
    void Start() {
        targeter = targetPicker.GetComponent<IComponentWithTarget>();
        trainBuilding = GetComponentInParent<TrainBuilding>();
        targetingRenderer.positionCount = 2;
        //ChangeVisualizerStatus(false);
        DrawRangeEdge();

        trainBuilding.rotationChangedEvent += DrawRangeEdge;
    }


    void DrawRangeEdge() {
        if (rangeEdgeRenderer.enabled) {
            var range = targetPicker.range;
            var points = new List<Vector3>();
            if (targeter != null) {
                var origin = targeter.GetRangeOrigin();
                if (origin != null) {
                    var rotationSpan = targetPicker.rotationSpan;
                    if (targetPicker.myOverride != null && targetPicker.myOverride.isOverride) {
                        if (targetPicker.myOverride.rotations.Contains(trainBuilding.myRotation)) {
                            rotationSpan = targetPicker.myOverride.rotationSpanOverride;
                        }
                    }
                    
                    
                    var radians = Mathf.Deg2Rad * rotationSpan;
                    var scaleAdjustment = (1f / origin.lossyScale.x);

                    DrawSide(radians, origin, range, scaleAdjustment, points, false);

                    
                    DrawEdge(radians, origin, range, scaleAdjustment, points);
                    
                    
                    DrawSide(radians, origin, range, scaleAdjustment, points, true);
                }
            }

            rangeEdgeRenderer.positionCount = points.Count;
            rangeEdgeRenderer.SetPositions(points.ToArray());
        }
    }

    private static void DrawEdge(float radians, Transform origin, float range, float scaleAdjustment, List<Vector3> points) {
        var resolution = radians / 0.05f;
        var resolutionStep = (radians * 2 / resolution);
        for (int i = 0; i < resolution + 1; i++) {
            var start = -radians + i * resolutionStep;

            var leftEdgeNew = new Vector3(Mathf.Sin(start), 0, Mathf.Cos(start));
            leftEdgeNew = origin.TransformPoint(leftEdgeNew * range * scaleAdjustment);
            points.Add(leftEdgeNew);
        }
    }

    private static void DrawSide(float radians, Transform origin, float range, float scaleAdjustment, List<Vector3> points, bool isLeft) {
        if (isLeft) {
            var leftEdge = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));
            
            leftEdge = origin.TransformPoint(leftEdge * range * scaleAdjustment);
            
            points.Add(origin.position +((leftEdge-origin.position) * 0.1f));
            points.Add(leftEdge);
            
        } else {
            var rightEdge = new Vector3(Mathf.Sin(-radians), 0, Mathf.Cos(-radians));

            rightEdge = origin.TransformPoint(rightEdge * range * scaleAdjustment);
            
            points.Add(rightEdge);
            points.Add(origin.position + ((rightEdge-origin.position) * 0.1f));
        }
        
    }

    void Update()
    {
        if (!trainBuilding.isBuilt) {
            DrawRangeEdge();
        }
        
        DrawTargeter();
    }

    public float startDistance = 0.1f;
    public float endDistance = 0.3f;
    void DrawTargeter() {
        var target = targeter.GetActiveTarget();
        var targetPos = Vector3.zero;
        if (target == null) {
            targetPos = targeter.GetRangeOrigin().position + targeter.GetRangeOrigin().forward * targetPicker.range;
        } else {
            targetPos = target.position;
        }

        var vectorFromTargetToTargetingOrigin = targeter.GetRangeOrigin().position - targetPos;
        targetingRenderer.SetPositions(new [] {
            targetPos + vectorFromTargetToTargetingOrigin*startDistance,
            targetPos + vectorFromTargetToTargetingOrigin*endDistance,
        });
    }

    public void ChangeVisualizerStatus(bool isActive) {
        rangeEdgeRenderer.enabled = isActive;
    }
}
