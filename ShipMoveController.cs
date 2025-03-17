using UnityEngine;
using System.Collections.Generic;  

public enum ShipClass
{
    Small,   // Fast, tight turning
    Medium,  // Balanced
    Large    // Slow, wide turning
}

public enum TeamColor
{
    Friendly,  // Green team
    Enemy      // Red team
}

[System.Serializable]
public struct Hardpoint
{
    public Vector2 position;
    public HardpointSize size;
}

public enum HardpointSize
{
    Small,
    Medium,
    Large
}

[System.Serializable]
public class ShipStats
{
    public float maxSpeed;          // Max forward speed
    public float acceleration;      // Acceleration rate
    public float deceleration;      // Natural deceleration
    public float reverseMaxSpeed;   // Max reverse speed
    public float turnAcceleration;  // Turn force
    public float mass;              // Affects momentum

    public ShipStats(float maxSpd, float accel, float decel, float revSpd, float turn, float shipMass)
    {
        maxSpeed = maxSpd;
        acceleration = accel;
        deceleration = decel;
        reverseMaxSpeed = revSpd;
        turnAcceleration = turn;
        mass = shipMass;
    }
}

public class ShipMoveController : MonoBehaviour
{
        // Remove duplicate declarations, keep only one of each
    [SerializeField] private ShipClass shipClass = ShipClass.Medium;
    private ShipStats stats;
    private float currentSpeed = 0f;
    private Vector2 velocity;
    private float turnRate = 0f;
    private const float STOP_THRESHOLD = 0.1f;
    public Vector2 wakeForwardOffset = new Vector2(0, -1f);
    public Vector2 wakeReverseOffset = new Vector2(0, 1f);
    private bool isReversing = false;
    private GameObject wakeObject;
    [SerializeField] private Transform wakeTransform;
    [SerializeField] private RuntimeAnimatorController wakeAnimController;
    [SerializeField] private TeamColor teamColor = TeamColor.Friendly;
    
    // Ship sprites
    [SerializeField] private Sprite smallShipGreen;
    [SerializeField] private Sprite mediumShipGreen;
    [SerializeField] private Sprite largeShipGreen;
    [SerializeField] private Sprite smallShipGreenDestroyed;
    [SerializeField] private Sprite mediumShipGreenDestroyed;
    [SerializeField] private Sprite largeShipGreenDestroyed;
    [SerializeField] private Sprite smallShipRed;
    [SerializeField] private Sprite mediumShipRed;
    [SerializeField] private Sprite largeShipRed;
    [SerializeField] private Sprite smallShipRedDestroyed;
    [SerializeField] private Sprite mediumShipRedDestroyed;
    [SerializeField] private Sprite largeShipRedDestroyed;

    [Header("Hardpoints")]
    [SerializeField] private Hardpoint[] smallShipHardpoints;
    [SerializeField] private Hardpoint[] mediumShipHardpoints;
    [SerializeField] private Hardpoint[] largeShipHardpoints;
    
    [Header("Weapon Base Sprites")]
    [SerializeField] private Sprite smallGunBase;
    [SerializeField] private Sprite mediumGunBase;
    [SerializeField] private Sprite largeGunBase;
    
    private List<GameObject> activeHardpoints = new List<GameObject>();

    
    // Components and state
    private Vector3 movement;
    private Rigidbody2D playerRigidbody;
    private SpriteRenderer spriteRenderer;
    private Vector3 previousMovement;
    private float currentRotation;

    void Awake() 
    {
        // Get components from ship
        playerRigidbody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Setup Rigidbody2D
        if (playerRigidbody != null) 
        {
            playerRigidbody.bodyType = RigidbodyType2D.Kinematic;
            playerRigidbody.gravityScale = 0f;
            playerRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            // Set correct sprite based on ship class and team
            spriteRenderer.sprite = (teamColor == TeamColor.Friendly) 
                ? shipClass switch
                {
                    ShipClass.Small => smallShipGreen,
                    ShipClass.Medium => mediumShipGreen,
                    ShipClass.Large => largeShipGreen,
                    _ => mediumShipGreen
                }
                : shipClass switch
                {
                    ShipClass.Small => smallShipRed,
                    ShipClass.Medium => mediumShipRed,
                    ShipClass.Large => largeShipRed,
                    _ => mediumShipRed
                };
        }

        // Initialize ship class before creating wake
        InitializeShipClass();

        // Handle wake creation
        CreateWake();
        InitializeHardpoints();
    }

