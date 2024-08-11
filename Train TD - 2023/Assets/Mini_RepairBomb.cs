using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mini_RepairBomb : MonoBehaviour {

    public AnimationCurve alphaCurve;

    public float animSpeed = 1f;
    public float curTime = 0;

    private MeshRenderer _renderer;

    private Material myMat;

    private static readonly int CurrentAlpha = Shader.PropertyToID("_CurrentAlpha");

    // Start is called before the first frame update
    void Start() {
        _renderer = GetComponentInChildren<MeshRenderer>();
        myMat = _renderer.material;
    }

    // Update is called once per frame
    void Update() {
        curTime += animSpeed * Time.deltaTime;
        
        myMat.SetFloat(CurrentAlpha, alphaCurve.Evaluate(curTime));

        if (curTime > 1f) {
            Destroy(gameObject);
        }
    }
}
