using UnityEngine;
using System.Collections.Generic;
using System.Linq;  // Add this for LINQ extension methods

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
    public string id;
    public Vector2 position;
    public HardpointSize size;
    public string mountedWeaponId;
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
    [SerializeField] private ShipConfigurationSO smallShipConfig;
    [SerializeField] private ShipConfigurationSO mediumShipConfig;
    [SerializeField] private ShipConfigurationSO largeShipConfig;
    private ShipConfigurationSO currentConfig;
    
    [SerializeField] private TeamColor teamColor = TeamColor.Friendly;
    [SerializeField] private ShipClass shipClass = ShipClass.Medium;
    
    private const float STOP_THRESHOLD = 0.1f;
    
    [Header("Weapon Base Sprites")]
    [SerializeField] private Sprite smallGunBase;
    [SerializeField] private Sprite mediumGunBase;
    [SerializeField] private Sprite largeGunBase;

    // Keep only necessary runtime state
    private float currentSpeed = 0f;
    private Vector2 velocity;
    private float turnRate = 0f;
    private bool isReversing = false;
    private GameObject wakeObject;
    private Transform wakeTransform;
    private List<GameObject> activeHardpoints = new List<GameObject>();
    private Rigidbody2D playerRigidbody;
    private SpriteRenderer spriteRenderer;

    // Add these properties
    public ShipConfigurationSO SmallShipConfig => smallShipConfig;
    public ShipConfigurationSO MediumShipConfig => mediumShipConfig;
    public ShipConfigurationSO LargeShipConfig => largeShipConfig;

    private WebSocketClient networkClient;
    private bool isLocalPlayer;
    private Vector2 targetPosition;
    private float targetRotation;
    private const float INTERPOLATION_SPEED = 10f;

    private Dictionary<string, GameObject> otherShips = new Dictionary<string, GameObject>();
    private GameObject shipPrefab; // Assign in inspector

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        networkClient = FindAnyObjectByType<WebSocketClient>();
        
        if (networkClient != null)
        {
            networkClient.OnGameStateReceived += HandleGameState;
            isLocalPlayer = true; // Force true for testing
            Debug.Log("ShipMoveController initialized with NetworkClient");
        }
        else
        {
            Debug.LogError("NetworkClient not found in scene!");
        }

        // Initialize target position to current position
        targetPosition = transform.position;
    }

    private void UpdateConfiguration()
    {
        currentConfig = shipClass switch
        {
            ShipClass.Small => smallShipConfig,
            ShipClass.Medium => mediumShipConfig,
            ShipClass.Large => largeShipConfig,
            _ => mediumShipConfig
        };

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = teamColor == TeamColor.Friendly ? 
                currentConfig.friendlySprite : 
                currentConfig.enemySprite;
        }
    }

    public void InitializeHardpoints()
    {
        foreach (var hardpoint in activeHardpoints)
        {
            if (hardpoint != null) Destroy(hardpoint);
        }
        activeHardpoints.Clear();

        if (currentConfig.hardpoints == null) return;

        foreach (var hardpoint in currentConfig.hardpoints)
        {
            GameObject hardpointObj = new GameObject($"Hardpoint_{hardpoint.id}");
            hardpointObj.transform.SetParent(transform, false);
            hardpointObj.transform.localPosition = hardpoint.position;

            var controller = hardpointObj.AddComponent<HardpointController>();
            Sprite baseSprite = hardpoint.size switch
            {
                HardpointSize.Small => smallGunBase,
                HardpointSize.Medium => mediumGunBase,
                HardpointSize.Large => largeGunBase,
                _ => null
            };
            
            controller.Initialize(hardpoint, baseSprite);
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
                ? currentConfig.friendlySprite
                : currentConfig.enemySprite;
        }
        
        // Reinitialize ship stats
        UpdateConfiguration();
        InitializeHardpoints(); 
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (h != 0 || v != 0)
            {
                Debug.Log($"Input detected: h={h}, v={v}");
                if (networkClient != null)
                {
                    networkClient.SendPlayerInput(h, v);
                }
                else
                {
                    Debug.LogError("NetworkClient is null!");
                }
            }
        }

        // Only interpolate if target position is different from current
        if (Vector2.Distance(transform.position, targetPosition) > 0.001f)
        {
            Debug.Log($"Interpolating - Current: {transform.position}, Target: {targetPosition}, Distance: {Vector2.Distance(transform.position, targetPosition)}");
            transform.position = Vector2.Lerp(transform.position, targetPosition, Time.deltaTime * INTERPOLATION_SPEED);
        }

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
                        wakeTransform.localPosition = new Vector2(0, currentConfig.wakeReverseOffset.y);
                        wakeTransform.localRotation = Quaternion.Euler(0, 0, 180);
                        isReversing = true;
                    }
                    else if (currentSpeed > 0 && isReversing)
                    {
                        wakeTransform.localPosition = new Vector2(0, currentConfig.wakeForwardOffset.y);
                        wakeTransform.localRotation = Quaternion.identity;
                        isReversing = false;
                    }
                    
                    // Handle horizontal flipping based on turn direction only
                    wakeRenderer.flipX = (h > 0); // flip on right turn, normal on left turn
                }
                else
                {
                    // Reset when stopped
                    wakeTransform.localPosition = new Vector2(0, currentConfig.wakeForwardOffset.y);
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
            float speedFactor = Mathf.Abs(currentSpeed) / currentConfig.stats.maxSpeed;
            float effectiveTurnForce = currentConfig.stats.turnAcceleration * (1 - (speedFactor * 0.8f));
            // Remove the h negation to fix turn direction
            float angularAccel = (h * effectiveTurnForce) / (currentConfig.stats.mass + speedFactor * 2);
            turnRate = Mathf.Lerp(turnRate, -angularAccel, Time.fixedDeltaTime); // Negative here instead
            
            transform.Rotate(0, 0, turnRate * Time.fixedDeltaTime);
        }
        else
        {
            // Heavier ships take longer to stop turning
            float turnDamp = 2f / currentConfig.stats.mass;
            turnRate = Mathf.Lerp(turnRate, 0, Time.fixedDeltaTime * turnDamp);
        }

        // Handle acceleration/deceleration
        if (v != 0)
        {
            if (v > 0)
            {
                // Forward acceleration affected by mass
                float effectiveAccel = currentConfig.stats.acceleration / currentConfig.stats.mass;
                currentSpeed = Mathf.Min(currentSpeed + (effectiveAccel * Time.fixedDeltaTime), currentConfig.stats.maxSpeed);
            }
            else
            {
                // Reverse acceleration affected by mass
                float effectiveAccel = currentConfig.stats.acceleration / currentConfig.stats.mass;
                currentSpeed = Mathf.Max(currentSpeed - (effectiveAccel * Time.fixedDeltaTime), -currentConfig.stats.reverseMaxSpeed);
            }
        }
        else
        {
            if (Mathf.Abs(currentSpeed) > STOP_THRESHOLD)
            {
                // Deceleration affected by mass
                float dragForce = (currentConfig.stats.deceleration / currentConfig.stats.mass) * Time.fixedDeltaTime;
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
            wakeTransform.localPosition = new Vector2(0, currentConfig.wakeForwardOffset.y);

            // Add sprite renderer with correct sorting layer
            var wakeRenderer = wakeObject.AddComponent<SpriteRenderer>();
            wakeRenderer.sortingLayerName = "Wakes";
            wakeRenderer.sortingOrder = 0;

            // Add animator and assign correct controller based on ship size
            var wakeAnim = wakeObject.AddComponent<Animator>();
            wakeAnim.runtimeAnimatorController = currentConfig.wakeController;
        }
    }

    private void HandleGameState(string gameStateJson)
    {
        var gameState = JsonUtility.FromJson<GameStateData>(gameStateJson);
        
        foreach (var ship in gameState.ships)
        {
            if (ship.playerId == networkClient.PlayerId)
            {
                // Update local ship
                transform.position = new Vector3(ship.position.x, ship.position.y, 0);
                transform.rotation = Quaternion.Euler(0, 0, ship.rotation);
            }
            else
            {
                // Handle other ships
                if (!otherShips.ContainsKey(ship.playerId))
                {
                    // Create new ship instance
                    var newShip = Instantiate(shipPrefab, Vector3.zero, Quaternion.identity);
                    newShip.GetComponent<Renderer>().material.color = Color.red; // Mark as enemy
                    otherShips.Add(ship.playerId, newShip);
                }
                
                // Update ship position
                var otherShip = otherShips[ship.playerId];
                otherShip.transform.position = new Vector3(ship.position.x, ship.position.y, 0);
                otherShip.transform.rotation = Quaternion.Euler(0, 0, ship.rotation);
            }
        }

        // Remove disconnected ships
        var disconnectedShips = otherShips.Keys
            .Where(id => !gameState.ships.Any(s => s.playerId == id))
            .ToList();
            
        foreach (var id in disconnectedShips)
        {
            Destroy(otherShips[id]);
            otherShips.Remove(id);
        }
    }
}