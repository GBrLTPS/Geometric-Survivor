using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ArcadeMainMenu : MonoBehaviour
{
    [Header("Paineis da UI")]
    public GameObject menuContainer;
    public GameObject optionsPanel;

    [Header("Botoes do Menu Principal (Na Ordem)")]
    [Tooltip("Arraste aqui os componentes dos seus botoes do Menu Principal na ordem de navegacao (ex: 0 = Jogar, 1 = Opcoes)")]
    public List<ArcadeMenuButtonUI> mainButtons = new List<ArcadeMenuButtonUI>();

    [Header("Botoes do Menu de Opcoes (Na Ordem)")]
    [Tooltip("Arraste aqui os botoes do menu de opcoes (ex: 0 = Voltar)")]
    public List<ArcadeMenuButtonUI> optionsButtons = new List<ArcadeMenuButtonUI>();

    [Header("Configuracao de Cena / Jogo")]
    [Tooltip("Nome da cena que sera carregada ao clicar em Jogar. Se for na mesma cena, deixe vazio.")]
    public string gameSceneName = "";

    [Header("Inputs VR / Teclado (Mesmo esquema do Level Up)")]
    public InputAction navigateAction = new InputAction("Navigate", InputActionType.Value, expectedControlType: "Vector2");
    public InputAction selectAction = new InputAction("Select", InputActionType.Button);

    private int selectedIndex = 0;
    private bool isMenuOpen = true;
    private bool isInOptionsMenu = false;
    private float inputCooldown = 0.25f;
    private float nextInputTime = 0f;

    private ArcadeCharacter2D playerCharacter;

    private void Awake()
    {
        playerCharacter = FindFirstObjectByType<ArcadeCharacter2D>();
    }

    private void OnEnable()
    {
        navigateAction.Enable();
        selectAction.Enable();
    }

    private void OnDisable()
    {
        navigateAction.Disable();
        selectAction.Disable();
    }

    private void Start()
    {
        OpenMainMenu();
    }

    public void OpenMainMenu()
    {
        isMenuOpen = true;
        isInOptionsMenu = false;
        selectedIndex = 0;

        if (menuContainer != null) menuContainer.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        // Bloqueia movimentacao e acoes do jogo 2D enquanto estiver no menu
        if (playerCharacter != null)
        {
            playerCharacter.SetControlState(false);
        }

        UpdateHighlight();
    }

    public void OpenOptionsMenu()
    {
        isInOptionsMenu = true;
        selectedIndex = 0;

        if (menuContainer != null) menuContainer.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);

        UpdateHighlight();
    }

    private void Update()
    {
        if (!isMenuOpen) return;

        // 1. Leitura de navegação (Joystick / Analógico VR / Teclado)
        Vector2 navInput = navigateAction.ReadValue<Vector2>();

        if (navInput == Vector2.zero && Keyboard.current != null)
        {
            float v = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1 : 0) -
                      (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1 : 0);

            float h = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1 : 0) -
                      (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1 : 0);

            navInput = new Vector2(h, v);
        }

        // Navega para cima/baixo ou esquerda/direita
        float inputVal = Mathf.Abs(navInput.y) > 0.5f ? -navInput.y : navInput.x;

        if (Mathf.Abs(inputVal) > 0.5f && Time.unscaledTime >= nextInputTime)
        {
            int direction = (int)Mathf.Sign(inputVal);
            List<ArcadeMenuButtonUI> currentList = isInOptionsMenu ? optionsButtons : mainButtons;

            if (currentList.Count > 0)
            {
                selectedIndex = (selectedIndex + direction + currentList.Count) % currentList.Count;
                UpdateHighlight();
                nextInputTime = Time.unscaledTime + inputCooldown;
            }
        }

        // 2. Confirmação de Ação
        bool selectPressed = selectAction.WasPressedThisFrame() ||
                             (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame));

        if (selectPressed)
        {
            ConfirmSelection();
        }
    }

    private void UpdateHighlight()
    {
        List<ArcadeMenuButtonUI> currentList = isInOptionsMenu ? optionsButtons : mainButtons;

        for (int i = 0; i < currentList.Count; i++)
        {
            if (currentList[i] != null)
            {
                currentList[i].SetSelected(i == selectedIndex);
            }
        }
    }

    private void ConfirmSelection()
    {
        if (!isInOptionsMenu)
        {
            // Opções do Menu Principal
            if (selectedIndex == 0)
            {
                StartGame();
            }
            else if (selectedIndex == 1)
            {
                OpenOptionsMenu();
            }
        }
        else
        {
            // Opções do Menu de Opções
            if (selectedIndex == 0) // Botão Voltar
            {
                OpenMainMenu();
            }
        }
    }

    public void StartGame()
    {
        isMenuOpen = false;

        if (menuContainer != null) menuContainer.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        // Se houver uma cena diferente configurada, carrega a cena
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        // Libera o controle do personagem 2D para jogar
        if (playerCharacter != null)
        {
            playerCharacter.SetControlState(true);
        }
    }
}