using UnityEngine;
using TMPro;

public class Respawn : MonoBehaviour
{
    public float respawnTime = 5f;

    [Header("Sons")]
    public AudioClip hitSound;       // som a tocar quando atingido
    public float hitVolume = 1f;     // volume do som de impacto (pode passar de 1)

    public TextMeshProUGUI placarText; // referência ao texto na UI

    private AudioSource audioSource;

    void Start()
    {
        // Garante que existe um AudioSource no objeto
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        // toca som de hit sempre que for atingido
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, collision.contacts[0].point, hitVolume);
        }
    }

    public void DestroyTarget()
    {
        // Conta destruição
        GameGlobalVar.destroyCount++;
        Debug.Log("Total destruídos: " + GameGlobalVar.destroyCount);

        // Desativa objeto e agenda respawn
        gameObject.SetActive(false);
        Invoke(nameof(RespawnTarget), respawnTime);
    }

    private void RespawnTarget()
    {
        gameObject.SetActive(true);
    }

    void Update()
    {
        placarText.text = "Total destruídos: " + GameGlobalVar.destroyCount;
    }
}