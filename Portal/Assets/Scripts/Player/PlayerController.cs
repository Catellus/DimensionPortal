using UnityEngine;
using System.Collections;

public class PlayerController : EntityMotor
{
    #region Variables

    //[HideInInspector] 
    public pl_CameraController camController;

    [Space, Header("Controller - Movement")]
    public bool  bUseDebugYMovement;      //DEBUG: Toggles gravity usage
    public float maxWalkSpeed     = 6.0f; //Entity's maximum horizontal velocity (meters per second)
    public float accelerationTime = 0.1f; //Time in seconds to reach top speed from zero.

    private Vector2 m_velocity;            //Velocity passed into EnityMotor.Move -- Stores desired velocity
    private float   m_horizontalInput;     //Raw player input for the local X-axis
    private float   m_currentMaxWalkSpeed; //Current maximum horizontal velocity -- This value is updated for crouching, running, etc.
    private float   m_smoothedXVelocity;   //Used in a SmoothDamp to smooth the horizontal velocity
    private float   m_smoothedYVelocity;   //DEBUG: only used for no gravity movement

    [Space, Header("Controller - Jumping")]
    public float maxJumpHeight = 3.5f;            //Highest the player can jump from the start position
    public float minJumpHeight = 0.5f;            //Lowest  the player can jump from the start position
    public float timeToMaxApex = 0.35f;           //Time in seconds it takes the player to reach the highest possible jump
    public float DEBUG_DownGravMultiplier = 1.7f; //Value multiplied by rising gravity to find falling gravity

    private float m_fallingGravity;  //Velocity added to the player's Y velocity when not rising in a jump
    private float m_risingGravity;   //Velocity added to the player's Y velocity when rising in a jump
    private float m_maxJumpVelocity; //Value set as the player's Y velocity to reach the MaxJumpHeight in TimeToJumpApex seconds.
    //private float m_minJumpVelocity; //Value set as the player's Y velocity to reach the MinJumpHeight
    private bool  m_bRising;         //True if the player is rising in their jump -- Used to determine the gravity acting upon the player

    #endregion

    public virtual void Awake()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(this.gameObject.tag))
        {
            if (go != this.gameObject)
                Destroy(this.gameObject);
        }
    }

    public override void Start()
    {
        base.Start();
        m_currentMaxWalkSpeed = maxWalkSpeed;
        DetermineJumpForces();
        if (!camController)
            camController = this.gameObject.GetComponent<pl_CameraController>();
    }

    //Determines the risingGravity and jumpVelocities based on jumpHeights and jumpTime
    private void DetermineJumpForces()
    {
        m_risingGravity   = -(2 * maxJumpHeight) / Mathf.Pow(timeToMaxApex, 2);
        m_maxJumpVelocity = Mathf.Abs(m_risingGravity) * timeToMaxApex;
        //m_minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(m_risingGravity) * minJumpHeight);
        m_fallingGravity  = m_risingGravity * DEBUG_DownGravMultiplier;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
            bUseDebugYMovement = !bUseDebugYMovement;
        HandleInput();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (cinfo.falling) //If the player is not grounded, determine if risingGravity or fallingGravity should be used
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
        if (Input.GetKeyDown(KeyCode.S)) OnCrouchPressed();
        if (Input.GetKeyUp  (KeyCode.S)) OnCrouchReleased();

        if (Input.GetKeyDown(KeyCode.Escape))
            print("QUIT"); //Figure out what Unity's quit game function is.
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
        if (m_velocity.y > 0 || cinfo.throughPlatform)
            return -1;
        return 0;
    }

    protected override void EntityPassedThroughPortal()
    {
        base.EntityPassedThroughPortal();
        camController.UpdateSettings(cinfo.worldIndex);
    }

    #region Input Functions

    // JUMP
    void OnJumpPressed()
    {
        if (!cinfo.falling)
        {
            m_velocity.y = m_maxJumpVelocity;
            m_bRising = true;
        }
    }
    void OnJumpReleased()
    {
        
    }

    // CROUCH & DROP
    void OnCrouchPressed()
    {
        cinfo.crouching = true;
    }
    void OnCrouchReleased()
    {
        cinfo.crouching = false;
    }

    public float throughPlatformTime = 0.15f;

    IEnumerator PassThroughPermeable()
    {
        cinfo.throughPlatform = true;
        yield return new WaitForSeconds(throughPlatformTime);
        cinfo.throughPlatform = false;
    }

    #endregion //Input Functions

}
