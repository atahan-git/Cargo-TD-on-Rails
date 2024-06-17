using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PulseColors : MonoBehaviour
{
    
    public Color colorA=Color.magenta;
    public Color colorB = Color.cyan;

    public float cycleSpeed = 1f;

    public float currentState;

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
    void Update() {

        currentState += Time.deltaTime * cycleSpeed;
        if (currentState > 1) {
            currentState -= 1;
        }
        
        if (currentState < 0.5f) {
            _color = Color.Lerp(colorA, colorB, currentState*2);
        }else {
            _color = Color.Lerp(colorB, colorA, (currentState - 0.5f) * 2);
        }

        if (_image)
            _image.color = _color;
        if (_text)
            _text.color = _color;
        if (_sprite)
            _sprite.color = _color;
    }
}
