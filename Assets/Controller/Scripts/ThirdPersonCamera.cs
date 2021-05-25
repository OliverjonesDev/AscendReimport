using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ThirdPersonCamera : MonoBehaviour
{
    #region variables
    [Header("---------------")]
    [Header("Camera Settings")]
    public Transform target;
    public LayerMask camOcclussion;
    public Transform player;
    public bool thirdpersonMode = false;
    [Range(0,10)]
    public float boomArmLength = 2;
    public float pitchMin = 80;
    public float pitchMax = -40;
    public float yawMin;
    public float yawMax;
    [Range(1, 100)]
    public float mouseSensitivity = 10;
    [Range(0.0f, 1f)]
    public float rotationSmoothTime = 0f;
    public ControllerMovement playerState;
    Vector3 rotationSmoothVelocity;
    Vector3 currentRotation;

    public float yaw;
    public float pitch;

    #endregion
    //Put this in late update so the character does not jitter around as it tries to match with the current frame
    private void Update()
    {
        UpdateCamera();
        CameraZoom();
        MovementSpeedFOVChange();
    }

    void UpdateCamera()
    {
        //So left is left and right is right (+)
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        yaw = Mathf.Repeat(yaw, 360);
        //So up is down and down is up (invert this if you want opposite affect (-))
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        //Clamps how much the camera can rotate
        if (playerState.isWallRunning)
        {
            Mathf.Clamp(yaw, yawMin, yawMax);
        }
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        //Smoothes the camera
        if (!playerState.rappling)
        {
            currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotationSmoothVelocity, 0);
            transform.eulerAngles = currentRotation;
        }
        //Rotates the player
        if (playerState.isHanging == false)
        {
            if (!playerState.isWallRunning || playerState.rappling)
            {
                player.eulerAngles = new Vector3(player.eulerAngles.x, transform.eulerAngles.y, player.eulerAngles.z);
            }
        }
        if (playerState.isWallRunning)
        {
            if (playerState.isLeftWallRunning)
            {
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, -10);
            }
            if (playerState.isRightWallRunning)
            {
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, 10);
            }
        }
        transform.position = target.position - transform.forward * boomArmLength;
    }

    void occludeRay()
    {
        //declare a new raycast hit.
        RaycastHit wallHit = new RaycastHit();
        //linecast from your player (targetFollow) to your cameras mask (camMask) to find collisions.
        if (Physics.Linecast(target.transform.position, transform.position, out wallHit, camOcclussion))
        {
            gameObject.transform.position = new Vector3(wallHit.point.x + wallHit.normal.x * 0.5f, wallHit.point.y + wallHit.normal.y * 0.5f, wallHit.point.z + wallHit.normal.z * 0.5f);
            transform.position = Vector3.Lerp(transform.position, transform.position, Time.deltaTime * rotationSmoothTime);
        }
    }

    void MovementSpeedFOVChange()
    {
        if (playerState.rappling && Camera.main.fieldOfView < 120)
        {
            Camera.main.fieldOfView += .1f;
        }
        if (playerState.gravity.y < -10 && Camera.main.fieldOfView < 120)
        {
            Camera.main.fieldOfView += .1f;
        }
        if (playerState.gravity.y < -10 && Camera.main.fieldOfView > 120)
        {
            Camera.main.fieldOfView += .01f;
        }
        if (playerState.gravity.y > -10 && !playerState.rappling)
        {
            Camera.main.fieldOfView = 100;
        }
    }
    void CameraZoom()
    {
        if (thirdpersonMode)
        {
            if (Input.mouseScrollDelta.y > 0 && boomArmLength > 0)
            {
                boomArmLength -= 100 * Time.deltaTime;
            }
            if (Input.mouseScrollDelta.y < 0 && boomArmLength < 2)
            {
                boomArmLength += 100 * Time.deltaTime;
            }
            if (boomArmLength < 0)
            {
                boomArmLength = 0;
            }
            if (boomArmLength > 2)
            {
                boomArmLength = 2;
            }
        }
    }
}
