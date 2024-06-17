using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_GemDuper : MonoBehaviour,IChangeStateToEntireTrain
{
    public void ChangeStateToEntireTrain(List<Cart> carts) {
        Train.s.currentAffectors.dupeGems = true;
    }
}
