using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DEPRICATEDWALLRUNNING : MonoBehaviour
{
    #region variables
    [Header("References")]
    public CharacterController controller;
    public Transform playerCenter;
    public Transform cam;
    public Transform head;
    public Transform collisionDetectLedgeClimb;
    public Animator animatorController;

    [Header("Ledge Detection")]
    public Transform detectInitialLedge;
    [Range(0, 5)]
    public float ledgeDetectDist;
    [Range(0, 5)]
    public float clearenceCheck;
    public bool ledgeDetected;
    public bool ledgeClimable;
    public bool canClimbLeft;
    public bool canClimbRight;
    public float originToHitpointDistance;
    public Vector3 hitInfo;

    public RaycastHit ledgeDetection;
    public RaycastHit climable;
    public RaycastHit edgeDetection;
    public RaycastHit clearenceDetection;


    [Header("Ledge Climbing Fuckery")]
    public Vector3 edgeDetectionData;
    public float edgeXDistFromCurX;
    public float edgeYDistFromCurY;
    public float edgeZDistFromCurZ;

    [Header("Wall Run Detection")]
    public float wallRunDetectDistance = .75f;
    public bool wallrunDetected;
    public bool directionalKeyDown;
    public Transform lastWallRan;
    public Transform lastJumpedOff;
    private RaycastHit wallrunHit;
    public float wallFriction = 3;
    public bool wallDetected;

    [Header("Wall Climb Detection")]
    public Transform wallClimbDetection;
    private RaycastHit verticalClimbHitInfo;
    public float wallClimbDetectDist = 1;
    public float wallClimbSpeed;


    [Header("Glide Detection")]
    public bool canGlide;
    public float glideDetectionDistance = 2f;

    [Header("Inputs")]
    public float horizontal;
    public float vertical;
    public Vector3 direction;
    public bool isJumping;
    [Header("Wall Running")]
    public bool isWallRunning;
    public bool canWallRun;

    public bool isRightWallRunning;
    public bool isLeftWallRunning;
    public bool isWallJumping;

    [Header("Other States")]
    public bool isBackhopping;
    public bool isSlowWalking;
    public bool isGliding;
    public bool isSliding;
    public bool isCrouching;

    [Header("Player Stats")]
    public float walkSpeed = 16;
    //Speed is the current velocity of the player, whilst in air velocity does not decrease nor increase,
    //Gravity is intended to handle the downward speed of falling
    public float speed = 0;
    public float sprintSpeed = 8;
    public float acceleration = 2;
    public float sprintDeaceleration = 2;
    public float jumpHeight = 1.5f;
    public float wallJumpHeight = 3;
    public bool checkForCollisions;

    [Header("Physics")]
    [HideInInspector] [SerializeField] private float gravityConst = -9.81f;
    public Vector3 gravity;
    [HideInInspector] public bool isGrounded;
    [HideInInspector] [SerializeField] private float maxGravity;
    [HideInInspector] [SerializeField] private float termVelocity = -53f;

    [Header("Ground Check")]
    public float groundCheckRadius = 0.5f;
    public LayerMask groundMask;
    public Vector3 clearenceCheckVector;

    //Do not touch in editor, values are correct and do not need to be change
    private float turnSmoothVelocity;
    private float turnSmoothTime = 0.1f;
    void WallRun()
    {
        #region Casts
        RaycastHit leftWallCheck;
        RaycastHit rightWallCheck;
        //These raycasts will be used to detect the walls around the player, left and right wall, this will allow for the player
        //to determine what wall they would like to run on.
        Physics.Raycast(playerCenter.position, transform.TransformDirection(Vector3.left), out leftWallCheck, wallRunDetectDistance, groundMask);
        Physics.Raycast(playerCenter.position, transform.TransformDirection(Vector3.right), out rightWallCheck, wallRunDetectDistance, groundMask);
        //This checks if not on ground and allowed to wallrun
        Physics.Raycast(transform.position, Vector3.down, out wallrunHit, jumpHeight - 0.3f, groundMask);
        #endregion

        #region Wall Checks
        if (leftWallCheck.transform && rightWallCheck.transform)
        {
            float leftDist;
            float rightDist;
            leftDist = Vector3.Distance(leftWallCheck.point, playerCenter.position);
            rightDist = Vector3.Distance(rightWallCheck.point, playerCenter.position);
            if (leftDist > rightDist)
            {
                isRightWallRunning = false;
                isLeftWallRunning = true;
                wallDetected = true;
            }
            else
            {
                isLeftWallRunning = false;
                isRightWallRunning = true;
                wallDetected = true;
            }

        }
        #region Distance Calc
        float leftWallDist = Vector3.Distance(leftWallCheck.point, playerCenter.position);
        if (leftWallCheck.collider == null)
        {
            leftWallDist = 0;
            isLeftWallRunning = false;
        }
        float rightWallDist = Vector3.Distance(rightWallCheck.point, playerCenter.position);
        if (rightWallCheck.collider == null)
        {
            rightWallDist = 0;
            isRightWallRunning = false;
        }
        //If wall detected 
        if (leftWallCheck.collider != null || rightWallCheck.collider != null)
        {
            wallrunDetected = true;
        }
        else
        {
            wallrunDetected = false;
            isWallRunning = false;

        }
        if (rightWallCheck.transform == lastJumpedOff || leftWallCheck.transform == lastJumpedOff)
        {
            isWallRunning = false;
        }
        if (!canWallRun)
        {
            isLeftWallRunning = false;
            isRightWallRunning = false;
        }
        #endregion

        //Which wall is being ran on
        if (leftWallDist < rightWallDist)
        {
            RightWallRun(rightWallCheck);
        }
        else
        {
            LeftWallRun(leftWallCheck);
        }
        if (isWallRunning)
        {
            wallRunDetectDistance = 2;
        }
        else
        {
            wallRunDetectDistance = 0.75f;
        }
        if (isRightWallRunning)
        {
            cam.localEulerAngles = new Vector3(cam.localEulerAngles.x, cam.localEulerAngles.y, 10);
        }
        if (isLeftWallRunning)
        {
            cam.localEulerAngles = new Vector3(cam.localEulerAngles.x, cam.localEulerAngles.y, -10);
        }
        #endregion
    }
    //Broke these down so its easier to read what is happening within this core function
    void RightWallRun(RaycastHit rightWallCheck)
    {
        if (rightWallCheck.transform != null && canWallRun && rightWallCheck.transform != lastJumpedOff && gravity.y > -20)
        {
            if (!isRightWallRunning)
            {
                isRightWallRunning = true;
            }
            if (isLeftWallRunning)
            {
                isLeftWallRunning = false;
            }
            lastWallRan = rightWallCheck.transform;
            Debug.DrawLine(rightWallCheck.point, playerCenter.position, Color.green);
            isWallRunning = true;
            transform.rotation = Quaternion.FromToRotation(-Vector3.right, rightWallCheck.normal);
            if (isWallJumping && rightWallCheck.transform != lastJumpedOff)
            {
                lastJumpedOff = rightWallCheck.transform;
                gravity.y = Mathf.Sqrt(wallJumpHeight * -2 * gravityConst);
                isWallRunning = false;
            }

        }

    }
    void LeftWallRun(RaycastHit leftWallCheck)
    {
        if (leftWallCheck.transform != null && canWallRun && leftWallCheck.transform != lastJumpedOff && gravity.y > -20)
        {
            if (!isLeftWallRunning)
            {
                isLeftWallRunning = true;
            }
            if (isRightWallRunning)
            {
                isRightWallRunning = false;
            }
            lastWallRan = leftWallCheck.transform;
            Debug.DrawLine(leftWallCheck.point, playerCenter.position, Color.green);
            isWallRunning = true;
            //Rotates camera
            transform.rotation = Quaternion.FromToRotation(Vector3.right, leftWallCheck.normal);
            cam.localEulerAngles = new Vector3(cam.localEulerAngles.x, cam.localEulerAngles.y, -10);

            if (isWallJumping && leftWallCheck.transform != lastJumpedOff)
            {
                lastJumpedOff = leftWallCheck.transform;
                gravity.y = Mathf.Sqrt(wallJumpHeight * -2 * gravityConst);
                isWallRunning = false;
            }
        }
    }
}
#endregion