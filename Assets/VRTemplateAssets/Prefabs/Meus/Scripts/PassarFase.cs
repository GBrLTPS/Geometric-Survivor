using UnityEngine;
using TMPro;                  // Necessário para usar TextMeshProUGUI
using UnityEngine.SceneManagement;

public class Placar : MonoBehaviour
{
    public TextMeshProUGUI placarText;   // Referência ao texto na UI
    public string proximaCena;           // Nome da cena para carregar quando completar
    public int meta = 12;                // Quantos objetos precisam ser destruídos

    void Update()
    {
        // Atualiza o texto do placar
        placarText.text = "Total destruídos: " + GameGlobalVar.destroyCount + " / " + meta;

        // Se atingiu a meta, troca de cena
        if (GameGlobalVar.destroyCount >= meta && !string.IsNullOrEmpty(proximaCena))
        {
            // Zera o contador ANTES de trocar de cena
            GameGlobalVar.destroyCount = 0;

            SceneManager.LoadScene(proximaCena);
        }
    }
}