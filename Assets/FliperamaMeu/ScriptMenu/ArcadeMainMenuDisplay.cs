using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ArcadeMainMenuDisplay : MonoBehaviour
{
    [Header("Interação do Menu (VR)")]
    [Tooltip("Arraste aqui o objeto que vai SUMIR quando entrar no menu e APARECER quando sair. (Ex: Raio do VR, arma, etc)")]
    public GameObject targetObjectToDisable;

    [Header("Nomes das Cenas")]
    [Tooltip("Nome da cena principal de gameplay")]
    public string gameSceneName = "Game";

    [Header("Paineis de UI")]
    public GameObject mainButtonsContainer;
    public GameObject optionsPanelContainer;

    [Header("Botões do Menu Principal")]
    [Tooltip("Botão 0: Jogar, Botão 1: Opções")]
    public List<ArcadeMenuButtonUI> mainButtons = new List<ArcadeMenuButtonUI>();

    [Header("Botões do Submenu de Opções")]
    [Tooltip("Botão 0: Mutar Áudio, Botão 1: Voltar")]
    public List<ArcadeMenuButtonUI> optionButtons = new List<ArcadeMenuButtonUI>();

    [Header("Sons e Áudio")]
    public AudioSource audioSource;
    public AudioClip clickSound;

    [Header("Inputs de Navegação Interna")]
    [Tooltip("Inputs para navegar DEPOIS que o menu estiver ativado")]
    public InputAction navigateAction = new InputAction("Navigate", InputActionType.Value, expectedControlType: "Vector2");
    public InputAction selectAction = new InputAction("Select", InputActionType.Button);
    public InputAction backAction = new InputAction("Back", InputActionType.Button);

    // Estados do Menu
    private bool isInteracting = false; // True = Mexendo no menu | False = Fora do menu
    private bool isInOptions = false;
    private int selectedIndex = 0;
    private float inputCooldown = 0.20f;
    private float nextInputTime = 0f;
    private bool isMuted = false;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        navigateAction.Enable();
        selectAction.Enable();
        backAction.Enable();
    }

    private void OnDisable()
    {
        navigateAction.Disable();
        selectAction.Disable();
        backAction.Disable();
    }

    private void Start()
    {
        // Garante estado inicial dos painéis
        if (mainButtonsContainer != null) mainButtonsContainer.SetActive(true);
        if (optionsPanelContainer != null) optionsPanelContainer.SetActive(false);

        selectedIndex = 0;
        UpdateHighlight();

        // Garante que o jogo comece no estado "False" (não interagindo com o menu)
        SetInteractionState(false);
    }

    private void Update()
    {
        // Só permite navegar no menu SE isInteracting for TRUE (ou seja, se clicou na tela)
        if (!isInteracting) return;

        HandleMenuNavigation();

        // Botão B (Controle) volta do menu de opções para o principal
        if (isInOptions && backAction.WasPressedThisFrame())
        {
            CloseOptions();
        }
    }

    // =====================================================================
    // CHAME ESSA FUNÇÃO NO EVENTO DE CLIQUE DO SEU RAYCAST/PONTEIRO VR
    // =====================================================================
    public void ToggleInteraction()
    {
        SetInteractionState(!isInteracting);
    }

    // Função que aplica a lógica visual e trava/destrava os comandos
    private void SetInteractionState(bool state)
    {
        isInteracting = state;

        // Ativa/Desativa o objeto desejado (Se isInteracting é true, objeto fica false, e vice-versa)
        if (targetObjectToDisable != null)
        {
            targetObjectToDisable.SetActive(!isInteracting);
        }

        // Se acabou de entrar no menu, reseta a seleção para o primeiro botão
        if (isInteracting)
        {
            selectedIndex = 0;
            isInOptions = false; // Garante que comece no menu principal
            
            if (mainButtonsContainer != null) mainButtonsContainer.SetActive(true);
            if (optionsPanelContainer != null) optionsPanelContainer.SetActive(false);
            
            UpdateHighlight();
        }
    }

    #region -- Navegação entre Botões --

    private void HandleMenuNavigation()
    {
        Vector2 navInput = navigateAction.ReadValue<Vector2>();

        float inputVal = Mathf.Abs(navInput.y) > 0.5f ? -navInput.y : navInput.x;

        List<ArcadeMenuButtonUI> currentButtons = isInOptions ? optionButtons : mainButtons;

        // Movimento do destaque de seleção
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

        // Confirmação com o botão configurado para selecionar
        bool selectPressed = selectAction.WasPressedThisFrame();

        if (selectPressed)
        {
            PlaySound(clickSound);
            ConfirmSelection();
        }
    }

    private void UpdateHighlight()
    {
        List<ArcadeMenuButtonUI> currentButtons = isInOptions ? optionButtons : mainButtons;

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
        if (!isInOptions)
        {
            // Menu Principal: 0 = Jogar | 1 = Opções
            if (selectedIndex == 0)
            {
                StartGame();
            }
            else if (selectedIndex == 1)
            {
                OpenOptions();
            }
        }
        else
        {
            // Submenu de Opções: 0 = Mutar Áudio | 1 = Voltar
            if (selectedIndex == 0)
            {
                ToggleMute();
            }
            else if (selectedIndex == 1)
            {
                CloseOptions();
            }
        }
    }

    #endregion

    #region -- Ações dos Botões --

    public void StartGame()
    {
        // Ao clicar em JOGAR, saímos da interação e carregamos a cena
        SetInteractionState(false);

        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OpenOptions()
    {
        isInOptions = true;
        selectedIndex = 0;

        if (mainButtonsContainer != null) mainButtonsContainer.SetActive(false);
        if (optionsPanelContainer != null) optionsPanelContainer.SetActive(true);

        UpdateHighlight();
    }

    public void CloseOptions()
    {
        isInOptions = false;
        selectedIndex = 1; // Retorna com o destaque no botão de 'Opções'

        if (optionsPanelContainer != null) optionsPanelContainer.SetActive(false);
        if (mainButtonsContainer != null) mainButtonsContainer.SetActive(true);

        UpdateHighlight();
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        AudioListener.pause = isMuted;
        AudioListener.volume = isMuted ? 0f : 1f;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null && !isMuted)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion
}