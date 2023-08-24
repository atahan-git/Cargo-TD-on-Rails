using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("Dreamteck/Splines/Users/Path Generator With Random")]
    public class PathGeneratorWithRandom : MeshGenerator
    {
        public int slicesX
        {
            get { return _slicesX; }
            set
            {
                if (value != _slicesX)
                {
                    if (value < 1) value = 1;
                    _slicesX = value;
                    Rebuild();
                }
            }
        }
        
        public int slicesY
        {
            get { return _slicesY; }
            set
            {
                if (value != _slicesY)
                {
                    if (value < 1) value = 1;
                    _slicesY = value;
                    Rebuild();
                }
            }
        }

        public bool useShapeCurve
        {
            get { return _useShapeCurve; }
            set
            {
                if (value != _useShapeCurve)
                {
                    _useShapeCurve = value;
                    if (_useShapeCurve)
                    {
                        _shape = new AnimationCurve();
                        _shape.AddKey(new Keyframe(0, 0));
                        _shape.AddKey(new Keyframe(1, 0));
                    } else _shape = null;
                    Rebuild();
                }
            }
        }

        public bool compensateCorners
        {
            get { return _compensateCorners; }
            set
            {
                if (value != _compensateCorners)
                {
                    _compensateCorners = value;
                    Rebuild();
                }
            }
        }

        public float shapeExposure
        {
            get { return _shapeExposure; }
            set
            {
                if (spline != null && value != _shapeExposure)
                {
                    _shapeExposure = value;
                    Rebuild();
                }
            }
        }

        public AnimationCurve shape
        {
            get { return _shape; }
            set
            {
                if(_lastShape == null) _lastShape = new AnimationCurve();
                bool keyChange = false;
                if (value.keys.Length != _lastShape.keys.Length) keyChange = true;
                else
                {
                    for (int i = 0; i < value.keys.Length; i++)
                    {
                        if (value.keys[i].inTangent != _lastShape.keys[i].inTangent || value.keys[i].outTangent != _lastShape.keys[i].outTangent || value.keys[i].time != _lastShape.keys[i].time || value.keys[i].value != value.keys[i].value)
                        {
                            keyChange = true;
                            break;
                        }
                    }
                }
                if (keyChange) Rebuild();
                _lastShape.keys = new Keyframe[value.keys.Length];
                value.keys.CopyTo(_lastShape.keys, 0);
                _lastShape.preWrapMode = value.preWrapMode;
                _lastShape.postWrapMode = value.postWrapMode;
                _shape = value;

            }
        }

        protected override string meshName => "Path";
        [SerializeField]
        [HideInInspector]
        private float _magnitude = 0.2f;
        [SerializeField]
        [HideInInspector]
        private float _frequency = 1;
        /*[SerializeField]
        [HideInInspector]
        private Texture2D texture;*/

        [SerializeField]
        [HideInInspector]
        private int _slicesX = 1;
        [SerializeField]
        [HideInInspector]
        private int _slicesY = 1;
        [SerializeField]
        [HideInInspector]
        [Tooltip("This will inflate sample sizes based on the angle between two samples in order to preserve geometry width")]
        private bool _compensateCorners = false;
        [SerializeField]
        [HideInInspector]
        private bool _useShapeCurve = false;
        [SerializeField]
        [HideInInspector]
        private AnimationCurve _shape;
        [SerializeField]
        [HideInInspector]
        private AnimationCurve _lastShape;
        [SerializeField]
        [HideInInspector]
        private float _shapeExposure = 1f;


        protected override void Reset()
        {
            base.Reset();
        }


        protected override void BuildMesh()
        {
           base.BuildMesh();
           GenerateVertices();
           MeshUtility.GeneratePlaneTriangles(ref _tsMesh.triangles, _slicesX, (sampleCount), false);
        }


        void GenerateVertices()
        {
            int vertexCount = (_slicesX + 1) * (sampleCount);
            AllocateMesh(vertexCount, _slicesX * ((sampleCount)-1) * 6);
            int vertexIndex = 0;

            ResetUVDistance();

            bool hasOffset = offset != Vector3.zero;
            for (int i = 0; i < (sampleCount); i++)
            {
                if (_compensateCorners)
                {
                    GetSampleWithAngleCompensation(i, ref evalResult);
                }
                else
                {
                    GetSample(i, ref evalResult);
                }

                Vector3 center = Vector3.zero;
                try
                {
                   center = evalResult.position;
                } catch (System.Exception ex) { Debug.Log(ex.Message + " for i = " + i); return; }
                Vector3 right = evalResult.right;
                float resultSize = GetBaseSize(evalResult);
                if (hasOffset)
                {
                    center += (offset.x * resultSize) * right + (offset.y * resultSize) * evalResult.up + (offset.z * resultSize) * evalResult.forward;
                }
                float fullSize = size * resultSize;
                Vector3 lastVertPos = Vector3.zero;
                Quaternion rot = Quaternion.AngleAxis(rotation, evalResult.forward);
                if (uvMode == UVMode.UniformClamp || uvMode == UVMode.UniformClip) AddUVDistance(i);
                Color vertexColor = GetBaseColor(evalResult) * color;
                for (int n = 0; n < _slicesX + 1; n++)
                {
                    float slicePercent = ((float)n / _slicesX);
                    float shapeEval = 0f;
                    if (_useShapeCurve) shapeEval = _shape.Evaluate(slicePercent);
                    var random = 0f;
                    if(n != slicesX/2 && i != 0 && i != sampleCount-1)
                        random = (Mathf.PerlinNoise(i*_frequency, n*_frequency)-0.5f) * _magnitude;
                    _tsMesh.vertices[vertexIndex] = center + rot * right * (fullSize * 0.5f) - rot * right * (fullSize * slicePercent) + rot * evalResult.up * (shapeEval * _shapeExposure) + Vector3.up*random;
                    CalculateUVs(evalResult.percent, 1f - slicePercent);
                    _tsMesh.uv[vertexIndex] = Vector2.one * 0.5f + (Vector2)(Quaternion.AngleAxis(uvRotation + 180f, Vector3.forward) * (Vector2.one * 0.5f - __uvs));
                    if (_slicesX > 1)
                    {
                        if (n < _slicesX)
                        {
                            float forwardPercent = ((float)(n + 1) / _slicesX);
                            shapeEval = 0f;
                            if (_useShapeCurve) shapeEval = _shape.Evaluate(forwardPercent);
                            Vector3 nextVertPos = center + rot * right * fullSize * 0.5f - rot * right * fullSize * forwardPercent + rot * evalResult.up * shapeEval * _shapeExposure;
                            Vector3 cross1 = -Vector3.Cross(evalResult.forward, nextVertPos - _tsMesh.vertices[vertexIndex]).normalized;

                            if (n > 0)
                            {
                                Vector3 cross2 = -Vector3.Cross(evalResult.forward, _tsMesh.vertices[vertexIndex] - lastVertPos).normalized;
                                _tsMesh.normals[vertexIndex] = Vector3.Slerp(cross1, cross2, 0.5f);
                            } else _tsMesh.normals[vertexIndex] = cross1;
                        }
                        else   _tsMesh.normals[vertexIndex] = -Vector3.Cross(evalResult.forward, _tsMesh.vertices[vertexIndex] - lastVertPos).normalized;
                    }
                    else
                    {
                        _tsMesh.normals[vertexIndex] = evalResult.up;
                        if (rotation != 0f) _tsMesh.normals[vertexIndex] = rot * _tsMesh.normals[vertexIndex];
                    }
                    _tsMesh.colors[vertexIndex] = vertexColor;
                    lastVertPos = _tsMesh.vertices[vertexIndex];
                    vertexIndex++;
                }
            }
        }
    }
}