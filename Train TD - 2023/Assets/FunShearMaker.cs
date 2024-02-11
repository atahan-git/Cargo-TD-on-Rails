using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunShearMaker : MonoBehaviour {

    public GameObject prefab;

    [Range(-40,40)]
    public float angle;

    
    void OnValidate()
    {
        transform.DeleteAllChildrenEditor();
        var obj = Instantiate(prefab,transform);

        obj.transform.localEulerAngles = new Vector3(angle, 0, 0);
        
        ShearWithTransforms.ResizeObject(obj, 6.318f, 6.562802f);
    }
}
