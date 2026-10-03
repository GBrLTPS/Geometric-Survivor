using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class ArcadeHUDDisplay : MonoBehaviour
{
    [Header("Barra de Vida (HP)")]
    [Tooltip("Slider da Barra de Vida")]
    public Slider healthSlider;
    [Tooltip("Texto opcional da vida (ex: 100 / 100)")]
    public TextMeshProUGUI healthText;

    [Header("Barra de Experiência (XP)")]
    [Tooltip("Slider da Barra de XP")]
    public Slider xpSlider;
    [Tooltip("Texto do XP (ex: 1 / 10 XP)")]
    public TextMeshProUGUI xpText;

    [Header("Nível")]
    [Tooltip("Texto do Nível (ex: LVL 1)")]
    public TextMeshProUGUI levelText;

    [Header("Paineis de UI (Pausa e Game Over)")]
    public GameObject pauseMenuContainer;
    public GameObject gameOverContainer;

    [Header("Nomes das Cenas")]
    [Tooltip("Nome da cena do Menu Principal caso escolha voltar ao menu")]
    public string mainMenuSceneName = "Menu";

    [Header("Botões do Menu de Pausa")]
    [Tooltip("Botão 0: Continuar, Botão 1: Voltar ao Menu")]
    public List<ArcadeMenuButtonUI> pauseButtons = new List<ArcadeMenuButtonUI>();

    [Header("Botões do Menu de Game Over")]
    [Tooltip("Botão 0: Tentar Novamente, Botão 1: Voltar ao Menu")]
    public List<ArcadeMenuButtonUI> gameOverButtons = new List<ArcadeMenuButtonUI>();

    [Header("Sons e Áudio")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    public AudioClip gameOverSound;

    [Header("Inputs (Pausa e Navegação)")]
    public InputAction pauseAction = new InputAction("Pause", InputActionType.Button);
    public InputAction navigateAction = new InputAction("Navigate", InputActionType.Value, expectedControlType: "Vector2");
    public InputAction selectAction = new InputAction("Select", InputActionType.Button);

    [Header("Seguir Câmera")]
    public Camera arcadeCamera;
    public float distanceFromCamera = 5f;

    // Estados do HUD / Menus
    private bool isPaused = false;
    private bool isGameOver = false;
    private int selectedIndex = 0;
    private float inputCooldown = 0.25f;
    private float nextInputTime = 0f;

    private ArcadeCharacterStats playerStats;
    private ArcadeLevelSystem levelSystem;
    private ArcadeCharacter2D playerCharacter;

    // Cache de velocidades para pausar rigidbodies 2D do jogo
    private Dictionary<Rigidbody2D, Vector2> pausedVelocities = new Dictionary<Rigidbody2D, Vector2>();

    private void Awake()
    {
        playerStats = FindFirstObjectByType<ArcadeCharacterStats>();
        levelSystem = FindFirstObjectByType<ArcadeLevelSystem>();
        playerCharacter = FindFirstObjectByType<ArcadeCharacter2D>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void OnEnable()
    {
        pauseAction.Enable();
        navigateAction.Enable();
        selectAction.Enable();

        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealthDisplay;
            playerStats.OnHealthChanged += CheckPlayerDeath;
        }

        if (levelSystem != null)
        {
            levelSystem.OnLevelUp += UpdateLevelDisplay;
            levelSystem.OnXPChanged += UpdateXPDisplay;
        }
    }

    private void OnDisable()
    {
        pauseAction.Disable();
        navigateAction.Disable();
        selectAction.Disable();

        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthDisplay;
            playerStats.OnHealthChanged -= CheckPlayerDeath;
        }

        if (levelSystem != null)
        {
            levelSystem.OnLevelUp -= UpdateLevelDisplay;
            levelSystem.OnXPChanged -= UpdateXPDisplay;
        }
    }

    private void Start()
    {
        if (arcadeCamera == null)
        {
            arcadeCamera = Camera.main;
        }

        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(false);
        if (gameOverContainer != null) gameOverContainer.SetActive(false);

        if (playerStats != null) UpdateHealthDisplay();
        if (levelSystem != null)
        {
            UpdateLevelDisplay(levelSystem.currentLevel);
            UpdateXPDisplay(levelSystem.currentXP, levelSystem.xpToNextLevel);
        }
    }

    private void Update()
    {
        // Alterna pausa quando aperta o botão de Pausa (se não estiver em Game Over)
        if (!isGameOver && pauseAction.WasPressedThisFrame())
        {
            TogglePause();
        }

        // Navegação por Joystick / Teclado se algum menu estiver aberto
        if (isPaused || isGameOver)
        {
            HandleMenuNavigation();
        }
    }

    private void LateUpdate()
    {
        if (transform.parent == null && arcadeCamera != null)
        {
            transform.position = arcadeCamera.transform.position + (arcadeCamera.transform.forward * distanceFromCamera);
            transform.rotation = arcadeCamera.transform.rotation;
        }
    }

    #region -- Atualizações do HUD --

    private void UpdateHealthDisplay()
    {
        if (playerStats == null) return;

        if (healthSlider != null)
        {
            healthSlider.maxValue = playerStats.maxHealth;
            healthSlider.value = playerStats.currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(playerStats.currentHealth)} / {Mathf.CeilToInt(playerStats.maxHealth)}";
        }
    }

    private void UpdateLevelDisplay(int newLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"LVL {newLevel}";
        }
    }

    private void UpdateXPDisplay(float currentXP, float maxXP)
    {
        if (xpSlider != null)
        {
            xpSlider.maxValue = maxXP;
            xpSlider.value = currentXP;
        }

        if (xpText != null)
        {
            xpText.text = $"{Mathf.FloorToInt(currentXP)} / {Mathf.CeilToInt(maxXP)} XP";
        }
    }

    #endregion

    #region -- Morte e Game Over --

    private void CheckPlayerDeath()
    {
        if (playerStats != null && playerStats.currentHealth <= 0 && !isGameOver)
        {
            TriggerGameOver();
        }
    }

    public void TriggerGameOver()
    {
        isGameOver = true;
        isPaused = false;
        selectedIndex = 0;

        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(false);
        if (gameOverContainer != null) gameOverContainer.SetActive(true);

        SetArcadeWorldPaused(true);
        PlaySound(gameOverSound);
        UpdateHighlight();
    }

    #endregion

    #region -- Sistema de Pausa --

    public void TogglePause()
    {
        if (isGameOver) return;

        isPaused = !isPaused;
        selectedIndex = 0;

        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(isPaused);

        SetArcadeWorldPaused(isPaused);
        PlaySound(clickSound);
        UpdateHighlight();
    }

    #endregion

    #region -- Navegação nos Menus (Joystick 2D / Teclado) --

    private void HandleMenuNavigation()
    {
        Vector2 navInput = navigateAction.ReadValue<Vector2>();

        if (navInput == Vector2.zero && Keyboard.current != null)
        {
            float v = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1 : 0) -
                      (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1 : 0);

            float h = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1 : 0) -
                      (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1 : 0);

            navInput = new Vector2(h, v);
        }

        float inputVal = Mathf.Abs(navInput.y) > 0.5f ? -navInput.y : navInput.x;

        List<ArcadeMenuButtonUI> currentButtons = isGameOver ? gameOverButtons : pauseButtons;

        if (Mathf.Abs(inputVal) > 0.5f && Time.unscaledTime >= nextInputTime)
        {
            int direction = (int)Mathf.Sign(inputVal);

            if (currentButtons.Count > 0)
            {
                selectedIndex = (selectedIndex + direction + currentButtons.Count) % currentButtons.Count;
                UpdateHighlight();
                PlaySound(clickSound);
                nextInputTime = Time.unscaledTime + inputCooldown;
            }
        }

        bool selectPressed = selectAction.WasPressedThisFrame() ||
                             (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame));

        if (selectPressed)
        {
            PlaySound(clickSound);
            ConfirmSelection();
        }
    }

    private void UpdateHighlight()
    {
        List<ArcadeMenuButtonUI> currentButtons = isGameOver ? gameOverButtons : pauseButtons;

        for (int i = 0; i < currentButtons.Count; i++)
        {
            if (currentButtons[i] != null)
            {
                currentButtons[i].SetSelected(i == selectedIndex);
            }
        }
    }

    private void ConfirmSelection()
    {
        if (isGameOver)
        {
            // Botões de Game Over
            if (selectedIndex == 0)
            {
                RestartCurrentScene();
            }
            else if (selectedIndex == 1)
            {
                LoadMainMenuScene();
            }
        }
        else if (isPaused)
        {
            // Botões de Pausa
            if (selectedIndex == 0)
            {
                TogglePause(); // Resume o jogo
            }
            else if (selectedIndex == 1)
            {
                LoadMainMenuScene();
            }
        }
    }

    #endregion

    #region -- Funções dos Botões / Troca de Cena --

    public void RestartCurrentScene()
    {
        SetArcadeWorldPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenuScene()
    {
        SetArcadeWorldPaused(false);
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region -- Congelamento/Descongelamento do Mundo 2D --

    private void SetArcadeWorldPaused(bool pause)
    {
        ArcadeEnemySpawner spawner = FindFirstObjectByType<ArcadeEnemySpawner>();
        if (spawner != null) spawner.enabled = !pause;

        if (playerCharacter != null)
        {
            playerCharacter.SetControlState(!pause);
        }

        if (pause)
        {
            pausedVelocities.Clear();

            ArcadeEnemy[] enemies = FindObjectsByType<ArcadeEnemy>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                enemy.enabled = false;
                Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    pausedVelocities[rb] = rb.linearVelocity;
                    rb.isKinematic = true;
                }
            }

            ArcadeProjectile[] bullets = FindObjectsByType<ArcadeProjectile>(FindObjectsSortMode.None);
            foreach (var bullet in bullets)
            {
                bullet.enabled = false;
                Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    pausedVelocities[rb] = rb.linearVelocity;
                    rb.isKinematic = true;
                }
            }

            if (playerCharacter != null)
            {
                Rigidbody2D playerRb = playerCharacter.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    pausedVelocities[playerRb] = playerRb.linearVelocity;
                    playerRb.isKinematic = true;
                }
            }
        }
        else
        {
            if (playerCharacter != null)
            {
                Rigidbody2D playerRb = playerCharacter.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.isKinematic = false;
                    if (pausedVelocities.ContainsKey(playerRb))
                        playerRb.linearVelocity = pausedVelocities[playerRb];
                }
            }

            ArcadeEnemy[] enemies = FindObjectsByType<ArcadeEnemy>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                enemy.enabled = true;
                Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    if (pausedVelocities.ContainsKey(rb))
                        rb.linearVelocity = pausedVelocities[rb];
                }
            }

            ArcadeProjectile[] bullets = FindObjectsByType<ArcadeProjectile>(FindObjectsSortMode.None);
            foreach (var bullet in bullets)
            {
                bullet.enabled = true;
                Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    if (pausedVelocities.ContainsKey(rb))
                        rb.linearVelocity = pausedVelocities[rb];
                }
            }

            pausedVelocities.Clear();
        }
    }

    #endregion
}