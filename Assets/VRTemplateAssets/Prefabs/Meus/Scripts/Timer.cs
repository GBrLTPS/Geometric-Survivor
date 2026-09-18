using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class TimerController : MonoBehaviour
{
    [Header("Configuração de Fases")]
    public string menuSceneName = "Menu";        // cena do menu
    public string treinoSceneName = "FaseTreino"; // cena de treino que deve ser ignorada

    private TextMeshProUGUI timerText;   // Texto do timer (nas fases)
    private TextMeshProUGUI placarText;  // Texto do placar (apenas no menu)

    private float timer = 0f;
    private bool isCounting = false;

    void Awake()
    {
        // Garante que só exista um TimerManager
        if (FindObjectsByType<TimerController>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        if (isCounting && timerText != null)
        {
            timer += Time.deltaTime;
            int minutes = Mathf.FloorToInt(timer / 60);
            int seconds = Mathf.FloorToInt(timer % 60);

            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Cena carregada: " + scene.name);

        // Sempre tenta encontrar os textos pelo nome
        GameObject textoObj = GameObject.Find("TimerText");
        if (textoObj != null)
            timerText = textoObj.GetComponent<TextMeshProUGUI>();

        GameObject placarObj = GameObject.Find("PlacarText");
        if (placarObj != null)
            placarText = placarObj.GetComponent<TextMeshProUGUI>();

        // Ignora apenas a cena de treino (zera e para)
        if (scene.name == treinoSceneName)
        {
            Debug.Log("Cena de treino detectada, timer zerado e ignorado.");
            StopTimer();
            timer = 0f;
            return;
        }

        // Se for Menu: para e salva
        if (scene.name == menuSceneName)
        {
            StopTimer();
            Debug.Log("Salvando tempo: " + timer);
            SalvarTempo(timer);
            MostrarPlacar();
        }
        else
        {
            // Se for Fase1, sempre resetar
            if (scene.name == "Fase1")
            {
                timer = 0f;
                Debug.Log("Entrou na Fase1, timer resetado.");
            }

            // Qualquer outra cena (fase de jogo) continua contando
            StartTimer();
        }
    }

    public void ResetarRunAtual()
    {
        timer = 0f;       // zera o contador da run
        isCounting = false; // opcional: para de contar até começar de novo
        if (timerText != null)
            timerText.text = "00:00"; // atualiza o texto na tela
    }

    public void StartTimer()
    {
        isCounting = true;
    }

    public void StopTimer()
    {
        isCounting = false;
    }

    private void SalvarTempo(float tempo)
    {
        if (tempo <= 0f)
        {
            Debug.Log("Tempo não salvo porque é 0.");
            return;
        }

        // Carrega tempos antigos válidos
        List<float> tempos = new List<float>();
        for (int i = 0; i < 5; i++)
        {
            if (PlayerPrefs.HasKey("Tempo" + i))
            {
                float t = PlayerPrefs.GetFloat("Tempo" + i);
                if (t > 0f) tempos.Add(t);
                else PlayerPrefs.DeleteKey("Tempo" + i); // apaga tempos zerados
            }
        }

        // Adiciona o último tempo válido
        tempos.Add(tempo);

        // Ordena do menor para o maior (melhores tempos primeiro)
        tempos = tempos.OrderBy(t => t).Take(5).ToList();

        // Salva novamente
        for (int i = 0; i < tempos.Count; i++)
        {
            PlayerPrefs.SetFloat("Tempo" + i, tempos[i]);
            Debug.Log($"Tempo {i} salvo: {tempos[i]}");
        }

        PlayerPrefs.Save();
    }

    private void MostrarPlacar()
    {
        if (placarText == null) return;

        placarText.text = "Top Tempos:\n";

        for (int i = 0; i < 5; i++)
        {
            if (PlayerPrefs.HasKey("Tempo" + i))
            {
                float tempo = PlayerPrefs.GetFloat("Tempo" + i);
                int minutes = Mathf.FloorToInt(tempo / 60);
                int seconds = Mathf.FloorToInt(tempo % 60);
                placarText.text += $"{i + 1}. {minutes:00}:{seconds:00}\n";
            }
        }

        // Mostra também o último tempo
        int lastMinutes = Mathf.FloorToInt(timer / 60);
        int lastSeconds = Mathf.FloorToInt(timer % 60);
        placarText.text += $"\nÚltimo Tempo: {lastMinutes:00}:{lastSeconds:00}";
    }

    // 🔥 Detecta colisão com objeto chamado ResetTimer (opcional)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "ResetTimer")
        {
            Debug.Log("Colisão com ResetTimer detectada. Apagando tempos...");
            ResetarTempos();
        }
    }

    // 🔥 Público para ser chamado pelo Bullet
    public void ResetarTempos()
    {
        for (int i = 0; i < 5; i++)
        {
            if (PlayerPrefs.HasKey("Tempo" + i))
                PlayerPrefs.DeleteKey("Tempo" + i);
        }

        PlayerPrefs.Save();

        if (placarText != null)
            placarText.text = "Top Tempos:\nNenhum tempo salvo ainda.";
    }
}