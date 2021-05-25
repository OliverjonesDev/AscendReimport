using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SetTimerText : MonoBehaviour
{
    public TextMeshProUGUI textToChange;
    public Timer currentTime;
    private void Update()
    {
        textToChange.text = "Current Time: " + currentTime.currentTime;
    }
}
