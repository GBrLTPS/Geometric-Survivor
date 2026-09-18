using UnityEngine;

public class GlockResetToStart : MonoBehaviour
{
    [Header("Referência da Glock")]
    public Transform glock;

    [Header("Distância limite para reset")]
    public float resetThreshold = 1.5f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        // Salva a posição e rotação iniciais da Glock
        startPosition = glock.position;
        startRotation = glock.rotation;
    }

    void Update()
    {
        if (Vector3.Distance(glock.position, transform.position) > resetThreshold)
        {
            // Reposiciona a Glock para onde ela nasceu
            glock.position = startPosition;
            glock.rotation = startRotation;

            // Zera a física se houver Rigidbody
            Rigidbody rb = glock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}