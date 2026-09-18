using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GlockConfettiShooter : MonoBehaviour
{
    public ParticleSystem confettiEffect;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    public GameObject Projetil;       // Prefab da bolinha
    public Transform pontaArma;       // Ponta da arma
    public float bulletSpeed = 375f;  // Velocidade inicial em m/s

    [Header("Som do Tiro")]
    public AudioClip tiroSom;         // Som do disparo
    private AudioSource audioSource;  // Componente de áudio

    [Range(0f, 1f)]
    public float shotVolume = 1f;     // Volume ajustado pelo slider

    void Awake()
    {
        // pega o componente de interação da Glock
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // conecta o evento de "apertar gatilho" ao método OnShoot
        grabInteractable.activated.AddListener(OnShoot);

        // pega ou adiciona um AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        // aplica volume inicial
        audioSource.volume = shotVolume;
    }

    void OnShoot(ActivateEventArgs args)
    {
        // dispara confete SEMPRE que atirar
        if (confettiEffect != null)
        {
            confettiEffect.Play();
        }

        // toca som do tiro
        if (tiroSom != null && audioSource != null)
        {
            audioSource.volume = shotVolume;  // <- volume atualizado
            audioSource.PlayOneShot(tiroSom);
        }

        // instancia o projétil
        GameObject bullet = Instantiate(Projetil, pontaArma.position, pontaArma.rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.useGravity = true;

        // aplica velocidade inicial
        rb.linearVelocity = pontaArma.forward * bulletSpeed;

        // aplica resistência do ar
        rb.linearDamping = 0.02f;        // desaceleração horizontal
        rb.angularDamping = 0.05f;       // padrão para rotação

        // destrói a bala após alguns segundos
        Destroy(bullet, 5f);
    }

    // Chamado pelo Slider no UI
    public void SetShotVolume(float v)
    {
        shotVolume = v;
    }
}
