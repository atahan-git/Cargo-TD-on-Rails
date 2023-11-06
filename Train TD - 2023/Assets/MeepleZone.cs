using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class MeepleZone : MonoBehaviour {
    public bool isCircle = true;

    [ShowIf("isCircle")]
    public float radius = 0.5f;
    
    [HideIf("isCircle")]
    public float a = 1f;
    [HideIf("isCircle")]
    public float b = 2f;

    public GameObject meeplePrefab;
    public Vector2 meepleCount = new Vector2(5, 10);

    // Start is called before the first frame update
    void Start() {
        var count = Random.Range(meepleCount.x, meepleCount.y); 
        for (int i = 0; i < count; i++) {
            Instantiate(meeplePrefab, transform).GetComponent<Meeple>().SetUp(this);
        }
    }


    public Vector3 GetPointInZone() {
        if (isCircle) {
            var position = Random.insideUnitCircle*radius;
            return new Vector3(position.x, 0, position.y);
        } else {
            return new Vector3(Random.Range(-a, a)/2, 0, Random.Range(-b, b)/2);
        }
    }
    public const float groundLevel = 0.33f;
    private void OnDrawGizmos() {
        
        Gizmos.color = Color.yellow;
        if (isCircle) {
            Gizmos.DrawWireSphere(transform.position + Vector3.up*groundLevel, radius);
        } else {
            Gizmos.DrawWireCube(transform.position+ Vector3.up*groundLevel, new Vector3(a,1,b));
        }
    }
}
