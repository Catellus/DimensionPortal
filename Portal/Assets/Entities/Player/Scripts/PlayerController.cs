using UnityEngine;

public class PlayerController : EntityMotor
{
    #region Variables


    [Space, Header("Controller - Movement")]
    public bool  bUseDebugYMovement;      //DEBUG: Toggles gravity usage
    public float maxWalkSpeed     = 6.0f; //Entity's maximum horizonal velocity (meters per second)
    public float accelerationTime = 0.1f; //Time in seconds to reach top speed from zero.

    private Vector2 m_velocity;            //Velocity passed into EnityMotor.Move -- Stores desired velocity
    private float   m_horizontalInput;     //Raw player input for the local X-axis
    private float   m_currentMaxWalkSpeed; //Current maximum horizontal velocity -- This value is updated for crouching, running, etc.
    private float   m_smoothedXVelocity;   //Used in a SmoothDamp to smooth the horizontal velocity
    private float   m_smoothedYVelocity;   //DEBUG: only used for no gravity movement

    [Space, Header("Controller - Jumping")]
    public float maxJumpHeight = 3.5f;  //Highest the player can jump from the start position
    public float minJumpHeight = 0.5f;  //Lowest  the player can jump from the start position
    public float timeToMaxApex = 0.35f; //Time in seconds it takes the player to reach the highest possible jump
    public float DEBUG_DownGravMultiplier = 1.7f; //Value muliplied by rising gravity to find falling gravity

    private float m_fallingGravity;  //Velocity added to the player's Y velocity when not rising in a jump
    private float m_risingGravity;   //Velocity added to the player's Y velocity when rising in a jump
    private float m_maxJumpVelocity; //Value set as the player's Y velocity to reach the MaxJumpHeight in TimeToJumpApex seconds.
    private float m_minJumpVelocity; //Value set as the player's Y velocity to reach the MinJumpHeight
    private bool  m_bRising;         //True if the player is rising in their jump -- Used to determine the gravity acting upon the player

    [Space, Header("Controller - Cameras")]
    public CamerasController cameraController; //Cameras showing the A and B versions of the current level

    [Space, Header("Controller - Portal")]
    public GameObject portalSphere_prefab; //Prefab spawned by the player used to move and rotate the portal

    private bool        m_bPlacingPortal; //Used to determine to spawn a portalSphere or relocate the portal
    private PortalSphere m_portalSphere;  //Reference to a portalSphere spawned by the player

    #endregion

    public override void Start()
    {
        base.Start();
        m_currentMaxWalkSpeed = maxWalkSpeed;
        DetermineJumpForces();
    }

    //Determines the risingGravity and jumpVelocities based on jumpHeights and jumpTime
    private void DetermineJumpForces()
    {
        m_risingGravity   = -(2 * maxJumpHeight) / Mathf.Pow(timeToMaxApex, 2);
        m_maxJumpVelocity = Mathf.Abs(m_risingGravity) * timeToMaxApex;
        m_minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(m_risingGravity) * minJumpHeight);
        m_fallingGravity  = m_risingGravity * DEBUG_DownGravMultiplier;
    }

    private void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        if (cinfo.isFalling) //If the player is not grounded, determine if risingGravity or fallingGravity should be used
            CheckJumpApexReached();

        DetermineVelocity();                    //Smooths horizontal velocity and adds gravity
        Move(m_velocity * Time.fixedDeltaTime); 

        if (cinfo.above || cinfo.below) //Prevents the Y velocity from building up when grounded
            m_velocity.y = 0;
    }

    void HandleInput()
    {
        m_horizontalInput = Input.GetAxisRaw("Horizontal"); //Sets desired horizontal velocity right as player presses button
        if (Input.GetButtonDown("Jump")) OnJumpPressed();
        if (Input.GetButtonUp  ("Jump")) OnJumpReleased();
        if (Input.GetButtonDown("Fire")) OnFirePressed();
        if (Input.GetButtonUp  ("Fire")) OnFireReleased();
    }

    void CheckJumpApexReached()
    {
        if (m_bRising && m_velocity.y <= 0)
            m_bRising = false;
    }

    void DetermineVelocity()
    {
        float _targetXVelocity = m_horizontalInput * m_currentMaxWalkSpeed; //Gets the target horizontal velocity

        //Lerps between current velocity and target velocity -- Creates a smooth change in velocity
        m_velocity.x = Mathf.SmoothDamp(m_velocity.x, _targetXVelocity, ref m_smoothedXVelocity, accelerationTime);

        if (bUseDebugYMovement)
        {
            float targetYVelocity = Input.GetAxisRaw("Vertical") * m_currentMaxWalkSpeed;
            m_velocity.y = Mathf.SmoothDamp(m_velocity.y, targetYVelocity, ref m_smoothedYVelocity, accelerationTime);
        }
        else
            m_velocity.y += ((m_bRising) ? m_risingGravity : m_fallingGravity) * Time.fixedDeltaTime; //Adds gravity (less gravity when rising in jump)
    }

    protected override int HandlePermeablePlatform()
    {
        if (cinfo.throughPlatform)
            return -1;

        return 0;
    }

    protected override void EntityPassedThroughPortal()
    {
        cameraController.ChangeDimension(cinfo.inA);
    }


    #region Input Functions

    // JUMP
    void OnJumpPressed()
    {
        if (!cinfo.isFalling)
        {
            m_velocity.y = m_maxJumpVelocity;
            m_bRising = true;
        }
    }
    void OnJumpReleased()
    {
        
    }

    // FIRE PORTAL
    void OnFirePressed()
    {
        if (m_portalSphere == null)
            m_bPlacingPortal = false;

        if (m_bPlacingPortal) //If a portalSphere exists, set the portal's new location
        {
            m_portalSphere.SetPortal();
            m_bPlacingPortal = false;
        }
        else //If a portalSphere does not exist, spawn one
        {
            //TODO: Add controller support for this action
            //Gets the direction from the player to the cursor.
            Vector2 direction = ((Vector2)cameraController.playerWorldCam.ScreenToWorldPoint(Input.mousePosition) - (Vector2)this.transform.position).normalized;

            //Gets the rotation of the direction and spawns a new portalSphere
            Quaternion rotation = Quaternion.FromToRotation(Vector2.right, direction);
            m_portalSphere = Instantiate(portalSphere_prefab, this.transform.position, rotation).GetComponent<PortalSphere>();

            //Ensures the portalSphere is using the correct collision and has a reference to the portal to move
            m_portalSphere.SyncSettings(cinfo, collisionMask, portal);
            m_bPlacingPortal = true;
        }
    }
    void OnFireReleased()
    {
        
    }

    #endregion //Input Functions

}
