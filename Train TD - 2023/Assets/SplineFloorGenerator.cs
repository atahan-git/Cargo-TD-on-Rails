using System;
using System.Collections;
using System.Collections.Generic;
using Dreamteck;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

public class SplineFloorGenerator : MeshGenerator {
    
    
    [Header("values")]
    protected int slicesX = 1;
    public float innerRoadWidth = 8;
    public float transitionWidth = 1;
    public float outerDetailWidth = 8;
    public int transitionDetail = 4;
    public float edgeBaseUp = 1;
    [Header("Inner details")]
    public float magnitudeInner = 0.2f;
    public float frequencyInner = 2.65f;
    [Header("Outer details")]
    public float magnitudeOuter = 0.2f;
    public float frequencyOuter = 2.65f;
    [Header("Transition Details")] 
    public float magnitudeTransition = 0.5f;
    public float maxMagnitudeTransition = 2f;
    public float frequencyTransition = 1;

    [Header("Flatness")] 
    public float flatSectionLength = 10;
    public float flatSectionTransition = 2;

    public bool isLeftSide = true;

    public AnimationCurve continentalnessMap = AnimationCurve.Linear(0,0,1,1);
    public AnimationCurve transitionMap = AnimationCurve.Linear(0,0,1,1);

    [Button]
    public void BuildMeshEditor() {
        RebuildImmediate();
    }
    protected override void BuildMesh() {
        GenerateVertices();
        MeshUtility.GeneratePlaneTriangles(ref _tsMesh.triangles, slicesX, (sampleCount), false);
    }


