using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

//It is common to create a class to contain all of your
//extension methods. This class must be static.
public static class ExtensionMethods {

    public static void ResetTransformation(this Transform trans) {
        trans.localPosition = Vector3.zero;
        trans.localRotation = Quaternion.identity;
        trans.localScale = new Vector3(1, 1, 1);
    }

    public static Vector3 vector3(this Vector2 v2, float z) {
        return new Vector3(v2.x, v2.y, z);
    }

    public static Vector2 vector2(this Vector3 v3) {
        return new Vector2(v3.x, v3.y);
    }


    public static void DeleteAllChildren(this Transform transform, bool skipLast = false) {
        int childs = transform.childCount;
        int minus = 1;
        if (skipLast)
            minus += 1;
        for (int i = childs - minus; i >= 0; i--) {
            GameObject.Destroy(transform.GetChild(i).gameObject);
        }
    }
    
    public static void DeleteAllChildrenEditor(this Transform transform, bool skipLast = false) {
        int childs = transform.childCount;
        int minus = 1;
        if (skipLast)
            minus += 1;
        for (int i = childs - minus; i >= 0; i--) {
            GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }


    public static void InsertWithNullFill<T>(this List<T> ls, int index, T item) where T : class {
        while (!(index < ls.Count)) {
            ls.Add(null);
        }

        ls.Insert(index, item);
    }
    
    
    public static List<T> Shuffle<T>(List<T> list) {
        return list.OrderBy(x => Random.value).ToList();
    }

    public static float Remap(this float value, float from1, float to1, float from2, float to2) {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
    
    
    public static void SetLeft(this RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }
 
    public static void SetRight(this RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }
 
    public static void SetTop(this RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }
 
    public static void SetBottom(this RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }
    
    
    public static string GetGameObjectPath(this GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }
    
    public static Bounds GetCombinedBoundingBoxOfChildren(this Transform root)
    {
        var colliders = root.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            throw new ArgumentException("The supplied transform " + root.name + " does not have any children with colliders");
        }
 
        Bounds totalBBox = colliders[0].bounds;
        foreach (var collider in colliders)
        {
            totalBBox.Encapsulate(collider.bounds);
        }
        return totalBBox;
    }
    
    public static Vector3 RandomPointInsideBounds(this Bounds bounds)
    {
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        float z = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(x, y, z);
    }
    
    public static Quaternion QuaterionSmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time) {
        if (Time.deltaTime < Mathf.Epsilon) return rot;
        // account for double-cover
        var Dot = Quaternion.Dot(rot, target);
        var Multi = Dot > 0f ? 1f : -1f;
        target.x *= Multi;
        target.y *= Multi;
        target.z *= Multi;
        target.w *= Multi;
        // smooth damp (nlerp approx)
        var Result = new Vector4(
            Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
            Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
            Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
            Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
        ).normalized;
		
        // ensure deriv is tangent
        var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), Result);
        deriv.x -= derivError.x;
        deriv.y -= derivError.y;
        deriv.z -= derivError.z;
        deriv.w -= derivError.w;		
		
        return new Quaternion(Result.x, Result.y, Result.z, Result.w);
    }

}