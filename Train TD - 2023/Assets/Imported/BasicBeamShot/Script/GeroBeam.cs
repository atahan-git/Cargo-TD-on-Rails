using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;

public class GeroBeam : MonoBehaviour {
	public GameObject HitEffect;
	private ShotParticleEmitter SHP_Emitter;

	public float MaxLength = 16.0f;
	public float Width = 10.0f;
	private LineRenderer LR;
	private GeroBeamHit HitObj;
	private float RateA;

	private BeamParam BP;
    private Vector3 HitObjSize;
    private GameObject Flash;
    private float FlashSize;

    public LayerMask laserLayerMask;
    // Use this for initialization
    void Awake () {
		BP = GetComponent<BeamParam>();
		LR = this.GetComponent<LineRenderer>();
		HitObj = this.transform.Find("GeroBeamHit").GetComponent<GeroBeamHit>();
        HitObjSize = HitObj.transform.localScale;
        SHP_Emitter = this.transform.Find("ShotParticle_Emitter").GetComponent<ShotParticleEmitter>();
        Flash = this.transform.Find("BeamFlash").gameObject;
        FlashSize = Flash.transform.localScale.x;
        
        Flash.GetComponent<Renderer>().material.SetColor("_Color", BP.BeamColor * 2);

        Width = 0;
        DisableBeam();
    }

    [Button]
    public void ActivateBeam() {
	    gameObject.SetActive(true);
	    BP.bEnd = false;
	    SHP_Emitter.ShotPower = 1;
    }

    public void SetBeamPower( float power) {
	    Width = power;
    }

    [Button]
    public void DisableBeam() {
	    BP.bEnd = true;
    }
	
	void Update () {
        if (BP.bEnd) {
			SHP_Emitter.ShotPower = 0.0f;
			if (Width < 0.01f) {
				BP.bGero = false;
	            gameObject.SetActive(false);
			}
		}else if (!BP.bEnd && !BP.bGero) {
	        if (Width < 1f) {
		        SHP_Emitter.ShotPower = 1;
	        } else {
		        BP.bGero = true;
		        Width = 1;
	        }
        }


        if (BP.bGero || !BP.bEnd) {
	        LR.SetWidth(Width * BP.Scale, Width * BP.Scale);
	        //LR.SetColors(BP.BeamColor, BP.BeamColor);
	        MaxLength = BP.MaxLength;
	        

	        //Collision
	        bool bHitNow = false;

	        if (Width >= 0.01f) {
		        RaycastHit hit;
		        if (Physics.Raycast(transform.position, transform.forward, out hit, MaxLength, laserLayerMask)) {
			        GameObject hitobj = hit.collider.gameObject;
			        LR.SetPosition(1, transform.InverseTransformPoint(hit.point));
			        HitObj.transform.position = hit.point;
			        HitObj.transform.rotation = Quaternion.LookRotation(hit.normal);
			        HitObj.transform.localScale = HitObjSize * Width * BP.Scale * 10.0f;
			        bHitNow = true;
		        }
		        
	        }

	        float ShotFlashScale = FlashSize * Width * 5.0f * BP.Scale;
	        var scaleVec = new Vector3(ShotFlashScale, ShotFlashScale, ShotFlashScale);
	        Flash.GetComponent<ScaleWiggle>().DefScale = scaleVec;
	        HitObj.SetViewPat(bHitNow && !BP.bEnd, scaleVec*2);

	        //this.gameObject.GetComponent<Renderer>().material.SetFloat("_AddTex", Time.frameCount * -0.05f * BP.AnimationSpd * 10);
	        GetComponent<Renderer>().material.mainTextureOffset = new Vector2(BP.AnimationSpd * Time.time,0 );
	        //this.gameObject.GetComponent<Renderer>().material.SetFloat("_BeamLength", NowLength);
	        SHP_Emitter.col = BP.BeamColor * 2;
	        HitObj.col = BP.BeamColor * 2;
        }
	}
}
