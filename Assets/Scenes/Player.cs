using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Vector2 moveInput;
    private bool spaceHeld;
    Rigidbody rb;

    [Header("Movement Settings")]
    public float groundSpeed = 8f;
    public float airSpeed = 8f;

    public float groundAcceleration = 70f;
    public float groundDeceleration = 80f;

    public float airAcceleration = 10f;

    [Header("Jump Settings")]
    public float jumpPower = 10f;

    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.15f;
    public float wallJumpUpForce = 8f;
    public float wallJumpAwayForce = 7f;
    public float wallJumpForwardForce = 4f;
    private float coyoteTimer;
    private float jumpBufferTimer;

    [Header("Wall Movements Settings")]
    public float minimumWallRunSpeed = 4f;
    public float maximumWallRunSpeed = 12f;
    public float wallRunSpeedMultiplier = 1.15f;
    public float wallRunDuration = 1.25f;
    public float wallRunDrag = 0.5f;

    public float wallTargetDistance = 0.55f;
    public float wallAttachStrength = 12f;
    public float maxWallAttachSpeed = 3f;
    public float wallCheckDistance = 0.8f;
    public float wallStartLift = 2f;
    public float wallMaxRiseSpeed = 4f;
    public float wallGravityScale = 0.8f;
    public float wallMaxFallSpeed = 4f;

    private float wallMoveTimer;

    private Vector3 wallEntryVel;
    private Vector3 wallRunDirection;

    private float wallRunSpeed;
    private float wallVerticalSpeed;

    private Vector3 wallNormal;

    private bool touchingRunnableWall;
    private bool canWallRun = true;

    private Collider currentWallCollider;
    private RaycastHit currentWallHit;

    public int WallSide { get; private set; }

    [Header("Collision-Related")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    public bool isWallLeft;
    public bool isWallRight;
    public bool isWallRunning;

    public bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>(); // Making sure good ole rigidbody works.

        rb.maxLinearVelocity = 20f; // Ensuring that physics do not push Player past 20 speed.
    }

    void Update()
    {

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        spaceHeld = Input.GetKey(KeyCode.Space);

        if (!spaceHeld && !isWallRunning)
        {
            canWallRun = true;
        }

        // If the player presses jump, it should remember and buffer a small window to briefly jump past edges.

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferTimer = jumpBufferTime;

            // If we are current wall running, pressing space should launch us from the wall.

            if (isWallRunning)
            {
                WallJump();
            }

            else
            {

                // Otherwise treat Space ass our normal Jump.

                jumpBufferTimer = jumpBufferTime;
            }
        }

        else
        {
            jumpBufferTimer -= Time.deltaTime;

        }
    }

    void FixedUpdate()
    {
        rb.angularVelocity = Vector3.zero;

        GroundCheck();

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            canWallRun = true;

            if (isWallRunning)
            {
                StopWallRun();
            }
        }
        else
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }

        if (!isWallRunning && jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            Jump();

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        if (!isWallRunning && !isGrounded && touchingRunnableWall && spaceHeld && canWallRun)
        {
            StartWallRun();
        }

        if (isWallRunning)
        {
            WallRun();
        }
        else
        {
            Movement();
        }
    }

    void WallJump()

        // We need to combine three directions for our wall jump.
        // Away from wall + Upward Force + and our own momentum.
    {
        Vector3 jumpDirection = wallNormal * wallJumpAwayForce + Vector3.up * wallJumpUpForce + wallRunDirection * wallJumpForwardForce;

        // Before launching away, we need to stop our current WallRun

        StopWallRun();

        Vector3 currentVel = rb.linearVelocity;

        rb.linearVelocity = new Vector3(currentVel.x, 0f, currentVel.z);

        rb.AddForce(jumpDirection, ForceMode.Impulse);

        canWallRun = false;

        Debug.Log("Wall Jump!");
            
        
    }

    void StartWallRun()
    {
        Vector3 currentVel = rb.linearVelocity;

        Vector3 horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);

        // We need some form of movement before the player can initiate a wall run.

        if (horizontalVel.sqrMagnitude < 0.5)
        {
            return;
        }

        wallEntryVel = currentVel;

        // Remove the part of our momemtum going into the wall and snapshot it.

        Vector3 projectedVel = Vector3.ProjectOnPlane(horizontalVel, wallNormal);

        if (projectedVel.sqrMagnitude < 0.05f)
        {
            projectedVel = Vector3.ProjectOnPlane(transform.forward, wallNormal);
        }
        

        if (projectedVel.sqrMagnitude < 0.05f)
        {
            projectedVel = Vector3.Cross(Vector3.up, wallNormal);

            if (Vector3.Dot(projectedVel, transform.forward) <0f)
            {
                projectedVel = -projectedVel;
            }
        }

        wallRunDirection = projectedVel.normalized;

        wallRunSpeed = Mathf.Clamp(horizontalVel.magnitude * wallRunSpeedMultiplier, minimumWallRunSpeed, maximumWallRunSpeed); // Added RunSpeedMultipler to increase movement feel.

        // Give the players an upward start during Wall Run.

        wallVerticalSpeed = Mathf.Clamp(currentVel.y, wallStartLift, wallMaxRiseSpeed);

        wallMoveTimer = wallRunDuration;

        isWallRunning = true;
        canWallRun = false;

        //WallRun will handle gravity while active.

        rb.useGravity = false;


        float wallSideDot = Vector3.Dot(wallNormal, transform.right);

        if (wallSideDot < 0f)
        {
            WallSide = 1; 
            isWallRight = true; 
            isWallLeft = false;     
        }

        else
        {
            WallSide = -1; 
            isWallLeft = true; 
            isWallRight = false;

        }

        Debug.Log("Wall-Run time.");
    }

    void StopWallRun()
    {
        if (!isWallRunning)
        {
            return;
        }

        isWallRunning = false;

        WallSide = 0;

        isWallLeft = false;
        isWallRight = false;

        rb.useGravity = true;
    }

       


   void WallRun()
    {
        wallMoveTimer -= Time.fixedDeltaTime;

        if (wallMoveTimer <= 0f)
        {
            StopWallRun();
            return;
        }

        if (!WallStillThere())
        {
            StopWallRun();
            return;
        }

        // When momemtum reaches its peak, momentum should slowly lower.
        wallRunSpeed = Mathf.MoveTowards(wallRunSpeed, 0f, wallRunDrag * Time.fixedDeltaTime);

        Vector3 runVelocity = wallRunDirection * wallRunSpeed;


        wallVerticalSpeed += Physics.gravity.y * wallGravityScale * Time.fixedDeltaTime;

        
        wallVerticalSpeed =Mathf.Max(wallVerticalSpeed,-wallMaxFallSpeed);


        // We need to make sure that the Player attaches at the correct distance to the wall.
        float distanceError =currentWallHit.distance - wallTargetDistance;

        float correctionSpeed = Mathf.Clamp(distanceError * wallAttachStrength, -maxWallAttachSpeed, maxWallAttachSpeed);

        Vector3 attachmentVelocity = -wallNormal * correctionSpeed;


        rb.linearVelocity = runVelocity + attachmentVelocity + Vector3.up * wallVerticalSpeed;
    }


    void Jump()
    {
        Vector3 currentVel = rb.linearVelocity;

        // We need to ensure that Vertical velocity is reset before any jumping is made.

        rb.linearVelocity = new Vector3(currentVel.x, 0f, currentVel.z);

        rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);

    }
    void Movement()
    {
        // First we have to grab the direction that the player wants to move.

        Vector3 dir = Direction();

        // We need to calculate our current velocity.

        Vector3 currentVel = rb.linearVelocity;

        // We only want to use the X and Z planes for movement here.
        // Y-axis belongs to the power of Jumping.

        Vector3 currentHorizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);

        // Now we need to decide how fast we move when we are grounded, and add acceleration to it.

        float targetSpeed = isGrounded ? groundSpeed : airSpeed;

        Vector3 targetHorizontalVel = dir * targetSpeed;

        float acceleration;

        if (isGrounded)
        {

            if (dir != Vector3.zero)  
            {
                acceleration = groundAcceleration;
            }

            else
            {
                acceleration = groundDeceleration;
            }
        }

        // Movement should feel a bit different in the air, as their will be acceleration during the air as well as a vertical drop.
        // There should be a feeling of snappy weight ideally.

        else
        {
            acceleration = airAcceleration;

        }

        Vector3 newHorizontalVel = Vector3.MoveTowards

        (currentHorizontalVel, targetHorizontalVel, acceleration * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newHorizontalVel.x, currentVel.y, newHorizontalVel.z);

    }

    Vector3 Direction()
    {

        Vector3 dir = transform.forward * moveInput.y + transform.right * moveInput.x; // Ensures that as long as we are aiming towards our intended direction, inputs should stay relative towards that direction.

        // We need to prevent diagonal movement from being faster.

        return dir.normalized;
    }

    // Using built in sphere to check for overlap on the floor. 

    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);  
    }

    bool WallStillThere()
    {
        Vector3 towardWall = -wallNormal;

        if (Physics.Raycast(rb.worldCenterOfMass, towardWall, out RaycastHit hit, wallCheckDistance, wallLayer, QueryTriggerInteraction.Ignore))
        {
            currentWallHit = hit;

            wallNormal = hit.normal;

            return true;
        }

        return false;
    }



    // Debugging Purposes

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Debug.DrawRay(transform.position, -transform.right * wallCheckDistance);
        Debug.DrawRay(transform.position, transform.right * wallCheckDistance);
    }

    private void OnCollisionStay(Collision collision)
    {
        // Should ignore anything unrelated to the wall layer.
        if ((wallLayer.value &(1 << collision.gameObject.layer)) == 0)
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact =
                collision.GetContact(i);

           
            float verticalNormal = Mathf.Abs(Vector3.Dot(contact.normal, Vector3.up));

            if (verticalNormal < 0.35f)
            {
                touchingRunnableWall = true;

                wallNormal = contact.normal;

                currentWallCollider = collision.collider;

                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider == currentWallCollider)
        {
            touchingRunnableWall = false;
            currentWallCollider = null;
        }
    }
}
    
