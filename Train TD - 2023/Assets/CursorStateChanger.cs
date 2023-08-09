using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorStateChanger : MonoBehaviour, IShowButtonOnCartUIDisplay {
    //public PlayerWorldInteractionController.CursorState targetState;

    public Color color = Color.green;
    public Color GetColor() {
        return color;
    }
}


public interface IShowButtonOnCartUIDisplay {
    public Color GetColor();
}