    private void InitializeHardpoints()
    {
        // Clear any existing hardpoints
        foreach (var hardpoint in activeHardpoints)
        {
            if (hardpoint != null)
            {
                Destroy(hardpoint);
            }
        }
        activeHardpoints.Clear();

        // Get the correct hardpoint array based on ship class
        Hardpoint[] currentHardpoints = shipClass switch
        {
            ShipClass.Small => smallShipHardpoints,
            ShipClass.Medium => mediumShipHardpoints,
            ShipClass.Large => largeShipHardpoints,
            _ => null
        };

        if (currentHardpoints == null) return;

        // Create hardpoint objects
        foreach (var hardpoint in currentHardpoints)
        {
            GameObject hardpointObj = new GameObject("Hardpoint");
            hardpointObj.transform.SetParent(transform, false);
            hardpointObj.transform.localPosition = hardpoint.position;

            // Add sprite renderer
            var renderer = hardpointObj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Ships";
            renderer.sortingOrder = 1; // Above ship

            // Set correct base sprite
            renderer.sprite = hardpoint.size switch
            {
                HardpointSize.Small => smallGunBase,
                HardpointSize.Medium => mediumGunBase,
                HardpointSize.Large => largeGunBase,
                _ => null
            };

            activeHardpoints.Add(hardpointObj);
        }
    }

    public void UpdateShipType(ShipClass newClass, TeamColor newTeam)
    {
        shipClass = newClass;
        teamColor = newTeam;
        
        // Update sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = (teamColor == TeamColor.Friendly) 
                ? shipClass switch
                {
                    ShipClass.Small => smallShipGreen,
                    ShipClass.Medium => mediumShipGreen,
                    ShipClass.Large => largeShipGreen,
                    _ => mediumShipGreen
                }
                : shipClass switch
                {
                    ShipClass.Small => smallShipRed,
                    ShipClass.Medium => mediumShipRed,
                    ShipClass.Large => largeShipRed,
                    _ => mediumShipRed
                };
        }
        
