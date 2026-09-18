using UnityEngine;

public class ZoomSimple : MonoBehaviour
{
    [Header("Câmera da mira (arraste aqui)")]
    public Camera scopeCamera;

    [Header("Configuração")]
    public float minFOV = 3f;
    public float maxFOV = 18f;
    public float zoomStep = 3f;     // De quanto em quanto muda por clique
    public float clickDelay = 0.15f; // Pequeno delay para não ficar seco

    private float nextClickTime = 0f;

    public void ZoomIn()
    {
        if (Time.time < nextClickTime) return;
        nextClickTime = Time.time + clickDelay;

        if (scopeCamera == null)
        {
            Debug.LogWarning("ZoomIn chamado mas a câmera está NULL!");
            return;
        }

        scopeCamera.fieldOfView -= zoomStep;
        scopeCamera.fieldOfView = Mathf.Clamp(scopeCamera.fieldOfView, minFOV, maxFOV);

        Debug.Log("ZoomIn → FOV atual: " + scopeCamera.fieldOfView);
    }

    public void ZoomOut()
    {
        if (Time.time < nextClickTime) return;
        nextClickTime = Time.time + clickDelay;

        if (scopeCamera == null)
        {
            Debug.LogWarning("ZoomOut chamado mas a câmera está NULL!");
            return;
        }

        scopeCamera.fieldOfView += zoomStep;
        scopeCamera.fieldOfView = Mathf.Clamp(scopeCamera.fieldOfView, minFOV, maxFOV);

        Debug.Log("ZoomOut → FOV atual: " + scopeCamera.fieldOfView);
    }
}
