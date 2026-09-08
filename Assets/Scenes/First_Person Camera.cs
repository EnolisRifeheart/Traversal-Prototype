using UnityEngine;

public class First_PersonCamera : MonoBehaviour
{
    public Transform playerBody;
    public Player player;

    public float mouseSensitivity = 200f;

    private float pitch;


    [Header("Wall Run Camera")]
    public float wallLookLimit = 90f;
    public float wallRollAmount = 6f;
    public float wallCameraSmooth = 8f;

    private float wallYaw; //  Stores the Left and Right head rotation during Wall Running.
    private float currentRoll;

    [Header("FOV Settings")]
    public Camera playerCamera;

    public float normalFOV = 80f;
    public float wallRunFOV = 86f;

    public float fovSmooth = 8f;


    void Start()
    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = normalFOV;
        }
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Ensures that onlying looking up and down would rotate the camera holder.

        pitch -= mouseY;

        // Makes sure that the playerr does not flip their head completely backward or have sudden snaps.

        pitch = Mathf.Clamp(pitch, -90f, 90f);

        if (player.isWallRunning) // While wall running we want the player to feel like their rotating their head while on the wall.
        {
            wallYaw += mouseX;

            wallYaw = Mathf.Clamp(wallYaw, -wallLookLimit, wallLookLimit);
        }

        else
        {
            playerBody.Rotate(Vector3.up * mouseX);

            wallYaw = Mathf.Lerp(wallYaw, 0f, wallCameraSmooth * Time.deltaTime);
        }
        
        float targetRoll = 0f;

        if (player.isWallRunning)

            if (player.WallSide == -1)
            {
                targetRoll = -wallRollAmount;
            }

            else if (player.WallSide == 1)
            {
                targetRoll = wallRollAmount;
 
            }

        currentRoll = Mathf.Lerp(currentRoll, targetRoll, wallCameraSmooth * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(pitch, wallYaw, currentRoll);

            if (playerCamera != null)
            {
                float targetFOV;

                if (player.isWallRunning)
                {

                    targetFOV = wallRunFOV;

                }

                else
                {
                    targetFOV = normalFOV;

                }

                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovSmooth * Time.deltaTime);
            }           
        }
    }

