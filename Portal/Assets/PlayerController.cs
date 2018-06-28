using UnityEngine;

public class PlayerController : EntityMotor
{
    #region Variables

    Vector2 m_velocity;
    float m_walkInput;

    [Space, Header("Controller - Movement")]
    public float maxWalkSpeed = 6;
    float m_curMaxWalkSpeed;

    public float accelerationTime = 0.1f;
    float m_smoothedVelocity;

    [Space, Header("Controller - Jumping")]
    public float maxJumpHeight = 3.5f;
    public float minJumpHeight = 0.5f;
    public float timeToMaxApex = 0.35f;
    public float DEBUG_DownGravMultiplier = 1.7f;
    float m_gravity;
    float m_jumpGravity;
    float m_maxJumpVelocity;
    float m_minJumpVelocity;
    bool  m_rising;

    [Space, Header("Controller - Crouch")]
    public bool crouching;
    public float crouchHeightPercent = 0.5f;

    [Space, Header("Controller - Cameras")]
    public CamerasController cam;

    #endregion

    public override void Start()
    {
        base.Start();
        m_curMaxWalkSpeed = maxWalkSpeed;
        DetermineJumpForces();
    }

    private void DetermineJumpForces()
    {
        m_jumpGravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToMaxApex, 2);
        m_maxJumpVelocity = Mathf.Abs(m_jumpGravity) * timeToMaxApex;
        m_minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(m_jumpGravity) * minJumpHeight);
        m_gravity = m_jumpGravity * DEBUG_DownGravMultiplier;
    }

    private void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        if (cinfo.isFalling)
            CheckJumpApexReached();

        DetermineVelocity();
        Move(m_velocity * Time.fixedDeltaTime);

        if (cinfo.above || cinfo.below)
            m_velocity.y = 0;
    }

    void HandleInput()
    {
        m_walkInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetButtonDown("Jump")) OnJumpPressed();
        if (Input.GetButtonUp  ("Jump")) OnJumpReleased();
        if (Input.GetButtonDown("Fire")) OnFirePressed();
        if (Input.GetButtonUp  ("Fire")) OnFireReleased();
    }

    void CheckJumpApexReached()
    {
        if (m_rising && m_velocity.y <= 0)
            m_rising = false;
    }

    float m_smoothedYVelocity;

    void DetermineVelocity()
    {
        if (!crouching)
        {
            float targetXVel = m_walkInput * m_curMaxWalkSpeed;
            m_velocity.x = Mathf.SmoothDamp(m_velocity.x, targetXVel, ref m_smoothedVelocity, accelerationTime);
            m_velocity.y = Mathf.SmoothDamp(m_velocity.y, Input.GetAxisRaw("Vertical") * m_curMaxWalkSpeed, ref m_smoothedYVelocity, accelerationTime);
            //m_velocity.y = Input.GetAxisRaw("Vertical") * m_curMaxWalkSpeed;
            //m_velocity.y += ((m_rising) ? m_jumpGravity : m_gravity) * Time.fixedDeltaTime;
        }
    }

    protected override int HandlePermeablePlatform()
    {
        if (cinfo.throughPlatform)
            return -1;

        return 0;
    }

    protected override void EntityPassedThroughPortal()
    {
        cam.ChangeDimension(cinfo.inA);
    }


    #region Input Functions
    void OnJumpPressed()  { m_velocity.y = m_maxJumpVelocity; m_rising = true; }
    void OnJumpReleased() { }

    bool placingPortal;

    public GameObject portalSphere;
    PortalBallController ptlSphere;

    void OnFirePressed()
    {
        if (ptlSphere == null)
            placingPortal = false;

        if (placingPortal)
        {
            ptlSphere.SetPortal();
            placingPortal = false;
        }
        else
        {
            Vector2 direction = ((Vector2)cam.CamA.ScreenToWorldPoint(Input.mousePosition) - (Vector2)this.transform.position).normalized;
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)direction);

            Quaternion rot = Quaternion.FromToRotation(Vector2.right, direction);
            ptlSphere = Instantiate(portalSphere, this.transform.position, rot).GetComponent<PortalBallController>();

            ptlSphere.SyncSettings(cinfo, collisionMask, portal);
            //ptlSphere.SyncSettings(cinfo, collisionMask, portal);
            placingPortal = true;
        }
    }
    void OnFireReleased() { }

    #endregion
}
