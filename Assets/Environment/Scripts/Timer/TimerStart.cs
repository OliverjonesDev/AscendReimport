using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerStart : MonoBehaviour
{
    public bool timerOn;

    private void OnTriggerEnter(Collider other)
    {
        timerOn = true;
    }
}
