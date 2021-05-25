using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ObsticleMoving : MonoBehaviour
{
    Vector3 originalPosition;
    [Range(0, 10)]
    public float moveSpeed;
    [Header("Back Fourth Movement")]
    [Range(-10, 10)]
    public float XMoveAmount;
    [Header("Up Down Movement")]
    [Range(-10, 10)]
    public float YMoveAmount;
    [Header("Left Right Movement")]
    [Range(-10, 10)]
    public float ZMoveAmount;

    [Header("Debugging maths")]
    public float xMaths;
    public float zMaths;
    public float yMaths;
    public float time;
    private void Start()
    {
        originalPosition = transform.position;
    }
    private void Update()
    {
        ChangeObjectPosition();
        time = Time.fixedTime;
        xMaths = XMoveAmount * Mathf.Sin(Time.fixedTime * moveSpeed);
        yMaths = YMoveAmount * Mathf.Sin(Time.fixedTime * moveSpeed);
        zMaths = ZMoveAmount * Mathf.Sin(Time.fixedTime * moveSpeed);

    }
    void ChangeObjectPosition()
    {
        Vector3 position = originalPosition;
        position.x += XMoveAmount * Mathf.Sin(Time.fixedTime * moveSpeed);
        position.y += YMoveAmount * Mathf.Sin(Time.fixedTime * moveSpeed);
        position.z += ZMoveAmount * Mathf.Sin(Time.fixedTime * moveSpeed);
        transform.position = position;
    }
}
