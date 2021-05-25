using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public float currentTime;
    public TimerStart timerStart;
    public TimerEnd timerEnd;

    public void Start()
    {
        currentTime = 0.0f;
    }
    public void Update()
    {
        if (timerStart.timerOn == true && timerEnd.timerOff == false)
        {
            currentTime += Time.deltaTime;
        }
    }
}
