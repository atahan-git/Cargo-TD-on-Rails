using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PulseAlpha : MonoBehaviour {

    public float speed = 1f;

    private float curTime = 0;
    private Color _color;
    private Image _image;
    private TMP_Text _text;
    private SpriteRenderer _sprite;

    private void Start() {
        _image = GetComponent<Image>();
        _text = GetComponent<TMP_Text>();
        _sprite = GetComponent<SpriteRenderer>();

        if (_image)
            _color = _image.color;
        if (_text)
            _color = _text.color;
        if (_sprite)
            _color = _sprite.color;
    }


    // Update is called once per frame
    void Update() {
        _color.a = LevelReferences.s.alphaPulseCurve.Evaluate(curTime);
        curTime += Time.deltaTime * speed;
        
        if (_image)
            _image.color = _color;
        if (_text)
            _text.color = _color;
        if (_sprite)
            _sprite.color = _color;
    }
}
