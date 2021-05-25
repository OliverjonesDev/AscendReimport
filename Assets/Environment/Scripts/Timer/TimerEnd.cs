using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerEnd : MonoBehaviour
{
    public bool timerOff;

    private void OnTriggerEnter(Collider other)
    {
        timerOff = true;
    }
}
