using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bottle : MonoBehaviour
{
    private void OnTriggerEnter()
    {
        BottleFanLogic.Instance.ResetBottle();
    }
}
