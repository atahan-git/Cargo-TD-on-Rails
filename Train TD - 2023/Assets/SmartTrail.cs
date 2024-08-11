using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class SmartTrail : MonoBehaviour
{
    public float lifetime = 0.15f;
    public float minVertexDistance = 0.2f;

    private LineRenderer lineRenderer;
    public CustomArray<Vector3> points = new CustomArray<Vector3>(16);
    public CustomArray<Vector3> worldPoints = new CustomArray<Vector3>(16);
    public CustomArray<float> pointTimes = new CustomArray<float>(16);

    void Start()
    {
        Reset();
    }

    public bool doTrail = true;
    public void StopTrailing() {
        doTrail = false;
        /*if (lineRenderer != null) {
            points.InsertAtZero(transform.position);
            pointTimes.InsertAtZero(Time.time);
            UpdateLineRenderer();
        }*/
    }

    private Transform posParent => ProjectileProvider.s.transform;
    public void Reset() {
        points.Count = 0;
        worldPoints.Count = 0;
        pointTimes.Count = 0;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        doTrail = true;
    }

    private void LateUpdate() {
        UpdateTrailPositions();
    }


    void UpdateTrailPositions() {
        if (doTrail) {
            //Debug.Break();
            if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], transform.position) >= minVertexDistance) {
                points.InsertAtZero(posParent.InverseTransformPoint(transform.position));
                worldPoints.InsertAtZero(transform.position);
                pointTimes.InsertAtZero( Time.time);
            }
        }

        if (railgunMode) {
            railgunCurPos = Vector3.MoveTowards(railgunCurPos, railgunTo, 100 * Time.deltaTime);
            tAdd += Time.deltaTime;
            
            if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], railgunCurPos) >= minVertexDistance) {
                points.InsertAtZero(posParent.InverseTransformPoint(railgunCurPos));
                worldPoints.InsertAtZero(railgunCurPos);
                pointTimes.InsertAtZero( Time.time+tAdd);
            }

            if (Vector3.Distance(railgunCurPos, railgunTo) <= 0) {
                railgunMode = false;
            }
        }
        
        // Remove old points
        if (points.Count > 0) {
            for (int i = points.Count - 1; i >= 0; i--) {
                if (Time.time - pointTimes[i] > lifetime) {
                    points.RemoveAtEnd();
                    worldPoints.RemoveAtEnd();
                    pointTimes.RemoveAtEnd();
                }
            }
        }

        UpdateLineRenderer();
    }


    public bool railgunMode = false;
    public Vector3 railgunCurPos;
    public Vector3 railgunTo;
    public float tAdd;
    public void RailgunOntoPoint(Vector3 from, Vector3 to) {
        railgunMode = true;
        doTrail = false;
        railgunCurPos = posParent.InverseTransformPoint(from);
        railgunTo = posParent.InverseTransformPoint(to);
        tAdd = 0;

        lifetime = 0.3f;
        //Debug.Break();
        
        points.InsertAtZero(railgunCurPos);
        worldPoints.InsertAtZero(from);
        pointTimes.InsertAtZero(Time.time);

        /*if (lineRenderer == null) {
            Start();
        }*/
    }

    void UpdateLineRenderer()
    {

        for (int i = 0; i < points.Count; i++) {
            worldPoints[i] = posParent.TransformPoint(points[i]);
        }
        
        lineRenderer.positionCount = worldPoints.Count;
        lineRenderer.SetPositions(worldPoints.ToArray());

        /*if (doTrail && points.Count > 0 && !railgunMode)
        {
            //float[] widths = new float[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                points[i] += -Vector3.forward*Time.deltaTime*5;
            }
        }*/

        /*if ( points.Count > 0 && !railgunMode) {

            var localizedVectorAdd = Train.s.GetTrainForward() * LevelReferences.s.speed*Time.deltaTime;
            
            for (int i = 0; i < points.Count; i++)
            {
                points[i] += localizedVectorAdd;
            }
        }*/
    }
    
    
    [Serializable]
    public class CustomArray<T>
    {
        [ShowInInspector]
        private T[] elements;
        public int Count;

        public CustomArray(int length) {
            elements = new T[length];
            Count = 0;
        }

        public void InsertAtZero(T element)
        {
            if (Count == elements.Length)
            {
                ResizeArray(elements.Length * 2);
            }
            
            for (int i = Count; i > 0; i--)
            {
                elements[i] = elements[i - 1];
            }

            elements[0] = element;
            Count++;
        }

        public void RemoveAtEnd()
        {
            if (Count > 0)
            {
                Count--;
            }
        }
        
        private void ResizeArray(int newSize)
        {
            T[] newArray = new T[newSize];
            elements.CopyTo(newArray, 0);
            elements = newArray;
        }

        public T[] ToArray() {
            return elements;
        }
        
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new System.IndexOutOfRangeException("Index out of range");
                }
                return elements[index];
            }
            set
            {
                if (index < 0 || index >= Count)
                {
                    throw new System.IndexOutOfRangeException("Index out of range");
                }
                elements[index] = value;
            }
        }
    }
}
