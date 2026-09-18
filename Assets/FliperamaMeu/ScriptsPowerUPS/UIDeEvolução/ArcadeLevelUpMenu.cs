using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadeLevelUpMenu : MonoBehaviour
{
    [Header("Painel Principal do Menu")]
    public GameObject menuContainer;

    [Header("Cards de Upgrade na UI")]
    public List<UpgradeCardUI> cardSlots = new List<UpgradeCardUI>();

    [Header("Configuração de Opções")]
    public int optionsCount = 3;

    [Header("Inputs VR / Teclado")]
    public InputAction navigateAction = new InputAction("Navigate", InputActionType.Value, expectedControlType: "Vector2");
    public InputAction selectAction = new InputAction("Select", InputActionType.Button);

    private ArcadeLevelSystem levelSystem;
    private ArcadeCharacter2D playerCharacter;
    private int selectedIndex = 0;
    private bool isMenuOpen = false;
    private float inputCooldown = 0.25f;
    private float nextInputTime = 0f;

    // Cache para restaurar velocidades 2D pós-pausa
    private Dictionary<Rigidbody2D, Vector2> pausedVelocities = new Dictionary<Rigidbody2D, Vector2>();

    private void Awake()
    {
        levelSystem = FindFirstObjectByType<ArcadeLevelSystem>();
        playerCharacter = FindFirstObjectByType<ArcadeCharacter2D>();

        if (menuContainer != null)
        {
            menuContainer.SetActive(false);
        }
    }

    private void OnEnable()
    {
        navigateAction.Enable();
        selectAction.Enable();

        if (levelSystem != null)
        {
            levelSystem.OnLevelUp += OpenMenu;
        }
    }

    private void OnDisable()
    {
        navigateAction.Disable();
        selectAction.Disable();

        if (levelSystem != null)
        {
            levelSystem.OnLevelUp -= OpenMenu;
        }
    }

    public void OpenMenu(int newLevel)
    {
        if (levelSystem == null || levelSystem.availableUpgrades == null || levelSystem.availableUpgrades.Count == 0) return;

        isMenuOpen = true;
        selectedIndex = 0;

        // Congela o mundo do fliperama 2D
        SetArcadeWorldPaused(true);

        menuContainer.SetActive(true);

        List<UpgradeData> randomPicks = GetRandomUpgrades(optionsCount);

        for (int i = 0; i < cardSlots.Count; i++)
        {
            if (i < randomPicks.Count)
            {
                cardSlots[i].gameObject.SetActive(true);
                cardSlots[i].Setup(randomPicks[i]);
            }
            else
            {
                cardSlots[i].gameObject.SetActive(false);
            }
        }

        UpdateCardHighlight();
    }

    private void SetArcadeWorldPaused(bool pause)
    {
        // 1. Pausa/Despausa o Spawner
        ArcadeEnemySpawner spawner = FindFirstObjectByType<ArcadeEnemySpawner>();
        if (spawner != null) spawner.enabled = !pause;

        // 2. Trava controles do player
        if (playerCharacter != null)
        {
            playerCharacter.SetControlState(!pause);
        }

        // 3. Congela ou restaura os Rigidbodies 2D (Inimigos, Player, Projéteis)
        if (pause)
        {
            pausedVelocities.Clear();

            // Salva e congela inimigos
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

            // Salva e congela projéteis
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

            // Congela o Rigidbody 2D do Player
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
            // Restaura o Player
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

            // Restaura Inimigos
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

            // Restaura Projéteis
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

    private List<UpgradeData> GetRandomUpgrades(int count)
    {
        List<UpgradeData> pool = new List<UpgradeData>(levelSystem.availableUpgrades);
        List<UpgradeData> selected = new List<UpgradeData>();

        int amountToPick = Mathf.Min(count, pool.Count);

        for (int i = 0; i < amountToPick; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            selected.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        return selected;
    }

    private void Update()
    {
        if (!isMenuOpen) return;

        Vector2 navInput = navigateAction.ReadValue<Vector2>();

        if (navInput == Vector2.zero && Keyboard.current != null)
        {
            float h = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1 : 0) -
                      (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1 : 0);
            navInput = new Vector2(h, 0);
        }

        if (Mathf.Abs(navInput.x) > 0.5f && Time.unscaledTime >= nextInputTime)
        {
            int direction = (int)Mathf.Sign(navInput.x);
            int activeCardsCount = GetActiveCardsCount();

            if (activeCardsCount > 0)
            {
                selectedIndex = (selectedIndex + direction + activeCardsCount) % activeCardsCount;
                UpdateCardHighlight();
                nextInputTime = Time.unscaledTime + inputCooldown;
            }
        }

        bool selectPressed = selectAction.WasPressedThisFrame() ||
                             (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame));

        if (selectPressed)
        {
            ConfirmSelection();
        }
    }

    private int GetActiveCardsCount()
    {
        int count = 0;
        foreach (var card in cardSlots)
        {
            if (card.gameObject.activeSelf) count++;
        }
        return count;
    }

    private void UpdateCardHighlight()
    {
        int activeIndex = 0;
        for (int i = 0; i < cardSlots.Count; i++)
        {
            if (cardSlots[i].gameObject.activeSelf)
            {
                cardSlots[i].SetSelected(activeIndex == selectedIndex);
                activeIndex++;
            }
        }
    }

    private void ConfirmSelection()
    {
        int activeIndex = 0;
        UpgradeData chosenUpgrade = null;

        for (int i = 0; i < cardSlots.Count; i++)
        {
            if (cardSlots[i].gameObject.activeSelf)
            {
                if (activeIndex == selectedIndex)
                {
                    chosenUpgrade = cardSlots[i].currentUpgrade;
                    break;
                }
                activeIndex++;
            }
        }

        if (chosenUpgrade != null && levelSystem != null)
        {
            levelSystem.ApplyUpgradeToPlayer(chosenUpgrade);
        }

        CloseMenu();
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        menuContainer.SetActive(false);

        // Despausa todo o mundo do fliperama mantendo as velocidades originais
        SetArcadeWorldPaused(false);
    }
}