        // Reinitialize ship stats
        InitializeShipClass();
            InitializeHardpoints(); 

    }

    private void InitializeShipClass()
    {
        switch (shipClass)
        {
            case ShipClass.Small:
                stats = new ShipStats(
                    maxSpd: 5f,      
                    accel: 0.1f,      // Slower acceleration
                    decel: 0.05f,     // Much slower deceleration
                    revSpd: 2f,       
                    turn: 0.8f,       // DO NOT MODIFY TURN VALUES - Working correctly
                    shipMass: 1.0f    
                );
                break;

            case ShipClass.Medium:
                stats = new ShipStats(
                    maxSpd: 4f,       
                    accel: 0.08f,     // Slower acceleration
                    decel: 0.03f,     // Much slower deceleration
                    revSpd: 1.5f,     
                    turn: 0.6f,       // DO NOT MODIFY TURN VALUES - Working correctly
                    shipMass: 1.5f    
                );
                break;

            case ShipClass.Large:
                stats = new ShipStats(
                    maxSpd: 3f,       
                    accel: 0.05f,     // Slower acceleration
                    decel: 0.02f,     // Much slower deceleration
                    revSpd: 1f,       
                    turn: 0.4f,       // DO NOT MODIFY TURN VALUES - Working correctly
                    shipMass: 2.0f    
                );
                break;
        }
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Move(h, v);
        UpdateAnimator();
    }

    void UpdateAnimator() 
    {
        if (wakeObject != null)
        {
            var wakeAnim = wakeObject.GetComponent<Animator>();
            var wakeRenderer = wakeObject.GetComponent<SpriteRenderer>();
            
            if (wakeAnim != null && wakeRenderer != null)
            {
                float h = Input.GetAxisRaw("Horizontal");
                bool isMoving = Mathf.Abs(currentSpeed) > STOP_THRESHOLD;
                
                wakeRenderer.enabled = isMoving;
                
                if (isMoving)
                {
                    wakeAnim.SetFloat("Speed", Mathf.Abs(currentSpeed));
                    
                    // Handle wake position and base rotation for forward/reverse
                    if (currentSpeed < 0 && !isReversing)
                    {
                        wakeTransform.localPosition = new Vector2(0, 0.25f);
                        wakeTransform.localRotation = Quaternion.Euler(0, 0, 180);
                        isReversing = true;
                    }
                    else if (currentSpeed > 0 && isReversing)
                    {
                        wakeTransform.localPosition = new Vector2(0, -0.14f);
                        wakeTransform.localRotation = Quaternion.identity;
                        isReversing = false;
                    }
                    
                    // Handle horizontal flipping based on turn direction only
                    wakeRenderer.flipX = (h > 0); // flip on right turn, normal on left turn
                }
                else
                {
                    // Reset when stopped
                    wakeTransform.localPosition = new Vector2(0, -0.14f);
                    wakeTransform.localRotation = Quaternion.identity;
                    wakeRenderer.flipX = false;
                    isReversing = false;
                }
            }
        }
    }

    void Move(float h, float v)
    {
        if (h != 0)
        {
            float speedFactor = Mathf.Abs(currentSpeed) / stats.maxSpeed;
            float effectiveTurnForce = stats.turnAcceleration * (1 - (speedFactor * 0.8f));
            // Remove the h negation to fix turn direction
            float angularAccel = (h * effectiveTurnForce) / (stats.mass + speedFactor * 2);
            turnRate = Mathf.Lerp(turnRate, -angularAccel, Time.fixedDeltaTime); // Negative here instead
            
            transform.Rotate(0, 0, turnRate * Time.fixedDeltaTime);
        }
        else
        {
            // Heavier ships take longer to stop turning
            float turnDamp = 2f / stats.mass;
            turnRate = Mathf.Lerp(turnRate, 0, Time.fixedDeltaTime * turnDamp);
        }

        // Handle acceleration/deceleration
        if (v != 0)
        {
            if (v > 0)
            {
                // Forward acceleration affected by mass
                float effectiveAccel = stats.acceleration / stats.mass;
                currentSpeed = Mathf.Min(currentSpeed + (effectiveAccel * Time.fixedDeltaTime), stats.maxSpeed);
            }
            else
            {
                // Reverse acceleration affected by mass
                float effectiveAccel = stats.acceleration / stats.mass;
                currentSpeed = Mathf.Max(currentSpeed - (effectiveAccel * Time.fixedDeltaTime), -stats.reverseMaxSpeed);
            }
        }
        else
        {
            if (Mathf.Abs(currentSpeed) > STOP_THRESHOLD)
            {
                // Deceleration affected by mass
                float dragForce = (stats.deceleration / stats.mass) * Time.fixedDeltaTime;
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, dragForce);
            }
            else
            {
                currentSpeed = 0;
            }
        }

        // Calculate and apply movement
        velocity = transform.up * currentSpeed;
        Vector2 newPosition = playerRigidbody.position + (velocity * Time.fixedDeltaTime);
        playerRigidbody.MovePosition(newPosition);
    }

    void CreateWake()
    {
        if (Application.isPlaying)
        {
            if (wakeObject != null)
            {
                Destroy(wakeObject);
            }

            // Create wake object
            wakeObject = new GameObject("Wake");
            wakeTransform = wakeObject.transform;
            wakeTransform.SetParent(transform, false);
            wakeTransform.localPosition = wakeForwardOffset;

            // Add sprite renderer with correct sorting layer
            var wakeRenderer = wakeObject.AddComponent<SpriteRenderer>();
            wakeRenderer.sortingLayerName = "Ships";
            wakeRenderer.sortingOrder = -1; // Still behind the ship in the Ships layer

            // Add animator and assign controller
            var wakeAnim = wakeObject.AddComponent<Animator>();
            wakeAnim.runtimeAnimatorController = wakeAnimController;

            Debug.Log("Wake created with Ships sorting layer");
        }
    }
}