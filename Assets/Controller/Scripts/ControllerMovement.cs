using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControllerMovement : MonoBehaviour
{
    #region variables

    #region References
    [Header("References")]
    public CharacterController controller;
    public Transform playerCenter;
    public Transform cam;
    public Transform head;
    public Transform collisionDetectLedgeClimb;
    public Animator animatorController;
    public Transform clearenceCheckConfirm;

    #endregion

    public int elevatorTokens;

    #region Ledge Detection / Climbing
    [Header("Ledge Detection")]
    public Transform detectInitialLedge;
    [Range(0,5)]
    public float ledgeDetectDist;
    [Range(0, 5)]
    public float clearenceCheck;
    public bool ledgeDetected;
    public bool ledgeClimable;
    public bool canClimbLeft;
    public bool canClimbRight;
    public float originToHitpointDistance;
    public Vector3 hitInfo;

    public LayerMask ledgeMask;

    public RaycastHit ledgeDetection;
    public RaycastHit climable;
    public RaycastHit edgeDetection;
    public RaycastHit clearenceDetection;
    [Range(0, 1)]
    public float vaultObjectHeight;



    [Header("Ledge Climbing Fuckery")]
    public Vector3 edgeDetectionData;
    public bool isHanging;
    public float edgeXDistFromCurX;
    public float edgeYDistFromCurY;
    public float edgeZDistFromCurZ;
    bool hangPosUpdated;
    Vector3 hangPos;

    #endregion

    #region Wall Running / Climbing

    [Header("Wall Run Detection")]
    public float wallRunDetectDistance = .75f;
    public bool wallrunDetected;
    public bool directionalKeyDown;
    public Transform lastWallRan;
    public Transform lastJumpedOff;
    private RaycastHit wallrunHit;
    public bool frontWallDetected;
    public float wallFriction = 3;
    public bool wallDetected;
    public RaycastHit currentWall;

    [Header("Wall Climb Detection")]
    public Transform wallClimbDetection;
    private RaycastHit verticalClimbHitInfo;
    public float wallClimbDetectDist = 1;
    public float wallClimbSpeed;
    public LayerMask wallRunMask;
    public LayerMask CantClimb;

    #endregion

    #region Rapple

    [Header("Rapple Variables")]
    public float rappleDistance = 5;
    public float rappleVaultDistance = 1.5f;
    public float rappleSpeed = 15;
    public float curRappleSpeed;
    public float vaultJumpHeight;
    public LayerMask rappleMask;
    public bool rappling;
    public float distanceToRappleObject;

    private Vector3 rapplePoint;

    #endregion

    #region Gliding

    [Header("Glide Detection")]
    public bool canGlide;
    public float glideDetectionDistance = 2f;

    #endregion

    #region Inputs/States

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


    [Header("Other States")]
    public bool isBackhopping;
    public bool isSlowWalking;
    public bool isGliding;
    public bool isSliding;
    public bool isCrouching;

    #endregion

    #region Player Stats

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

    #endregion

    #region Physics

    [Header("Physics")]
    [HideInInspector] [SerializeField] private float gravityConst = -9.81f;
    public Vector3 gravity;
    [HideInInspector] [SerializeField] private float maxGravity;
    [HideInInspector] [SerializeField] private float termVelocity = -53f;
    public bool gravityEnabled = true;

    #endregion

    #region Ground Checks

    [Header("Ground Check")]
    public bool isGrounded;
    public bool isOnSlope;
    public bool groundSphereDetect;
    public float groundCheckRadius = 0.5f;
    public float groundCheckDistance = 0.25f;
    public float slopeCheckDistance = .5f;
    public LayerMask groundMask;
    public Vector3 clearenceCheckVector;
    public RaycastHit slopeCheckHit;
    private float slopeAngle;

    #endregion

    //Do not touch in editor, values are correct and do not need to be change

    #endregion

    #region unity Functions

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        lastJumpedOff = null;
        gravityEnabled = true;
    }
    private void Update()
    {
        UserInput();
        SetSpeed();
        Jump();
        //Restart current level
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    private void FixedUpdate()
    {
        Grounded();
        ApplyGravity();
        Walk();
        VerticalWallJump();
        LedgeDetection();
        WallRunHandle();
        Rapple();

    }

    #endregion

    #region custom functions

    #region input
    void UserInput()
    {
        //Checks for user inputs
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
        direction = new Vector3(horizontal, 0f, vertical).normalized;
        //This normalizes the direction, making it between -1, 1, and makes it so movement can happen on all axis
        isJumping = Input.GetButtonDown("Jump") && !ledgeDetected;
        canWallRun = speed != 0 && !isGrounded && !wallrunHit.collider && !ledgeDetected;
        isBackhopping = Input.GetButtonDown("Jump") && verticalClimbHitInfo.collider;
        //isClimbing = ledgeDetected && !isGrounded;
        isSlowWalking = Input.GetButton("Walk") && isGrounded;
        //Needs to be a toggle
        isGliding = Input.GetButton("Jump") && canGlide && !isWallRunning;
        isCrouching = Input.GetButton("Crouch");
        isSliding = Input.GetButton("Crouch") && isGrounded && isSlowWalking;
    }

    #endregion

    #region physics
    void Grounded()
    {
        //This is a sphere cast to check if the player is grounded
        groundSphereDetect = Physics.CheckSphere(transform.position, groundCheckRadius, groundMask);
        Physics.Raycast(transform.position, Vector3.down,out slopeCheckHit, slopeCheckDistance,groundMask);
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance);
        if (slopeCheckHit.transform != null)
        {
            slopeAngle = Vector3.Angle(slopeCheckHit.normal, Vector3.up);
            if (slopeAngle > controller.slopeLimit)
            {
                isOnSlope = true;
            }
            else
            {
                isOnSlope = false;
            }
        }
        if (groundSphereDetect || !isOnSlope && slopeCheckHit.transform )

        {
            isGrounded = true;
            
        }
        else
        {
            isGrounded = false;
        }

    }
    void Jump()
    {

        //Jumping
        //If player is grounded, allow for jumping and if jump input is down
        if (isJumping && isGrounded)
        {
            gravity.y = Mathf.Sqrt(jumpHeight * -2 *gravityConst);
        }
        if (isJumping && currentWall.transform != lastJumpedOff)
        {
            lastJumpedOff = currentWall.transform;
            gravity.y = Mathf.Sqrt(wallJumpHeight * -2 * gravityConst);
            isWallRunning = false;
        }
    }
    void ApplyGravity()
    {
        //Terminal velocity for average human 53mps
        //Sets gravity of player too 0 and changes step off set so player can walk up slight ledges and steps
        if (gravityEnabled)
        {
            if (isGrounded && gravity.y <= 0)
            {
                gravity.y = -5f;
                controller.stepOffset = .2f;
                lastJumpedOff = null;

            }
            //Player slowly falls down wall
            if (isWallRunning && gravity.y > 0)
            {
                gravity.y = Mathf.Lerp(gravity.y, 0, wallFriction * Time.deltaTime);
            }
            if (isWallRunning && gravity.y < 1)
            {
                gravity.y = Mathf.Lerp(gravity.y, -1.5f, wallFriction * Time.deltaTime);
            }
            //Applies gravity whilst player is in air
            if (!isGrounded && !isWallRunning)
            {
                if (gravity.y > maxGravity)
                {
                    gravity.y += gravityConst * Time.deltaTime;
                    controller.stepOffset = 0f;
                }
            }
            RaycastHit surfaceDetection;
            Physics.Raycast(head.position, Vector3.up, out surfaceDetection, .3f, groundMask);
            //Debug.DrawLine(head.position)
            if (surfaceDetection.collider != null)
            {
                if (gravity.y > 0)
                {
                    gravity.y = 0f;
                }
            }
            //Moves player by cur gravity amount
            controller.Move(gravity * Time.deltaTime);
        }
    }

    #endregion

    #region general movement
    void Walk()
    {
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
        Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        moveDirection = moveDirection.normalized;
        if (!rappling)
        {
            if (direction.magnitude != 0 && !isWallRunning)
            {
                controller.Move(moveDirection * speed * Time.fixedDeltaTime);
            }
            if (isWallRunning)
            {
                controller.Move(transform.TransformDirection(Vector3.forward) * speed * Time.fixedDeltaTime);

            }
        }

    }
    void SetSpeed()
    {
        if (direction.magnitude != 0 && !isSlowWalking)
        {
            speed = sprintSpeed;
        }
        if (isGrounded)
        {
            if (isSlowWalking)
            {
                speed = walkSpeed;
            }
            else if (direction.magnitude == 0)
            {
                speed = 0f;
            }
        }
        //Changes air speed, when using horizontal and back keys
        if (horizontal != 0 && vertical <= 0 && !isGrounded|| vertical <= 0 && !isGrounded)
        {
            if (wallrunDetected && vertical == -1 || verticalClimbHitInfo.collider != null && vertical == -1)
            {
                speed = 0;
            }
            else
            {
                speed = walkSpeed / 2;
            }
        }
        //If player not wall running, speed set 0, they fall
    }
    void LedgeDetection()
    {
        RaycastHit hit;
        RaycastHit clearence;
        Physics.Raycast(detectInitialLedge.position, Vector3.down, out hit, ledgeDetectDist, ledgeMask);
        hitInfo = new Vector3(hit.point.x, hit.point.y, hit.point.z);
        //Cleareance Check
        Physics.Raycast(head.position, Vector3.up, out clearence, clearenceCheck, ledgeMask);
        clearenceCheckVector = new Vector3(clearence.point.x, clearence.point.y, clearence.point.z);
        checkForCollisions = Physics.Raycast(collisionDetectLedgeClimb.position, detectInitialLedge.transform.position, ledgeMask);
        //Cyan is distance to auto vault
        //Blue is distance to climb up ledge
        Debug.DrawLine(detectInitialLedge.position, detectInitialLedge.position + Vector3.down * ledgeDetectDist, Color.cyan);
        Debug.DrawLine(detectInitialLedge.position, detectInitialLedge.position + Vector3.down * vaultObjectHeight, Color.blue);
        if (!checkForCollisions)
        {
            if (hit.transform != null && clearence.transform == null)
            {
                ledgeDetected = true;
                originToHitpointDistance = Vector3.Distance(hit.point, detectInitialLedge.position);
                Debug.DrawLine(hit.point, detectInitialLedge.position);
                if (Input.GetButton("Jump") && ledgeDetected)
                {
                    speed = 0;
                    //Move is relative to character controller, it is not absolute
                    //Need to get rid of players current position and then move player to hit point of raycast in world space
                    gravity.y = -2f;
                    transform.position = hit.point;
                    Debug.Log("Ledge climbed");
                }
                Debug.DrawLine(hit.point, detectInitialLedge.transform.position, Color.green);
                Debug.Log("Ledge detected");
            }
            else
            {
                ledgeDetected = false;
            }
        }
    }
    //This has good aspects and real shit ones, we can figure how to use this dogshit later
    
    /*void NewLedgeDetection()
    {
        RaycastHit wallDetectionCast;
        Debug.DrawRay(detectInitialLedge.position, Vector3.down * ledgeDetectDist, Color.black);
        //This is used to check if player can climb current ledge if there is nothing above/is room
        //if Null Player can climb, if != null no climb
        Physics.Raycast(head.position, Vector3.up, out clearenceDetection, clearenceCheck, groundMask);
        if (clearenceDetection.transform != null)
        {
            Debug.DrawRay(head.position, Vector3.up * clearenceCheck);
        }
        else
        {
            InitialLedgeDetection();
            Debug.DrawRay(playerCenter.position, transform.TransformDirection(Vector3.forward) * 1f);
            wallDetected = Physics.Raycast(playerCenter.position, transform.TransformDirection(Vector3.forward), out wallDetectionCast, 1f, groundMask);
            if (ledgeDetection.transform != null && Input.GetButton("Jump"))
            {
                if (hangPosUpdated == false)
                {
                    transform.rotation = Quaternion.FromToRotation(-Vector3.forward, wallDetectionCast.normal);
                    hangPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                    hangPosUpdated = true;
                    transform.position = hangPos;
                    //transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                    gravity.y = 0f;
                }
                isHanging = true;
            }
            if (isHanging)
            {
                if (Input.GetAxis("Vertical") < 0f)
                {
                    isHanging = false;
                }
                if (Input.GetAxis("Vertical") > 0f)
                {
                    Debug.Log(climable.point);
                    transform.position = climable.point;
                    if (!wallDetected)
                    {
                        isHanging = false;
                    }
                }
            }
            else
            {
                ledgeClimable = false;
                ledgeDetected = false;
                canClimbLeft = false;
                canClimbRight = false;
                isHanging = false;
                hangPosUpdated = false;

            }

        }
    }
    void InitialLedgeDetection()
    {
        Vector3 edgeDetectionPos;
        //This is used to detect an inital ledge that the player may be able to climb
        Physics.Raycast(detectInitialLedge.position, Vector3.down * ledgeDetectDist, out ledgeDetection, ledgeDetectDist, groundMask);
        ledgeDetected = ledgeDetection.transform;
        if (ledgeDetected)
        {
            Debug.DrawLine(ledgeDetection.point, detectInitialLedge.position, Color.magenta);
            //This is used to detect edge position
            edgeDetectionPos = new Vector3(head.transform.position.x, ledgeDetection.point.y, head.transform.position.z);
            Physics.Raycast(edgeDetectionPos, transform.TransformDirection(Vector3.forward), out edgeDetection, 1, groundMask);
            Debug.DrawLine(edgeDetectionPos, edgeDetection.point, Color.blue);
            edgeDetectionData = edgeDetection.point;
        }
        //Used to detect if there is enough room to climb ledge
        if (ledgeDetected)
        {
            Physics.Raycast(detectInitialLedge.position + transform.TransformDirection(Vector3.forward) * .25f, Vector3.down * ledgeDetectDist, out climable, ledgeDetectDist, groundMask);
            if (climable.transform == null)
            {
                Debug.DrawRay(detectInitialLedge.position + transform.TransformDirection(Vector3.forward) * .25f, Vector3.down * ledgeDetectDist, Color.red);
                Debug.DrawLine(climable.point, detectInitialLedge.position, Color.red);
                ledgeClimable = false;
            }
            if (climable.transform != null)
            {
                Debug.DrawRay(detectInitialLedge.position + transform.TransformDirection(Vector3.forward) * .25f, Vector3.down * ledgeDetectDist, Color.red);
                Debug.DrawLine(climable.point, detectInitialLedge.position + transform.TransformDirection(Vector3.forward) * .25f, Color.green);
                ledgeClimable = true;
            }
            else
            {
                ledgeClimable = false;
            }
        }
        else
        {
            ledgeClimable = false;
        }


        if (ledgeDetection.transform != null)
        {
            originToHitpointDistance = Vector3.Distance(ledgeDetection.point, detectInitialLedge.position);
        }
        else
        {
            originToHitpointDistance = 0f;
        }

    }
    void LeftLedgeDetection()
    {
        RaycastHit ledgeDetectionLeft;
        //This is used to detect an inital ledge that the player may be able to climb
        Physics.Raycast(detectInitialLedge.position + transform.TransformDirection(Vector3.left) * .5f, Vector3.down, out ledgeDetectionLeft, ledgeDetectDist, groundMask);
        if (ledgeDetectionLeft.transform != null)
        {
            Debug.DrawLine(detectInitialLedge.position + transform.TransformDirection(Vector3.left) * .5f, ledgeDetectionLeft.point, Color.red);
            canClimbLeft = true;
        }
        else
        {
            canClimbLeft = false;
        }
    }
    void RightLedgeDetection()
    {
        RaycastHit ledgeDetectionRight;
        //This is used to detect an inital ledge that the player may be able to climb
        Physics.Raycast(detectInitialLedge.position + transform.TransformDirection(Vector3.right) * .5f, Vector3.down, out ledgeDetectionRight, ledgeDetectDist, groundMask);
        if (ledgeDetectionRight.transform != null)
        {
            canClimbRight = true;
            Debug.DrawLine(detectInitialLedge.position + transform.TransformDirection(Vector3.right) * .5f, ledgeDetectionRight.point, Color.red);
        }
        else
        {
            canClimbRight = false;
        }
    }
    */
    #region Wall Running
    void VerticalWallJump()
    {
        Physics.Raycast(wallClimbDetection.position, transform.TransformDirection(Vector3.forward), out verticalClimbHitInfo, wallRunDetectDistance, groundMask);
        if (isBackhopping && verticalClimbHitInfo.transform != lastJumpedOff && !ledgeDetected && Input.GetButton("Jump"))
        {
            if (!isGrounded && verticalClimbHitInfo.transform.gameObject.layer != CantClimb)
            {
                lastJumpedOff = verticalClimbHitInfo.transform;
            }
            jumpHeight = 1.5f;
            gravity.y = Mathf.Sqrt(jumpHeight * -2 * gravityConst);

        }
    }
    void WallRunHandle()
    {
        #region Casts
        RaycastHit leftWallCheck;
        RaycastHit rightWallCheck;
        frontWallDetected = Physics.Raycast(playerCenter.position, transform.TransformDirection(Vector3.forward), 1f, groundMask);
        //These raycasts will be used to detect the walls around the player, left and right wall, this will allow for the player
        //to determine what wall they would like to run on.
        Physics.Raycast(playerCenter.position, transform.TransformDirection(Vector3.left), out leftWallCheck, wallRunDetectDistance, wallRunMask);
        Physics.Raycast(playerCenter.position, transform.TransformDirection(Vector3.right), out rightWallCheck, wallRunDetectDistance, wallRunMask);
        //This checks if not on ground and allowed to wallrun
        Physics.Raycast(transform.position, Vector3.down, out wallrunHit, jumpHeight - 0.3f, groundMask);
        #endregion
        if (leftWallCheck.transform && rightWallCheck.transform && !frontWallDetected) 
        {
            float leftDist;
            float rightDist;
            leftDist = Vector3.Distance(leftWallCheck.point, playerCenter.position);
            rightDist = Vector3.Distance(rightWallCheck.point, playerCenter.position);
            if (leftDist > rightDist)
            {
                isRightWallRunning = true;
                currentWall = rightWallCheck;
                wallDetected = true;
                WallRunExecute(currentWall);
            }
            else
            {
                isLeftWallRunning = true;
                currentWall = leftWallCheck;
                wallDetected = true;
                WallRunExecute(currentWall);
            }

        }
        if (leftWallCheck.transform != null && !frontWallDetected)
        {
            isLeftWallRunning = true;
            wallDetected = true;
            currentWall = leftWallCheck;
            WallRunExecute(currentWall);
        }
        else if (rightWallCheck.transform != null && !frontWallDetected)
        {
            isRightWallRunning = true;
            wallDetected = true;
            currentWall = rightWallCheck;
            WallRunExecute(currentWall);
        }
        else
        {
            isRightWallRunning = false;
            isLeftWallRunning = false;
            wallDetected = false;
        }
        if (isGrounded)
        {
            isWallRunning = false;
            lastWallRan = null;
            lastJumpedOff = null;
            transform.localEulerAngles = new Vector3(0f, transform.rotation.y, 0f);
        }
        else if (wallDetected == false)
        {
            isWallRunning = false;
            wallRunDetectDistance = .75f;
        }
        if (frontWallDetected == true)
        {
            isWallRunning = false;
        }
        if (!isWallRunning)
        {
            transform.localEulerAngles = new Vector3(0, transform.rotation.y, 0);
        }
    }
    void WallRunExecute(RaycastHit currentWall)
    {
        if (!rappling)
        {
            if (canWallRun && lastJumpedOff != currentWall.transform)
            {
                if (currentWall.transform.CompareTag("Cylinder"))
                {
                    wallRunDetectDistance = 2f;
                }
                else
                {
                    wallRunDetectDistance = 0.75f;
                }
                lastWallRan = currentWall.transform;
                isWallRunning = true;
                Debug.DrawLine(currentWall.point, playerCenter.position, Color.green);
                if (isLeftWallRunning)
                {

                    Vector3 temp = Vector3.Cross(transform.up, currentWall.normal);
                    transform.rotation = Quaternion.LookRotation(-temp);
                    //transform.rotation = Quaternion.FromToRotation(Vector3.right, currentWall.normal);
                    Debug.Log("Wall running - Left");
                }
                if (isRightWallRunning)
                {
                    //This makes player only rotate on the Y so player cannot wall run directly up walls that are rotated
                    Vector3 temp = Vector3.Cross(transform.up, currentWall.normal);
                    transform.rotation = Quaternion.LookRotation(temp);
                    //This was the old rotation, this made the player rotate all of its axis
                    //transform.rotation = Quaternion.FromToRotation(-Vector3.right, currentWall.normal);
                    Debug.Log("Wall running - Right");
                }
                if (isWallRunning && Input.GetAxis("Vertical") < 0)
                {
                    isWallRunning = false;
                    lastJumpedOff = currentWall.transform;

                }
            }
        }
    }

    #endregion

    #region Rapple
    void Rapple()
    {
        #region casts
        RaycastHit canRapple;
        Physics.Raycast(cam.transform.position, cam.TransformDirection(Vector3.forward), out canRapple, rappleDistance, rappleMask);
        //Debug.DrawRay(cam.transform.position, cam.TransformDirection(Vector3.forward) * rappleDistance);
        #endregion

        if (canRapple.transform == null)
        {
            distanceToRappleObject = 0;
        }
        else
        {
            distanceToRappleObject = Vector3.Distance(transform.position, canRapple.point);
        }

        if (canRapple.transform != null)
        {
            Debug.Log("Looking at object that can be rappled too");
            if (Input.GetKey(KeyCode.Mouse0) || rappling)
            {
                if (rapplePoint == Vector3.zero)
                {
                    rapplePoint = canRapple.point;
                }
                rappling = true;
                Debug.Log("Player is Rappling");
                gravityEnabled = false;

                if (rappleSpeed > curRappleSpeed)
                {
                    if (curRappleSpeed == 0)
                    {
                        curRappleSpeed = speed;
                    }
                    curRappleSpeed = Mathf.Lerp(curRappleSpeed, rappleSpeed, 2 * Time.deltaTime);
                }
                transform.position = Vector3.MoveTowards(transform.position, rapplePoint, curRappleSpeed * Time.deltaTime);
                cam.LookAt(rapplePoint);
            }
        }
        if (distanceToRappleObject <= rappleVaultDistance)
        {
            RappleBoost();
        }

    }
    void RappleBoost()
    {
        if (rappling)
        {
            rapplePoint = Vector3.zero;
            curRappleSpeed = 0;
            isWallRunning = false;
            lastWallRan = null;
            lastJumpedOff = null;
            gravityEnabled = true;
            rappling = false;
            gravityEnabled = true;
            gravity.y = Mathf.Sqrt(vaultJumpHeight * -2 * gravityConst);
            Debug.Log("BOOOOOOOOOOOST");
        }
    }

    #endregion

    /*  Gliding
        #region Gliding

        void Gliding()
        {
            RaycastHit glideDetection;
            Physics.Raycast(playerCenter.position, transform.TransformDirection(Vector3.down), out glideDetection, glideDetectionDistance, groundMask);
            if (glideDetection.transform == null && !verticalClimbHitInfo.collider)
            {
                canGlide = true;
                Debug.DrawRay(playerCenter.position, transform.TransformDirection(Vector3.down) * glideDetectionDistance, Color.black);
            }
            else
            {
                canGlide = false;
            }
            if (isGliding)
            {
                maxGravity = -1f;
                Debug.Log("Gliding");
                if (gravity.y < maxGravity)
                {

                    gravity.y = maxGravity;
                }
            }
            if (!isGliding && maxGravity != termVelocity)
            {
                maxGravity = termVelocity;
            }
        }

        #endregion*/

    #endregion

    #endregion
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Level Change"))
        {
            Debug.Log("Level Change");
            SceneManager.LoadScene(other.GetComponent<LevelChange>().levelName);
            Debug.Log("Level loaded");
        }
        if (other.gameObject.CompareTag("Elevator Token"))
        {
            elevatorTokens++;
        }
    }

}