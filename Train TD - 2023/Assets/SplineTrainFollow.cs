using System;
using System.Collections;
using System.Collections.Generic;
using Dreamteck.Splines;
using Dreamteck.Splines.Examples;
using UnityEngine;

public class SplineTrainFollow : MonoBehaviour
{
    // Start is called before the first frame update
    void Start() {
        Train.s.onTrainCartsChanged.AddListener(OnTrainChanged);
    }

    private SplineFollower mainFollower;

    public Transform[] centralWagons;

    void OnTrainChanged() {
        var trainEngine = Train.s.carts[0].GetComponent<SplineTrainEngine>();
        if (trainEngine == null) {
            var engineCart =Train.s.carts[0].gameObject;
            engineCart.AddComponent<SplineWagon>().isEngine = true;
            trainEngine = engineCart.AddComponent<SplineTrainEngine>();
            mainFollower = engineCart.AddComponent<SplineFollower>();
            mainFollower.followSpeed = 0;
        }

        mainFollower.spline = SplinePathMaster.s.currentSegment.splineComputer;


        for (int i = 0; i < Train.s.carts.Count; i++) {
            var wagon = Train.s.carts[i].GetComponent<SplineWagon>();
            if (wagon == null) {
                if (i != 0) {
                    var secondaryFollower = Train.s.carts[i].gameObject.AddComponent<SplinePositioner>();
                    secondaryFollower.spline = SplinePathMaster.s.currentSegment.splineComputer;
                }

                wagon = Train.s.carts[i].gameObject.AddComponent<SplineWagon>();
            }

            wagon.isEngine = i == 0;
        }

        SplineWagon previousWagon = null;
        Cart nextWagon = Train.s.carts[Train.s.carts.Count-2].GetComponent<Cart>();
        for (int i = Train.s.carts.Count-1; i >= 0; i--) {
            var wagon = Train.s.carts[i].GetComponent<SplineWagon>();

            if (previousWagon != null) {
                wagon.back = previousWagon;
            }

            if (nextWagon != null) {
                wagon.offset = nextWagon.length;
            }

            previousWagon = wagon;

            if (i - 2 >= 0) {
                nextWagon = Train.s.carts[i - 2].GetComponent<Cart>();
            } else {
                nextWagon = null;
            }
        }
        
        for (int i = 0; i < Train.s.carts.Count; i++) {
            var wagon = Train.s.carts[i].GetComponent<SplineWagon>();
            wagon.Init();
        }


        var isCenter = Train.s.carts.Count % 2 == 0;

        if (isCenter) {
            centralWagons = new Transform[1];
            centralWagons[0] = Train.s.carts[Train.s.carts.Count / 2].transform;
        } else {
            centralWagons = new Transform[2];
            centralWagons[0] = Train.s.carts[Train.s.carts.Count / 2].transform;
            centralWagons[1] = Train.s.carts[Train.s.carts.Count / 2 +1].transform;
        }
    }

    private void LateUpdate() {
        if(mainFollower != null)
            mainFollower.followSpeed = 3;
        
        if (centralWagons.Length > 0) {
            var center = Vector3.zero;
            for (int i = 0; i < centralWagons.Length; i++) {
                center += centralWagons[i].position;
            }

            center /= centralWagons.Length;

            center.y = 0;

            //SplinePathMaster.s.transform.position = -center;
            //mainFollower.transform.position -= center;
        }
    }
}
