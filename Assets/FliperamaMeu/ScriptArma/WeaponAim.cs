using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadeWeaponAim : MonoBehaviour
{
    [Header("Configuração de Inversão")]
    [Tooltip("Inverte a direção horizontal da mira")]
    public bool invertHorizontal = false;

    [Tooltip("Inverte a direção vertical da mira (cima/baixo)")]
    public bool invertVertical = false;

    [Tooltip("Ajuste fino de rotação caso a arma aponte para um lado diferente do esperado (ex: 0, 90, 180, -90)")]
    public float angleOffset = 0f;

    [Header("Pivô da Arma")]
    [Tooltip("O objeto que vai rodar (se vazio, usa este próprio GameObject)")]
    public Transform weaponPivot;

    [Header("Input do Analógico")]
    [Tooltip("Analógico usado para mirar (Value > Vector2)")]
    public InputAction aimAction = new InputAction("Aim", InputActionType.Value, expectedControlType: "Vector2");

    [Header("Suavização")]
    public float rotationSpeed = 25f;
    public float deadzone = 0.15f;

    private ArcadeCharacter2D character;

    private void Awake()
    {
        if (weaponPivot == null) weaponPivot = transform;
        character = GetComponentInParent<ArcadeCharacter2D>();
    }

    private void OnEnable()
    {
        aimAction.Enable();
    }

    private void OnDisable()
    {
        aimAction.Disable();
    }

    private void Update()
    {
        // Se o fliperama não estiver ativo, não atualiza a mira
        if (character != null && !character.isPlaying) return;

        // 1. Lê a direção do analógico
        Vector2 input = aimAction.ReadValue<Vector2>();

        // 2. Aplica as inversões se estiverem marcadas
        if (invertHorizontal) input.x *= -1f;
        if (invertVertical)   input.y *= -1f;

        // 3. Zona morta para não girar involuntariamente e manter a última direção
        if (input.sqrMagnitude < deadzone * deadzone) return;

        // 4. Calcula o ângulo em relação ao plano 2D (Eixo Z)
        float angle = (Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg) + angleOffset;

        // 5. Aplica a rotação no eixo Z (padrão 2D)
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);

        // 6. Rotação suave
        weaponPivot.localRotation = Quaternion.Slerp(weaponPivot.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}