using UnityEngine;
using UnityEngine.SceneManagement;

public class Bullet : MonoBehaviour
{
    [Range(0, 100)]
    public float ricochetChance = 50f;   // porcentagem de chance de ricochetear
    public float speedLossFactor = 0.8f; // perda de velocidade após ricochete
    public int maxRicochets = 3;         // limite de ricochetes
    private int ricochetCount = 0;

    private Rigidbody rb;

    [Header("Som de Ricochete")]
    public AudioClip ricochetSound;      // Som do ricochete
    private AudioSource audioSource;     // Componente de áudio

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // pega ou adiciona um AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        GameObject atingido = collision.gameObject;

        // --- Se não era página, tenta carregar cena com nome do objeto ---
        // --- Se não era página, tenta carregar cena com nome do objeto ---
        if (CenaExiste(atingido.name))
        {
            // 🔥 Se for a cena "Menu", zera apenas o timer da run atual
            if (atingido.name == "Menu")
            {
                TimerController timerController = FindObjectOfType<TimerController>();
                if (timerController != null)
                {
                    timerController.ResetarRunAtual(); // zera o contador da run
                }
            }

            GameGlobalVar.destroyCount = 0;
            SceneManager.LoadScene(atingido.name);
            Destroy(gameObject);
            return;
        }

        // --- Se o objeto atingido estiver dentro de "Menus", tenta trocar de página ---
        MenuPaginas paginas = Object.FindFirstObjectByType<MenuPaginas>();
        if (paginas != null && atingido.transform.IsChildOf(paginas.transform))
        {
            bool paginaExiste = paginas.MostrarPagina(atingido.name);
            if (paginaExiste)
            {
                Destroy(gameObject);
                return; // só retorna se realmente era uma página
            }
        }

        // --- Se não era página, tenta carregar cena com nome do objeto ---
        if (CenaExiste(atingido.name))
        {
            GameGlobalVar.destroyCount = 0;
            SceneManager.LoadScene(atingido.name);
            Destroy(gameObject);
            return;
        }

        // --- Lógica de alvo ---
        Respawn target = atingido.GetComponent<Respawn>();
        if (target != null)
        {
            target.DestroyTarget();
        }

        // --- 🔥 ResetTimer: se a bala acertar o objeto chamado ResetTimer ---
        if (atingido.name == "ResetTimer")
        {
            Debug.Log("Bala acertou ResetTimer, apagando tempos...");
            TimerController timerController = FindObjectOfType<TimerController>();
            if (timerController != null)
            {
                timerController.ResetarTempos();
            }
        }

        // --- Chance de ricochete ---
        if (Random.Range(0f, 100f) <= ricochetChance && ricochetCount < maxRicochets)
        {
            rb.linearVelocity *= speedLossFactor;
            ricochetCount++;

            // toca som de ricochete
            if (ricochetSound != null)
            {
                AudioSource.PlayClipAtPoint(ricochetSound, collision.contacts[0].point);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    bool CenaExiste(string nomeCena)
    {
        int total = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < total; i++)
        {
            string caminho = SceneUtility.GetScenePathByBuildIndex(i);
            string nome = System.IO.Path.GetFileNameWithoutExtension(caminho);
            if (nome == nomeCena)
                return true;
        }
        return false;
    }
}