using UnityEngine;

public class CameraSeguir : MonoBehaviour
{
    [Header("Alvo")]
    [Tooltip("Arraste o quadrado (jogador) do fliperama para cá")]
    [SerializeField] private Transform alvo;

    [Header("Configurações de Movimento")]
    [Tooltip("Velocidade de amortecimento. Valores altos grudam mais rápido. Use 0 para movimento instantâneo.")]
    [SerializeField] private float suavidade = 5f;

    [Header("Eixos Ativos (Quais eixos a câmera vai seguir?)")]
    [SerializeField] private bool seguirX = true;
    [SerializeField] private bool seguirY = false;
    [SerializeField] private bool seguirZ = false;

    [Header("Distância Manual (Offsets)")]
    [Tooltip("Ajuste esses valores para afastar ou aproximar a câmera do objeto")]
    [SerializeField] private Vector3 distanciaOffset = new Vector3(0f, 0f, -10f);

    void LateUpdate()
    {
        if (alvo == null) return;

        // Pega a posição atual da própria câmera como base (caso o eixo não seja seguido)
        float novoX = transform.position.x;
        float novoY = transform.position.y;
        float novoZ = transform.position.z;

        // Se o eixo estiver ativo, ele calcula a posição do alvo + o offset manual
        if (seguirX) novoX = alvo.position.x + distanciaOffset.x;
        if (seguirY) novoY = alvo.position.y + distanciaOffset.y;
        if (seguirZ) novoZ = alvo.position.z + distanciaOffset.z;

        Vector3 posicaoAlvo = new Vector3(novoX, novoY, novoZ);

        // Se suavidade for maior que zero, move suavemente. Se for zero, teleporta instantaneamente.
        if (suavidade > 0)
        {
            transform.position = Vector3.Lerp(transform.position, posicaoAlvo, suavidade * Time.deltaTime);
        }
        else
        {
            transform.position = posicaoAlvo;
        }
    }
}
