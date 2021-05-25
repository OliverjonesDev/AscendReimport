using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ControllerMovement))]
[RequireComponent(typeof(AudioSource))]
public class ControllerAudio : MonoBehaviour
{
    public ControllerMovement conMovRef;
    public AudioSource audioSource;
    public AudioClip sprint;
    public AudioClip wallrunningClip;
    public AudioClip walk;
    public AudioClip gravity;

    public void Update()
    {
        WalkingAudio();
        WallRunningAudio();
        FallingAudio();
    }

    public void WalkingAudio()
    {
        if (conMovRef.isGrounded)
        {
            if (conMovRef.isSlowWalking && !audioSource.isPlaying && conMovRef.isGrounded)
            {
                if (audioSource.clip != walk)
                {
                    audioSource.clip = walk;
                }
                audioSource.volume = Random.Range(0.3f, .6f);
                audioSource.pitch = Random.Range(0.3f, .6f);
                audioSource.Play();
            }

            if (conMovRef.direction.magnitude > 0 && !audioSource.isPlaying && conMovRef.isGrounded)
            {
                if (audioSource.clip != sprint)
                {
                    audioSource.clip = sprint;
                }
                audioSource.volume = 0.1f; 
                audioSource.pitch = 1;
                audioSource.Play();
            }
        }
        if ((!conMovRef.isGrounded && audioSource.clip == sprint) || conMovRef.direction.magnitude == 0)
        {
            audioSource.Stop();
        }
    }
    public void WallRunningAudio()
    {
        if (conMovRef.isWallRunning)
        {
            if (conMovRef.isWallRunning && !audioSource.isPlaying)
            {
                if (audioSource.clip != wallrunningClip)
                {
                    audioSource.clip = wallrunningClip;
                }
                audioSource.volume = 0.1f;
                audioSource.pitch = 0.7f;
                audioSource.Play();
            }
        }
        if ((!conMovRef.isWallRunning && audioSource.clip == wallrunningClip) || conMovRef.direction.magnitude == 0)
        {
            audioSource.Stop();
        }
    }
    public void FallingAudio()
    {
        if (!conMovRef.isGrounded && !conMovRef.isWallRunning )
        {
            if ((conMovRef.gravity.y <= -5 || conMovRef.rappling) && !audioSource.isPlaying)
            {
                if (audioSource.clip != gravity)
                {
                    audioSource.clip = gravity;
                }
                audioSource.volume = 0.1f;
                audioSource.Play();

            }
        }
        if(audioSource.clip == gravity && (conMovRef.isGrounded || conMovRef.isWallRunning))
        {
            audioSource.Stop();
        }
    }

}
