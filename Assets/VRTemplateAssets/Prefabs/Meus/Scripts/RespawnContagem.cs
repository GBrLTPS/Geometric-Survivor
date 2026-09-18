using UnityEngine;
using TMPro;

public class RespawnContagem : MonoBehaviour
{
    public float respawnTime = 5f;

    [Header("Sons")]
    public AudioClip hitSound;
    public float hitVolume = 1f;

    public TextMeshProUGUI placarText;

    public int maxAlvos = 15; // número total de alvos

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, collision.contacts[0].point, hitVolume);
        }

        DestroyTarget();
    }

    public void DestroyTarget()
    {
        GameGlobalVar.destroyCount++;
        Debug.Log("Total destruídos: " + GameGlobalVar.destroyCount);

        gameObject.SetActive(false);
        Invoke(nameof(RespawnTarget), respawnTime);
    }

    private void RespawnTarget()
    {
        GameGlobalVar.destroyCount = Mathf.Max(0, GameGlobalVar.destroyCount - 1);
        gameObject.SetActive(true);
    }

    void Update()
    {
        placarText.text = "Alvos destruídos: " + GameGlobalVar.destroyCount + "/" + maxAlvos;
    }
}