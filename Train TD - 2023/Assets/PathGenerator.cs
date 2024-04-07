using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class PathGenerator : MonoBehaviour {

    [Serializable]
    public class TrainPath {
        public Vector3[] points;
        public Bounds bounds;
        public float length;
        public float stepLength;
        public int endPoint;
        public int rotateStopPoint;
        public int endRotateStartPoint;
        public bool endPath = false;
        public bool addImprintNoise = true;
        public bool debugDrawGizmo = true;
        public int trackObjectsId = -1;
        public string pathRewardUniqueName = "";
        public bool pathRewardMerge = false;
    }


    public float stepLength = 0.2f;
    public float turnAngle = 0.5f;
    public float pathWidth = 20;
    public float circularPathWidth = 5;
    public float maxAngle = 45;
    public float circularMaxAngle = 25;
    public Vector2Int baseRandomInterval = new Vector2Int(5, 10);
    public Vector2Int goBackRandomInterval = new Vector2Int(4, 6);
    public int minGoBackInterval =2;
    public float turnChance = 0.33f;
    public TrainPath MakeTrainPath(Vector3 startPoint, Vector3 startDirection, Vector3 direction, float length ,bool immediateTurn = false) {
        float curAngle = Vector3.Angle(startDirection,direction);
        float rotAngle = 0;
        if (Vector3.Cross(startDirection, direction).y > 0) {
            curAngle = -curAngle;
        }

        bool doClampAngle = !(Mathf.Abs(curAngle) > maxAngle);

        if (immediateTurn) {
            rotAngle = turnAngle;
            if (Vector3.Cross(startDirection, direction).y < 0) { // immediately start turning in the other direction
                rotAngle = -rotAngle;
            }
        }
        
        var trackPieceCount = Mathf.CeilToInt((length / stepLength)/5)*5 + 1;
        var path = new Vector3[trackPieceCount];
        length = (path.Length - 1) * stepLength;

        var minEdge = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var maxEdge = new Vector3(float.MinValue,10,float.MinValue);// give volume to the bounds

        var interval = Mathf.CeilToInt(30/stepLength);
        path[0] = startPoint;
        for (int i = 1; i < path.Length; i++) {
            
            if (doClampAngle) {
                if (rotAngle > 0) {
                    curAngle += rotAngle;
                    if (curAngle > maxAngle) {
                        interval = interval % 5; // turn away as fast as possible
                    }
                } else {
                    curAngle += rotAngle;
                    if (curAngle < -maxAngle) {
                        interval = interval % 5; // turn away as fast as possible
                    }
                }
            } else {
                curAngle += rotAngle;
                if (Mathf.Abs(curAngle) < maxAngle)
                    doClampAngle = true;
            }

            var curDirection = Quaternion.AngleAxis(curAngle, Vector3.up) * direction;
            
            path[i] = path[i - 1] + curDirection.normalized * stepLength;

            if (interval < minGoBackInterval*5) {
                if (DistanceToLine(startPoint, direction, path[i]) > pathWidth) {
                    if (Vector3.Cross(path[i]-startPoint, direction).y < 0) {
                        rotAngle = -turnAngle;
                    } else {
                        rotAngle = turnAngle;
                    }

                    interval = Random.Range(goBackRandomInterval.x, goBackRandomInterval.y);
                    interval *= 5;
                }
            }

            interval -= 1;
            if (interval <= 0) {
                var randDir = Random.value;
                if (randDir < turnChance) {
                    rotAngle = turnAngle;
                }else if (randDir < turnChance*2) {
                    rotAngle = -turnAngle;
                } else {
                    rotAngle = 0;
                }


                interval = Random.Range(baseRandomInterval.x, baseRandomInterval.y);
                interval *= 5;
            }

            maxEdge.x = Mathf.Max(maxEdge.x, path[i].x);
            maxEdge.y = Mathf.Max(maxEdge.y, path[i].y);
            maxEdge.z = Mathf.Max(maxEdge.z, path[i].z);
            
            minEdge.x = Mathf.Min(minEdge.x, path[i].x);
            minEdge.y = Mathf.Min(minEdge.y, path[i].y);
            minEdge.z = Mathf.Min(minEdge.z, path[i].z);
        }
        
        
        var trainPath = new TrainPath();
        trainPath.points = path;
        trainPath.bounds = new Bounds();
        trainPath.bounds.SetMinMax(minEdge, maxEdge);
        trainPath.length = length;
        trainPath.stepLength = stepLength;
        return trainPath;
    }

    public const float stationStraightDistance = 100;
    public TrainPath MakeStationAtEndPath(Vector3 startPoint, Vector3 startDirection, Vector3 direction, float length ,bool immediateTurn = false) {
        Assert.IsTrue(length > 0);
        
        var curvyPath = MakeTrainPath(startPoint, startDirection, direction, length, immediateTurn);
        startPoint = curvyPath.points[^1];
        startDirection = GetDirectionVectorOnTheLine(curvyPath, curvyPath.length);

        var straightPath = MakeStraightPath(startPoint, startDirection, stationStraightDistance);

        var combinedPath = new TrainPath();
        combinedPath.points = new Vector3[curvyPath.points.Length + straightPath.points.Length -1];
        
        curvyPath.points.CopyTo(combinedPath.points,0);
        straightPath.points.CopyTo(combinedPath.points,curvyPath.points.Length -1);
        
        combinedPath.bounds = new Bounds();
        combinedPath.bounds.Encapsulate(curvyPath.bounds);
        combinedPath.bounds.Encapsulate(straightPath.bounds);

        combinedPath.length = curvyPath.length + straightPath.length;
        combinedPath.stepLength = stepLength;

        combinedPath.endPath = true;

        return combinedPath;
    }
    
    
    public TrainPath MakeStationAtBeginningPath(Vector3 startPoint, Vector3 startDirection, Vector3 direction, float length, bool immediateTurn = false) {
        Assert.IsTrue(length > 0);
        
        var straightPath = MakeStraightPath(startPoint, startDirection, stationStraightDistance);
        startPoint = straightPath.points[^1];
        startDirection = GetDirectionVectorOnTheLine(straightPath, straightPath.length);
        
        var curvyPath = MakeTrainPath(startPoint, startDirection, direction, length, immediateTurn);


        var combinedPath = new TrainPath();
        combinedPath.points = new Vector3[curvyPath.points.Length + straightPath.points.Length -1];
        
        straightPath.points.CopyTo(combinedPath.points,0);
        curvyPath.points.CopyTo(combinedPath.points,straightPath.points.Length-1);
        
        combinedPath.bounds = new Bounds();
        combinedPath.bounds.Encapsulate(curvyPath.bounds);
        combinedPath.bounds.Encapsulate(straightPath.bounds);

        combinedPath.length = curvyPath.length + straightPath.length;
        combinedPath.stepLength = stepLength;

        return combinedPath;
    }

    public TrainPath MakeStraightPath(Vector3 startPoint, Vector3 direction, float length) {
        var trackPieceCount = Mathf.CeilToInt((length / stepLength)/5)*5 + 1;
        var path = new Vector3[trackPieceCount];
        length = (path.Length - 1) * stepLength;

        var minEdge = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var maxEdge = new Vector3(float.MinValue,10,float.MinValue);// give volume to the bounds

        path[0] = startPoint;
        for (int i = 1; i < path.Length; i++) {
            path[i] = path[i - 1] + direction.normalized * stepLength;

            maxEdge.x = Mathf.Max(maxEdge.x, path[i].x);
            maxEdge.y = Mathf.Max(maxEdge.y, path[i].y);
            maxEdge.z = Mathf.Max(maxEdge.z, path[i].z);
            
            minEdge.x = Mathf.Min(minEdge.x, path[i].x);
            minEdge.y = Mathf.Min(minEdge.y, path[i].y);
            minEdge.z = Mathf.Min(minEdge.z, path[i].z);
        }
        
        
        var trainPath = new TrainPath();
        trainPath.points = path;
        trainPath.bounds = new Bounds();
        trainPath.bounds.SetMinMax(minEdge, maxEdge);
        trainPath.length = length;
        trainPath.stepLength = stepLength;
        return trainPath;
    }


    private Vector3 cityDimensions = new Vector3(30, 0, 30); //  dimensions of cube
    private Vector3 cityOffset = new Vector3(0, 0, -8);
    public TrainPath MakeCityStampPath(Vector3 position, Quaternion direction) {
        var path = new List<Vector3>();

        cityDimensions = new Vector3(30, 0, 30);
        
        var safeDist = GetComponent<TerrainGenerator>().playZoneWidth*2f;
        var effectiveDimensions = cityDimensions + (Vector3.one * (0.5f - safeDist));
        var smallestDimension = Mathf.Min(effectiveDimensions.x, effectiveDimensions.z);
        var rectCount = Mathf.CeilToInt(smallestDimension / safeDist);
        
        var minEdge = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var maxEdge = new Vector3(float.MinValue,10,float.MinValue);// give volume to the bounds

        var pathLength = 0f;
        
        var radianTheta = QuaternionToRadian(direction);
        var l = stepLength;

        var effectiveOffset = position + RotatePoint(cityOffset, radianTheta);

        //print(effectiveDimensions);
        for (int r = 0; r < rectCount; r++) {
            var a = effectiveDimensions.x - (r*safeDist);
            var b = effectiveDimensions.z - (r*safeDist);

             var squareCenterChangeOffset = -new Vector3(a,0,b) / 2;

             pathLength += (a+b)*2;

            int pointsOnSideA = Mathf.CeilToInt(a / l);
            int pointsOnSideB = Mathf.CeilToInt(b / l);
            var a_l = a / pointsOnSideA;
            var b_l = b / pointsOnSideB;

            // Calculate points on top edge
            for (int i = 0; i < pointsOnSideA; i++) {
                var x = i * a_l;
                var y = 0;
                var pointOnPerimeter = RotatePoint(new Vector3(x, 0, y) + squareCenterChangeOffset, radianTheta);
                AddPointAndUpdateBounds(effectiveOffset + pointOnPerimeter, path, minEdge,maxEdge);
            }

            // Calculate points on right edge
            for (int i = 0; i < pointsOnSideB; i++) {
                var x = a;
                var y = i * b_l;
                var pointOnPerimeter = RotatePoint(new Vector3(x, 0, y) + squareCenterChangeOffset, radianTheta);
                AddPointAndUpdateBounds(effectiveOffset + pointOnPerimeter, path, minEdge,maxEdge);
            }

            // Calculate points on bottom edge
            for (int i = 0; i < pointsOnSideA; i++) {
                var x = a - i * a_l;
                var y = b;
                var pointOnPerimeter = RotatePoint(new Vector3(x, 0, y) + squareCenterChangeOffset, radianTheta);
                AddPointAndUpdateBounds(effectiveOffset + pointOnPerimeter, path, minEdge,maxEdge);
            }

            // Calculate points on left edge
            for (int i = 0; i < pointsOnSideB; i++) {
                var x = 0;
                var y = b - i * b_l;
                var pointOnPerimeter = RotatePoint(new Vector3(x, 0, y) + squareCenterChangeOffset, radianTheta);
                AddPointAndUpdateBounds(effectiveOffset + pointOnPerimeter, path, minEdge,maxEdge);
            }
        }
        
        
        var trainPath = new TrainPath();
        trainPath.points = path.ToArray();
        trainPath.bounds = new Bounds();
        trainPath.bounds.SetMinMax(minEdge, maxEdge);
        trainPath.length = pathLength;
        trainPath.stepLength = trainPath.length/(path.Count-1);
        trainPath.addImprintNoise = false;
        return trainPath;
    }

    Vector3 RotatePoint(Vector3 point, float radian_theta) {
        var x = point.x;
        var y = point.z;
        var rotated_x = x * Mathf.Cos(radian_theta) - y * Mathf.Sin(radian_theta);
        var rotated_y = x * Mathf.Sin(radian_theta) + y * Mathf.Cos(radian_theta);
        return new Vector3(rotated_x, 0, rotated_y);
    }

    float QuaternionToRadian(Quaternion quaternion)
    {
        // Convert quaternion to angle-axis representation
        float angle;
        Vector3 axis;
        quaternion.ToAngleAxis(out angle, out axis);

        // Unity uses left-handed coordinate system, so we negate the z-component of the axis
        axis.z = -axis.z;

        // Convert angle to radians
        float radianTheta = angle * Mathf.Deg2Rad;

        return radianTheta;
    }

    void AddPointAndUpdateBounds(Vector3 point, List<Vector3> path, Vector3 minEdge, Vector3 maxEdge) {
        path.Add(point);

        maxEdge.x = Mathf.Max(maxEdge.x, path[^1].x);
        maxEdge.y = Mathf.Max(maxEdge.y, path[^1].y);
        maxEdge.z = Mathf.Max(maxEdge.z, path[^1].z);
            
        minEdge.x = Mathf.Min(minEdge.x, path[^1].x);
        minEdge.y = Mathf.Min(minEdge.y, path[^1].y);
        minEdge.z = Mathf.Min(minEdge.z, path[^1].z);
    }


    public TrainPath MakeCirclePath(Vector3 center) {
        float curAngle = 0;
        float rotAngle = 0;
        var direction = Vector3.forward;

        var path = new List<Vector3>();

        var minEdge = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var maxEdge = new Vector3(float.MinValue,10,float.MinValue);// give volume to the bounds

        var interval = Mathf.CeilToInt(30/stepLength);
        path.Add( Vector3.zero);
        var length = 0f;

        var isStartingOut = true;
        var distance = 0f;
        var stitchDist = 25f;

        int endPoint;
        int rotateStopPoint;
        int endRotateStartPoint;

        var i = 1;
        // make most of the circle
        while (distance > stitchDist || isStartingOut) {
            var vec1 = Vector3.zero - center;
            var vec2 = path[^1] - center;
            var currentCircleAngle = Vector3.Angle(vec1, vec2);
            if (Vector3.Cross(vec1, vec2).y > 0) {
                currentCircleAngle = 180 + (180 - currentCircleAngle);
            }
            var adjustedAngle = -currentCircleAngle;
            if (rotAngle > 0) {
                if (curAngle <= adjustedAngle + circularMaxAngle) {
                    curAngle += rotAngle;
                }
            } else {
                if (curAngle >= adjustedAngle - circularMaxAngle) {
                    curAngle += rotAngle;
                }
            }
            
            if (currentCircleAngle > 90)
                isStartingOut = false;

            stitchDist = 50-2f*(360 - currentCircleAngle) + (Mathf.Abs(adjustedAngle-curAngle))*2f;
                
            distance = Vector3.Distance(path[^1], path[0]);

            var curDirection = Quaternion.AngleAxis(curAngle, Vector3.up) * direction;
            
            path.Add(path[^1] + curDirection * stepLength);
            length += stepLength;
            

            if (interval < minGoBackInterval) {
                var dist = CircularDistanceToLine(center, path[^1]);
                //print($"dist: {dist}, angle: {currentCircleAngle}");
                if (Mathf.Abs(dist) > circularPathWidth) {
                    if (dist < 0) {
                        rotAngle = turnAngle;
                    } else {
                        rotAngle = -turnAngle;
                    }

                    interval = Random.Range(goBackRandomInterval.x, goBackRandomInterval.y);
                }
            }

            interval -= 1;
            if (interval <= 0) {
                var randDir = Random.value;
                if (randDir < turnChance) {
                    rotAngle = turnAngle;
                }else if (randDir < turnChance*2) {
                    rotAngle = -turnAngle;
                } else {
                    rotAngle = 0;
                }


                interval = Random.Range(baseRandomInterval.x, baseRandomInterval.y);
            }

            maxEdge.x = Mathf.Max(maxEdge.x, path[^1].x);
            maxEdge.y = Mathf.Max(maxEdge.y, path[^1].y);
            maxEdge.z = Mathf.Max(maxEdge.z, path[^1].z);
            
            minEdge.x = Mathf.Min(minEdge.x, path[^1].x);
            minEdge.y = Mathf.Min(minEdge.y, path[^1].y);
            minEdge.z = Mathf.Min(minEdge.z, path[^1].z);

            i += 1;
            if(i > 10000)
                break;
        }

        endPoint = i;
        
        // stitch ends
        interval = 100;
        {
            var curDirection = Quaternion.AngleAxis(curAngle, Vector3.up) * direction;
            var intersectNegative = FindIntersectionPoint(path[^1], curDirection, Vector3.zero, Vector3.forward).z < 0;
            var anglePositive = Vector3.Cross(curDirection, Vector3.forward).y > 0;
            
            if (anglePositive != intersectNegative) {
                rotAngle = -turnAngle;
            } else {
                rotAngle = turnAngle;
            }
        }
        
        // dont let the line be parellel
        while (Mathf.Abs(curAngle - 360) < 1f) {
            curAngle += rotAngle;
            var curDirection = Quaternion.AngleAxis(curAngle, Vector3.up) * direction;
            path.Add(path[^1] + curDirection * stepLength);
        }

        var debugShowTime = 20f;

        
        
        // first arc
        var backtrackPoint = Vector3.zero;
        var backtrackAngle = 0f;
        while (true) {
            curAngle += rotAngle;

            var curDirection = Quaternion.AngleAxis(curAngle, Vector3.up) * direction;
            path.Add(path[^1] + curDirection * stepLength);


            backtrackPoint = Vector3.zero;
            backtrackAngle = 0;
            if(path[^1].x > 0) {
                while(AbsoluteAngleDistance(curAngle, backtrackAngle) > turnAngle){
                    backtrackAngle -= turnAngle;
                    var backTrackDirection = Quaternion.AngleAxis(backtrackAngle, Vector3.up) * Vector3.forward;
                    backtrackPoint = backtrackPoint - backTrackDirection * stepLength;
                    //Debug.DrawLine(backtrackPoint, backtrackPoint+Vector3.left, Color.red, debugShowTime);
                }
            }else
            {
                while(AbsoluteAngleDistance(curAngle, backtrackAngle) > turnAngle){
                    backtrackAngle += turnAngle;
                    var backTrackDirection = Quaternion.AngleAxis(backtrackAngle, Vector3.up) * Vector3.forward;
                    backtrackPoint = backtrackPoint - backTrackDirection * stepLength;
                    //Debug.DrawLine(backtrackPoint, backtrackPoint+Vector3.up, Color.blue, debugShowTime);
                }

            }

            if (DistanceToLine(path[^1], curDirection, backtrackPoint) < stepLength) {
                //Debug.DrawLine(backtrackPoint, backtrackPoint+Vector3.up*20, Color.blue, debugShowTime);
                break;
            }


            i += 1;
            if (i > 10000)
                break;
        }

        rotateStopPoint = i;

        // straight section
        while (true) {
            var curDirection = Quaternion.AngleAxis(curAngle, Vector3.up) * direction;
            path.Add(path[^1] + curDirection * stepLength);
            
            if (Vector3.Distance(path[^1], backtrackPoint) < stepLength) {
                //path.Add(path[^1] + curDirection * stepLength);
                break;
            }

            i += 1;
            if (i > 10000)
                break;
        }
        endRotateStartPoint = i-1;

        backtrackPoint = path[^1];
        // secondArc
        while (AbsoluteAngleDistance(backtrackAngle, 0) > turnAngle) {
            backtrackAngle = Mathf.MoveTowards(backtrackAngle, 0, turnAngle);
            var backTrackDirection = Quaternion.AngleAxis(backtrackAngle, Vector3.up) * Vector3.forward;
            backtrackPoint = backtrackPoint + backTrackDirection * stepLength;
            path.Add(backtrackPoint);
        }

        {
            var backTrackDirection = Quaternion.AngleAxis(backtrackAngle, Vector3.up) * Vector3.forward;
            backtrackPoint = backtrackPoint + backTrackDirection * stepLength;
            path.Add(backtrackPoint);
        }

        
        // smooth out the connection point


        var smoothRangeBack = 4;
        var smoothRangeForward = 4;
        var startSmooth = path[^smoothRangeBack];
        var endSmooth = path[smoothRangeForward];

        var n = 0;
        var total = smoothRangeForward + smoothRangeBack;
        for (int j = smoothRangeBack; j > 0 ; j--) {
            //Debug.DrawLine(path[^smoothRangeBack],  Vector3.Lerp(startSmooth, endSmooth, ((float)n)/total) + Vector3.up, Color.red, 5f);
            path[^j] = Vector3.Lerp(startSmooth, endSmooth, ((float)n)/total);
            n++;
        }

        n--;
        
        for (int j = 0; j < smoothRangeForward; j++) {
            //Debug.DrawLine(path[smoothRangeForward],  Vector3.Lerp(startSmooth, endSmooth, ((float)n)/total) + Vector3.down, Color.green, 5f);
            path[j] = Vector3.Lerp(startSmooth, endSmooth, ((float)n)/total);
            n++;
        }


        var trainPath = new TrainPath();
        trainPath.points = path.ToArray();
        trainPath.bounds = new Bounds();
        trainPath.bounds.SetMinMax(minEdge, maxEdge);
        trainPath.length = length;
        trainPath.stepLength = stepLength;
        trainPath.endPoint = endPoint;
        trainPath.rotateStopPoint = rotateStopPoint;
        trainPath.endRotateStartPoint = endRotateStartPoint;
        return trainPath;
    }

    static float AbsoluteAngleDistance(float angle1, float angle2)
    {
        // Normalize angles to be within [0, 360) range
        angle1 = NormalizeAngle(angle1);
        angle2 = NormalizeAngle(angle2);

        // Calculate the absolute difference between the angles
        float absoluteDifference = Mathf.Abs(angle1 - angle2);

        // Choose the smaller of the two possible distances (clockwise or counterclockwise)
        float distance = Mathf.Min(absoluteDifference, 360 - absoluteDifference);

        return distance;
    }

    static float NormalizeAngle(float angle)
    {
        // Normalize the angle to be within [0, 360) range
        while (angle < 0)
        {
            angle += 360;
        }

        while (angle >= 360)
        {
            angle -= 360;
        }

        return angle;
    }

    public static Vector3 FindIntersectionPoint(Vector3 p1, Vector3 v1, Vector3 p2, Vector3 v2) {
        
        float crossProduct = v1.x * v2.z - v1.z * v2.x;

        // Set up the system of equations
        float t1 = ((p2.x - p1.x) * v2.z - (p2.z - p1.z) * v2.x) / crossProduct;
        float t2 = ((p2.x - p1.x) * v1.z - (p2.z - p1.z) * v1.x) / crossProduct;

        // Calculate the intersection point
        Vector3 intersectionPoint = new Vector3(p1.x + t1 * v1.x, 0, p1.z + t1 * v1.z);

        return intersectionPoint;
    }

    public static float DistanceToLine(Vector3 startPoint, Vector3 direction, Vector3 point) {
        return Vector3.Cross((startPoint - point), direction).magnitude / direction.magnitude;
    } 
    
    public static float CircularDistanceToLine(Vector3 centerPoint, Vector3 point) {
        var radius = centerPoint.magnitude;
        return Vector3.Distance(centerPoint, point) -radius;
    } 
    
    public static Vector3 MoveLine(TrainPath trainPath, float distance) {
        var center = Vector3.zero;
        var point = GetPointOnLine(trainPath, distance)-center;
        var path = trainPath.points;
        /*var rot = GetDirectionOnTheLine(distance);
        rot = Quaternion.Euler(rot.eulerAngles.x, -rot.eulerAngles.y, rot.eulerAngles.z);*/

        for (int i = 0; i < path.Length; i++) {
            var newPoint = path[i];
            newPoint -= point;
            /*var v = newPoint - center; //the relative vector from P2 to P1.
            v = rot * v; //rotatate
            newPoint = center + v; //bring back to world space*/
            path[i] = newPoint;
        }

        trainPath.bounds.center -= point;
        return point;
    }
    public static Vector3 GetPointOnLine(TrainPath trainPath, float distance) {
        var path = trainPath.points;
        var target = distance / trainPath.stepLength;
        var floorIndex = Mathf.FloorToInt(target);
        var ceilIndex = Mathf.CeilToInt(target);
        //print($"{target}, {floorIndex}, {ceilIndex}, {path.Length}");
        
        if (ceilIndex <= 0) {
            return path[0];
        }

        if (ceilIndex > path.Length-1) {
            return path[^1];
        }
        

        var point = Vector3.Lerp(path[floorIndex], path[ceilIndex], target%1);

        //Debug.DrawLine(point,point+ Vector3.up*5, Color.red, 1f);
        
        return point;
    }
    
    public static Quaternion GetDirectionOnTheLine(TrainPath trainPath, float distance) {
        var path = trainPath.points;
        var target = distance / trainPath.stepLength;

        if (target <= 0) {
            target = trainPath.stepLength/2f;
        }

        if (target >= path.Length-1) {
            target = path.Length-1 -  (trainPath.stepLength/ 2f);
        }

        var direction = path[Mathf.FloorToInt(target) + 1] - path[Mathf.FloorToInt(target)];

        //Debug.DrawLine(path[Mathf.FloorToInt(target)]+Vector3.up*5f,path[Mathf.FloorToInt(target)]+direction.normalized*5 + Vector3.up*5f, Color.blue, 1f);
        if (direction.magnitude == 0) {
            print($"something is wrong {trainPath}, {distance}, {trainPath.length}, {trainPath.points.Length}");
        }
        return Quaternion.LookRotation(direction);
    }
    
    public static Vector3 GetDirectionVectorOnTheLine(TrainPath trainPath, float distance) {
        var path = trainPath.points;
        var target = distance / trainPath.stepLength;

        if (target <= 0) {
            target = trainPath.stepLength/2f;
        }

        if (target >= path.Length-1) {
            target = path.Length-1 -  (trainPath.stepLength/ 2f);
        }

        var direction = path[Mathf.FloorToInt(target) + 1] - path[Mathf.FloorToInt(target)];

        //Debug.DrawLine(path[Mathf.FloorToInt(target)],path[Mathf.FloorToInt(target)]+direction.normalized*5, Color.blue, 1f);
        return direction.normalized;
    }
}
