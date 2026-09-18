using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ArcadeCharacter2D : MonoBehaviour
{
    [Header("Configuração de Direção")]
    public bool invertMovement = false;

    [Header("Movimento Básico")]
    public float speed = 5f;
    public float jumpForce = 8f;

    [Header("Pulo na Parede (Wall Jump)")]
    [Tooltip("Força horizontal que arremessa o boneco para fora da parede")]
    public float wallJumpPushForce = 8f;
    [Tooltip("Força vertical aplicada durante o wall jump")]
    public float wallJumpUpForce = 8.5f;
    [Tooltip("Distância do raio de checagem de parede lateral (medida a partir da borda do colisor)")]
    public float wallCheckDistance = 0.2f;
    [Tooltip("Layer das paredes/obstáculos (NÃO selecione Everything)")]
    public LayerMask wallLayer;

    [Header("Pulo Duplo")]
    public int maxAirJumps = 1;
    private int airJumpsRemaining;
    private bool canGroundJump = false;
    private bool isWallJumping = false;

    [Header("Detector de Chão (Hitbox)")]
    public GroundTriggerDetector groundDetector;

    [Header("Dash")]
    public float dashForce = 14f;
    public float dashUpForce = 3f;
    public float dashDuration = 0.15f;
    private bool isDashing = false;
    private bool canDash = true;

    [Header("Ajustes de Gravidade")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Locomoção VR")]
    public ContinuousMoveProvider moveProvider;

    [Header("Inputs VR")]
    public InputAction moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
    public InputAction jumpAction = new InputAction("Jump", InputActionType.Button);
    public InputAction dashAction = new InputAction("Dash", InputActionType.Button);
    public InputAction exitAction = new InputAction("Exit", InputActionType.Button);

    [Header("Estado")]
    [SerializeField] private bool _isPlaying = false;
    public bool isPlaying => _isPlaying;

    private Rigidbody2D rb;
    private Collider2D col;
    private ArcadeCharacterStats stats;
    private float moveInput;
    private float lastMoveDirection = 1f;
    private bool jumpRequested;
    private bool dashRequested;

    private float CurrentSpeed => stats != null ? stats.moveSpeed : speed;
    private float CurrentJumpForce => stats != null ? stats.jumpForce : jumpForce;
    private int CurrentMaxAirJumps => stats != null ? stats.extraJumps : maxAirJumps;
    private float CurrentDashForce => stats != null ? stats.dashForce : dashForce;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        stats = GetComponent<ArcadeCharacterStats>();

        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (groundDetector == null)
        {
            groundDetector = GetComponentInChildren<GroundTriggerDetector>();
        }

        ApplyConstraints();
        ApplyFrictionlessMaterial();
    }

    private void Start()
    {
        SetControlState(false);
    }

    private void OnValidate()
    {
        if (rb == null && this != null) rb = GetComponent<Rigidbody2D>();
        if (col == null && this != null) col = GetComponent<Collider2D>();
        ApplyConstraints();
    }

    private void ApplyConstraints()
    {
        if (this == null || rb == null) return;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void ApplyFrictionlessMaterial()
    {
        PhysicsMaterial2D frictionless = new PhysicsMaterial2D("Frictionless_Arcade2D")
        {
            friction = 0f,
            bounciness = 0f
        };

        if (col != null) col.sharedMaterial = frictionless;
    }

    public void SetControlState(bool active)
    {
        _isPlaying = active;

        moveInput = 0f;
        jumpRequested = false;
        dashRequested = false;
        isWallJumping = false;

        if (active)
        {
            moveAction.Enable();
            jumpAction.Enable();
            dashAction.Enable();
            exitAction.Enable();
        }
        else
        {
            moveAction.Disable();
            jumpAction.Disable();
            dashAction.Disable();
            exitAction.Disable();

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }
    }

    private void Update()
    {
        if (!_isPlaying) return;

        // 1. Saída do fliperama
        if (exitAction.WasPressedThisFrame() || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            ExitArcadeMode();
            return;
        }

        // 2. Leitura de Inputs
        Vector2 input = moveAction.ReadValue<Vector2>();

        if (input == Vector2.zero && Keyboard.current != null)
        {
            float h = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1 : 0) -
                      (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1 : 0);
            input = new Vector2(h, 0);
        }

        moveInput = input.x * (invertMovement ? -1f : 1f);

        if (Mathf.Abs(moveInput) > 0.1f)
        {
            lastMoveDirection = Mathf.Sign(moveInput);
        }

        // 3. Pulo
        bool jumpPressed = jumpAction.WasPressedThisFrame() ||
                           (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

        if (jumpPressed)
        {
            jumpRequested = true;
        }

        // 4. Dash
        bool dashPressed = dashAction.WasPressedThisFrame() ||
                           (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame);

        if (dashPressed && canDash && !isDashing)
        {
            canDash = false;
            dashRequested = true;
        }
    }

    private void FixedUpdate()
    {
        UpdateGroundStatus();

        if (!_isPlaying)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            return;
        }

        if (isDashing) return;

        if (dashRequested)
        {
            dashRequested = false;
            StartCoroutine(PerformDash());
            return;
        }

        if (jumpRequested)
        {
            jumpRequested = false;
            HandleJump();
        }

        if (!isWallJumping && rb != null)
        {
            rb.linearVelocity = new Vector2(moveInput * CurrentSpeed, rb.linearVelocity.y);
        }

        ApplyBetterGravity();
    }

    private void HandleJump()
    {
        if (rb == null) return;

        // 1. Pulo do Chão
        if (canGroundJump)
        {
            canGroundJump = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, CurrentJumpForce);
            return;
        }

        // 2. Checagem de Parede Lateral
        int wallDir = CheckWallDirection();

        if (wallDir != 0)
        {
            StartCoroutine(PerformWallJump(-wallDir));
            return;
        }

        // 3. Pulo Duplo no Ar
        if (airJumpsRemaining > 0)
        {
            airJumpsRemaining--;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, CurrentJumpForce);
        }
    }

        private int CheckWallDirection()
    {
        // Se estiver no chão, desativa a checagem de parede para não dar o pulo infinito
        if (canGroundJump) return 0;
    
        float extentX = col != null ? col.bounds.extents.x : 0.5f;
        Vector2 origin = transform.position;
    
        Vector2 rightOrigin = origin + new Vector2(extentX + 0.05f, 0f);
        Vector2 leftOrigin = origin + new Vector2(-extentX - 0.05f, 0f);
    
        // Raio para a direita (+1)
        RaycastHit2D hitPos = Physics2D.Raycast(rightOrigin, Vector2.right, wallCheckDistance);
        if (hitPos.collider != null && !hitPos.collider.isTrigger && !hitPos.transform.IsChildOf(transform))
        {
            if (hitPos.collider.GetComponentInParent<ArcadeEnemy>() == null)
                return 1;
        }
    
        // Raio para a esquerda (-1)
        RaycastHit2D hitNeg = Physics2D.Raycast(leftOrigin, Vector2.left, wallCheckDistance);
        if (hitNeg.collider != null && !hitNeg.collider.isTrigger && !hitNeg.transform.IsChildOf(transform))
        {
            if (hitNeg.collider.GetComponentInParent<ArcadeEnemy>() == null)
                return -1;
        }
    
        return 0;
    }

    private IEnumerator PerformWallJump(float pushDirection)
    {
        isWallJumping = true;
        canDash = true;
        airJumpsRemaining = CurrentMaxAirJumps;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(pushDirection * wallJumpPushForce, wallJumpUpForce);
        }

        yield return new WaitForSeconds(0.18f);

        isWallJumping = false;
    }

    private void UpdateGroundStatus()
    {
        bool grounded = groundDetector != null && groundDetector.isGrounded;

        if (grounded)
        {
            canGroundJump = true;
            airJumpsRemaining = CurrentMaxAirJumps;
            canDash = true;
        }
        else
        {
            canGroundJump = false;
        }
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        float direction = Mathf.Abs(moveInput) > 0.1f ? Mathf.Sign(moveInput) : lastMoveDirection;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * CurrentDashForce, dashUpForce);
        }

        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
    }

    private void ApplyBetterGravity()
    {
        if (rb == null) return;

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime);
        }
        else if (rb.linearVelocity.y > 0 && !jumpAction.IsPressed() && (Keyboard.current == null || !Keyboard.current.spaceKey.isPressed))
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime);
        }
    }

    public void ExitArcadeMode()
    {
        SetControlState(false);

        if (moveProvider != null)
        {
            moveProvider.enabled = true;
        }
        Debug.Log("Saiu do fliperama.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        float extentX = col != null ? col.bounds.extents.x : 0.5f;
        Vector2 origin = transform.position;

        Vector2 rightOrigin = origin + new Vector2(extentX + 0.02f, 0f);
        Vector2 leftOrigin = origin + new Vector2(-extentX - 0.02f, 0f);

        Gizmos.DrawLine(rightOrigin, rightOrigin + Vector2.right * wallCheckDistance);
        Gizmos.DrawLine(leftOrigin, leftOrigin + Vector2.left * wallCheckDistance);
    }
}