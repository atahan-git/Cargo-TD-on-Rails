using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class RandomMeepleCloth : MonoBehaviour {

    public Texture2D[] possibleNoiseTex;

    public float minSaturation = 0.4f;
    public float maxSaturation = 0.8f;
    public float minBrightness = 0.4f;
    public float maxBrightness = 0.8f;
    private static readonly int Texture1 = Shader.PropertyToID("_Texture");
    private static readonly int Color1 = Shader.PropertyToID("_Color1");
    private static readonly int Color2 = Shader.PropertyToID("_Color2");

    public Vector2 tilingRanges = new Vector2(0.8f, 2.5f);
    private static readonly int Tiling = Shader.PropertyToID("_Tiling");
    private static readonly int Offset = Shader.PropertyToID("_Offset");


    public bool forceColors = false;
    [ShowIf("forceColors")]
    public Color forcedColor1 = Color.magenta;
    [ShowIf("forceColors")]
    public Color forcedColor2 = Color.black;

    void Start()
    {
        var meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.SetTexture(Texture1, possibleNoiseTex[Random.Range(0, possibleNoiseTex.Length)]);
        if (forceColors) {
            meshRenderer.material.SetColor(Color1, forcedColor1);
            meshRenderer.material.SetColor(Color2, forcedColor2);
        } else {
            var randomColor1 = Random.ColorHSV(0, 1, minSaturation, maxSaturation, minBrightness, maxBrightness);
            meshRenderer.material.SetColor(Color1, randomColor1);
            
            var randomColor2 = Random.ColorHSV(0, 1, 0, maxSaturation, 0, maxBrightness);
            meshRenderer.material.SetColor(Color2, randomColor2);
        }

        
        
        meshRenderer.material.SetVector(Tiling, new Vector2(Random.Range(tilingRanges.x, tilingRanges.y),Random.Range(tilingRanges.x, tilingRanges.y)));
        meshRenderer.material.SetVector(Offset, new Vector2(Random.Range(0, 100),Random.Range(0, 100)));
    }
}
