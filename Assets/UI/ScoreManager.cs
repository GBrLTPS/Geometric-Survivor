using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static int destroyCount = 0; // contador global
    public TextMeshProUGUI scoreText;   // referência ao texto na UI

    void Update()
    {
        scoreText.text = "Alvos destruídos: " + destroyCount;
    }
}