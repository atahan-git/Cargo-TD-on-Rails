using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexTrackSegment : MonoBehaviour {

    public float myLength = 40;
    
    public void AttachSegment(HexTrackSegment toAttach) {
            toAttach.transform.SetParent(transform);
            toAttach.transform.position = transform.position + transform.forward * myLength;
            toAttach.transform.rotation = transform.rotation;
            myLength += toAttach.myLength;
    }
    
    public void AttachSwitch(TrackSwitchHex toAttach) {
	    toAttach.transform.SetParent(transform);
	    toAttach.transform.position = transform.position + transform.forward * (myLength + toAttach.trackSwitchLength/2f);
	    toAttach.transform.rotation = transform.rotation;
    }
}