    enum TransitionState {
        inner, transition, outer
    }
    void GenerateVertices() {
        var yDist = spline.CalculateLength() / sampleCount;
        var transitionDist = transitionWidth / transitionDetail;
        var innerSlices = Mathf.CeilToInt(innerRoadWidth / yDist);
        var outerSlices = Mathf.CeilToInt(outerDetailWidth / yDist);
        slicesX = innerSlices + outerSlices + transitionDetail;
        int vertexCount = (slicesX + 1) * (sampleCount);
        AllocateMesh(vertexCount, slicesX * ((sampleCount) - 1) * 6);
        int vertexIndex = 0;

        ResetUVDistance();

        bool hasOffset = offset != Vector3.zero;
        var currentDistance = 0f;
        GetSample(0, ref evalResult);
        var lastDistance = evalResult.position;
        for (int i = 0; i < (sampleCount); i++) {
            GetSample(i, ref evalResult);
            
            Vector3 center = Vector3.zero;
            try {
                center = evalResult.position;
            } catch (System.Exception ex) {
                Debug.Log(ex.Message + " for i = " + i);
                return;
            }

            currentDistance += Vector3.Distance(lastDistance, center);
            //print(currentDistance);
            lastDistance = center;

            var sideEdgeTransitionMultipler = 0f;
            if (currentDistance >= flatSectionLength) {
                var delta = currentDistance - flatSectionLength;
                if (flatSectionTransition > 0) {
                    sideEdgeTransitionMultipler = delta / flatSectionTransition;
                } else {
                    sideEdgeTransitionMultipler = 1;
                }

                sideEdgeTransitionMultipler = Mathf.Clamp01(sideEdgeTransitionMultipler);
            }

            Vector3 right = evalResult.right;
            float resultSize = GetBaseSize(evalResult);
            if (hasOffset) {
                center += (offset.x * resultSize) * right + (offset.y * resultSize) * evalResult.up + (offset.z * resultSize) * evalResult.forward;
            }

            //float fullSize = size * resultSize;
            float fullSize = (innerSlices*yDist) + (transitionDist*transitionDetail) + (outerSlices*yDist);
            Vector3 lastVertPos = Vector3.zero;
            Quaternion rot = Quaternion.AngleAxis(rotation, evalResult.forward);
            if (uvMode == MeshGenerator.UVMode.UniformClamp || uvMode == MeshGenerator.UVMode.UniformClip) AddUVDistance(i);
            Color vertexColor = GetBaseColor(evalResult) * color;

            var beginVertIndex = vertexIndex;
            var distance = 0f;

            if (!isLeftSide) {
                distance = -fullSize;
            }
            
            
            var transitionBloatInner = transitionMap.Evaluate(Mathf.PerlinNoise(center.x * frequencyTransition, center.z * frequencyTransition))*magnitudeTransition;
            var transitionBloatOuter = transitionMap.Evaluate(Mathf.PerlinNoise(center.z * frequencyTransition, center.x * frequencyTransition))*magnitudeTransition;
            var total = (transitionBloatInner + transitionBloatOuter)/maxMagnitudeTransition;
            if (total > 1) {
                transitionBloatInner *= 1 / total;
                transitionBloatOuter *= 1 / total;
            }
            var realTransitionDist = (transitionDist * transitionDetail + transitionBloatInner + transitionBloatOuter) / transitionDetail;
            var realTransitionTotal = realTransitionDist * transitionDetail;
            var transitionCurrent = 0f;

            var curState = TransitionState.inner;
            var sideEdgeTransitionInnerMultipler = 0f;
            for (int n = 0; n < slicesX +1; n++) {

                if (isLeftSide) {
                    sideEdgeTransitionInnerMultipler = ((float)n-(innerSlices+transitionDetail)) / outerSlices;
                    sideEdgeTransitionInnerMultipler = 1- sideEdgeTransitionInnerMultipler;
                } else {
                    sideEdgeTransitionInnerMultipler = (float)n/outerSlices;
                }

                sideEdgeTransitionInnerMultipler = Mathf.Clamp01(sideEdgeTransitionInnerMultipler);

                if (sideEdgeTransitionMultipler >= 1) {
                    sideEdgeTransitionInnerMultipler = 1;
                }

                var randomInner = 0f;
                var randomOuter = 0f;
                
                var distanceFromTransition = 0f;
                var transitionPercent = 0f;
                
                // find vertex pos. Also do some calcs for later
                if ((isLeftSide && n < innerSlices) || (!isLeftSide && n > slicesX-innerSlices+1)) { // inner path
                    curState = TransitionState.inner;
                    if(n > 0)
                        distance += yDist;

                    if (isLeftSide) {
                        if (n == innerSlices - 1)
                            distance -= transitionBloatInner;
                    } else {
                        if (n == (slicesX - innerSlices + 1 + 1)) {
                            distance -= transitionBloatInner - (realTransitionTotal-transitionCurrent);
                        }
                    }
                } else if(((isLeftSide && n < innerSlices+transitionDetail) || (!isLeftSide && n > slicesX-(innerSlices+transitionDetail)+1))){ //transition
                    curState = TransitionState.transition;
                    //distance += realTransitionDist;
                    if (isLeftSide) {
                        transitionPercent = (((float)n - innerSlices)) / (transitionDetail-1);
                    }else{
                        transitionPercent = 1-(((float)n - (slicesX-(innerSlices+transitionDetail))) / (transitionDetail-1));
                    }

                    var distanceDelta = transitionPercent;
                    if (distanceDelta > 0.5f) {
                        distanceDelta = 1 - distanceDelta;
                    }
                    distanceDelta *= 2;
                    distanceDelta = ParametricBlend(distanceDelta,2);
                    //Debug.Log(distanceDelta);
                    //Debug.Log($"{transitionPercent} - {distanceDelta}");

                    transitionCurrent += realTransitionDist * (distanceDelta + 0.2f);
                    distance +=  realTransitionDist * (distanceDelta+0.2f);
                    transitionPercent = ParametricBlend(transitionPercent, 3);
                } else {  // outer details
                    curState = TransitionState.outer;
                    if(n > 0)
                        distance += yDist;

                    if (!isLeftSide) {
                        if (n == outerSlices +1)
                            distance -= transitionBloatOuter;

                        distanceFromTransition = 1-(float)n/outerSlices;
                    } else {
                        if (n == innerSlices + transitionDetail) {
                            distance -= transitionBloatOuter - (realTransitionTotal-transitionCurrent);
                        }

                        distanceFromTransition = (float)(n-(innerSlices + transitionDetail))/outerSlices ;
                    }

                    distanceFromTransition = distanceFromTransition.Remap(0, 0.5f, 0.2f, 1f);
                }
                distanceFromTransition = Mathf.Clamp(distanceFromTransition,0.25f,1f);
                
                // set vertex pos
                var vertexPos = center - rot * right * (distance) + rot * evalResult.up;

                // calculate random variations
                if (!((isLeftSide && n == 0 ) || (!isLeftSide && n == slicesX))) {
                    randomInner = (Mathf.PerlinNoise(vertexPos.x * frequencyInner, vertexPos.z * frequencyInner) - 0.5f) * magnitudeInner;
                    
                    randomOuter = (Mathf.PerlinNoise(vertexPos.x * frequencyOuter, vertexPos.z * frequencyOuter));
                    randomOuter = continentalnessMap.Evaluate(randomOuter);
                    randomOuter *= magnitudeOuter * distanceFromTransition;
                    randomOuter *= sideEdgeTransitionMultipler;
                }
                
                // apply randomness based on state
                float upAmount;
                switch (curState) {
                    case TransitionState.inner:
                        vertexPos += Vector3.up * randomInner;
                        break;
                    case TransitionState.transition:
                        upAmount = Mathf.Lerp(randomInner, randomOuter, transitionPercent) + (transitionPercent * edgeBaseUp);
                        vertexPos += Vector3.up * (upAmount  * sideEdgeTransitionMultipler*sideEdgeTransitionInnerMultipler);
                        break;
                    case TransitionState.outer:
                        upAmount = randomOuter + edgeBaseUp;
                        vertexPos += Vector3.up * (upAmount  * sideEdgeTransitionMultipler*sideEdgeTransitionInnerMultipler);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (vertexIndex == 0)
                    print(vertexPos);
                
                _tsMesh.vertices[vertexIndex] = vertexPos;


                var slicePercent = distance / fullSize;
                //print($"{outerCount}/{outerSlices} - {transitionCount}/{transitionDetail} - {innerCount}/{innerSlices}");
                
                slicePercent = Mathf.Clamp01(slicePercent);
                CalculateUVs(evalResult.percent, 1f - slicePercent);
                _tsMesh.uv[vertexIndex] = Vector2.one * 0.5f + (Vector2)(Quaternion.AngleAxis(uvRotation + 180f, Vector3.forward) * (Vector2.one * 0.5f - __uvs));
                
                _tsMesh.colors[vertexIndex] = vertexColor;
                vertexIndex++;
            }


            vertexIndex = beginVertIndex;
            lastVertPos = Vector3.zero;
            for (int n = 0; n < slicesX +1; n++) {
                if (slicesX > 1) {
                    if (n < slicesX) {
                        Vector3 nextVertPos = _tsMesh.vertices[vertexIndex+1];
                        Vector3 cross1 = -Vector3.Cross(evalResult.forward, nextVertPos - _tsMesh.vertices[vertexIndex]).normalized;

                        if (n > 0) {
                            Vector3 cross2 = -Vector3.Cross(evalResult.forward, _tsMesh.vertices[vertexIndex] - lastVertPos).normalized;
                            _tsMesh.normals[vertexIndex] = Vector3.Slerp(cross1, cross2, 0.5f);
                        } else _tsMesh.normals[vertexIndex] = cross1;
                    } else _tsMesh.normals[vertexIndex] = -Vector3.Cross(evalResult.forward, _tsMesh.vertices[vertexIndex] - lastVertPos).normalized;
                } else {
                    _tsMesh.normals[vertexIndex] = evalResult.up;
                    if (rotation != 0f) _tsMesh.normals[vertexIndex] = rot * _tsMesh.normals[vertexIndex];
                }

                _tsMesh.colors[vertexIndex] = vertexColor;
                lastVertPos = _tsMesh.vertices[vertexIndex];
                vertexIndex++;
            }
        }
    }
    
    float ParametricBlend(float t,float alpha)
    {
        return Mathf.Pow(t, alpha) / (Mathf.Pow(t, alpha) + Mathf.Pow((1-t),alpha));
    }
}
