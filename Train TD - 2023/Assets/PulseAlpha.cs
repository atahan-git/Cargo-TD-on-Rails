using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PulseAlpha : MonoBehaviour {

    public float speed = 1f;

    private float curTime = 0;
    private Image _image;
    private Color _color;
    private TMP_Text _text;

    private bool isImage;
    private void Start() {
        _image = GetComponent<Image>();
        isImage = _image != null;
        if (isImage) {
            _color = _image.color;
        } else {
            _text = GetComponent<TMP_Text>();
            _color = _text.color;
        }
    }


    // Update is called once per frame
    void Update() {
        _color.a = LevelReferences.s.alphaPulseCurve.Evaluate(curTime);
        curTime += Time.deltaTime * speed;
        if (isImage) {
            _image.color = _color;
        } else {
            _text.color = _color;
        }
    }
}
