using UnityEngine;
using UnityEngine.SceneManagement;

public class GunController : MonoBehaviour
{
    public GameObject sniper;       // referência para a sniper desativada na cena
    public int maxPistolShots = 10; // limite de balas da pistola (teste)

    private int pistolShots = 0;    // contador acumulado
    private int ultimoContado = 0;  // quantos já tinham sido vistos

    void Start()
    {
        // 🔥 sempre começa zerado quando a fase inicia
        pistolShots = 0;
        ultimoContado = 0;

        if (sniper != null)
            sniper.SetActive(false);

        Debug.Log("Contador zerado ao iniciar a fase.");
    }

    void Update()
    {
        // procura todos os clones de bala na cena (precisa que o prefab tenha a tag "Bala")
        GameObject[] balas = GameObject.FindGameObjectsWithTag("Bala");

        // se apareceram novas balas desde a última checagem
        int novos = balas.Length - ultimoContado;
        if (novos > 0)
        {
            pistolShots += novos;
            ultimoContado = balas.Length;

            Debug.Log("Disparos acumulados: " + pistolShots);

            if (pistolShots >= maxPistolShots)
                AtivarSniper();
        }

        // quando as balas somem, atualiza o último contado
        if (balas.Length < ultimoContado)
            ultimoContado = balas.Length;
    }

    void AtivarSniper()
    {
        if (sniper != null && !sniper.activeSelf)
        {
            sniper.SetActive(true);
            Debug.Log("Sniper ativada!");
        }
    }
}