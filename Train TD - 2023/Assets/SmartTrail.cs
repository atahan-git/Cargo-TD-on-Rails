using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartTrail : MonoBehaviour
{
    public float lifetime = 0.15f;
    public float minVertexDistance = 0.2f;

    private LineRenderer lineRenderer;
    public List<Vector3> points = new List<Vector3>();
    public List<float> pointTimes = new List<float>();

    private IEnemyProjectile _enemyProjectile;
    private Projectile _projectile;
    private Vector3 velocity;
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());

        _enemyProjectile = GetComponentInParent<IEnemyProjectile>();
        _projectile = GetComponentInParent<Projectile>();
        if (_enemyProjectile != null) {
            velocity = _enemyProjectile.GetInitialVelocity();
        }else if (_projectile != null) {
            velocity = _projectile.initialVelocity;
        }
        
        //Debug.Break();
    }

    public bool doTrail = true;
    public void StopTrailing() {
        doTrail = false;
    }

    void FixedUpdate()
    {
        UpdateTrailPositions();
    }

    private void Update() {
        
    }


    void UpdateTrailPositions() {
        var rg = GetComponentInParent<Rigidbody>();


        if (doTrail && rg != null) {
            //Debug.Break();
            if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], rg.position) >= minVertexDistance) {
                points.Insert(0, transform.position);
                pointTimes.Insert(0, Time.time);
            }
        }

        if (railgunMode) {
            railgunCurPos = Vector3.MoveTowards(railgunCurPos, railgunTo, 100 * Time.deltaTime);
            tAdd += Time.deltaTime;
            
            if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], railgunCurPos) >= minVertexDistance) {
                points.Insert(0, railgunCurPos);
                pointTimes.Insert(0, Time.time+tAdd);
            }

            if (Vector3.Distance(railgunCurPos, railgunTo) <= 0) {
                railgunMode = false;
            }
        }
        
        // Remove old points
        if (points.Count > 0) {
            for (int i = points.Count - 1; i >= 0; i--) {
                if (Time.time - pointTimes[i] > lifetime) {
                    points.RemoveAt(i);
                    pointTimes.RemoveAt(i);
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
        railgunCurPos = from;
        railgunTo = to;
        tAdd = 0;

        lifetime = 0.3f;
        //Debug.Break();
        
        points.Insert(0, from);
        pointTimes.Insert(0, Time.time);

        velocity = Vector3.zero;

        /*if (lineRenderer == null) {
            Start();
        }*/
    }

    void UpdateLineRenderer()
    {
        lineRenderer.positionCount = points.Count;

        lineRenderer.SetPositions(points.ToArray());

        if (points.Count > 0)
        {
            //float[] widths = new float[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                points[i] += velocity*Time.deltaTime;
            }
        }
    }
}
