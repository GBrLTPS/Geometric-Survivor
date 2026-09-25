using UnityEngine;
using UnityEngine.SceneManagement; // Necessário para trocar de cena
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;

[RequireComponent(typeof(XRSimpleInteractable), typeof(Collider))]
public class ArcadeCabinetInteractable : MonoBehaviour
{
    [Header("Troca de Cena & Mensagem")]
    [Tooltip("Deixe em branco se quiser apenas teleportar na mesma cena")]
    [SerializeField] private string sceneToLoad = "";

    [Tooltip("Mensagem ou ID customizado para log/processamento ao interagir")]
    [TextArea(2, 4)]
    [SerializeField] private string customMessage = "";

    [Header("Referências de Controle")]
    [SerializeField] private ArcadeCharacter2D arcadeCharacter;
    
    [Tooltip("Arraste o ContinuousMoveProvider do XR Origin")]
    [SerializeField] private ContinuousMoveProvider moveProvider;

    [Tooltip("Arraste o JumpProvider (do objeto Jump no XR Origin)")]
    [SerializeField] private JumpProvider jumpProvider;

    [Header("Virar no Joystick (XR Origin)")]
    [Tooltip("Arraste aqui o componente de Turn do XR Origin (Snap Turn / Continuous Turn)")]
    [SerializeField] private Behaviour turnProvider;

    [Header("Teleporte do Jogador (XR Origin)")]
    [Tooltip("Arraste o objeto raiz do XR Origin Hands (XR Rig)")]
    [SerializeField] private Transform xrOriginTransform;

    [Header("Pontos de Teleporte (Configurável)")]
    [Tooltip("Crie um Empty GameObject onde o jogador deve ir ao jogar o arcade e arraste aqui.")]
    [SerializeField] private Transform pcActivePoint;

    [Tooltip("Crie um Empty GameObject de onde o jogador deve voltar ao sair e arraste aqui.")]
    [SerializeField] private Transform originalPoint;

    [Header("Valores Manuais (Caso não use os Transforms acima)")]
    [SerializeField] private Vector3 pcActivePosition = new Vector3(211.998f, 18.337f, 47.183f);
    [SerializeField] private Vector3 pcActiveRotationEuler = new Vector3(0f, 65.5f, 0f);

    [Space(5)]
    [SerializeField] private Vector3 originalPosition = new Vector3(127.25f, 0.0f, 78.75f);
    [SerializeField] private Vector3 originalRotationEuler = new Vector3(0f, 88.1f, 0f);

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        if (arcadeCharacter == null)
        {
            arcadeCharacter = FindFirstObjectByType<ArcadeCharacter2D>();
        }

        if (xrOriginTransform == null)
        {
            GameObject xrRig = GameObject.Find("XR Origin Hands");
            if (xrRig != null) xrOriginTransform = xrRig.transform;
        }
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnScreenInteracted);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnScreenInteracted);
        }
    }

    private void OnScreenInteracted(SelectEnterEventArgs args)
    {
        ToggleGameControl();
    }

    public void ToggleGameControl()
    {
        if (arcadeCharacter == null) return;

#if UNITY_EDITOR
        // Limpa a seleção do Editor para evitar travamentos de SerializedObject
        UnityEditor.Selection.activeObject = null;
#endif

        // Exibe a mensagem personalizada no Console se ela estiver preenchida
        if (!string.IsNullOrEmpty(customMessage))
        {
            Debug.Log($"<color=cyan>[ArcadeCabinet]: {customMessage}</color>");
        }

        bool activate = !arcadeCharacter.isPlaying;

        // 1. Liga/Desliga o controle do personagem 2D
        arcadeCharacter.SetControlState(activate);

        // 2. Desativa/Ativa os controles de VR
        if (moveProvider != null) moveProvider.enabled = !activate;
        if (jumpProvider != null) jumpProvider.enabled = !activate;
        if (turnProvider != null) turnProvider.enabled = !activate;

        // 3. Executa o teleporte
        if (xrOriginTransform != null)
        {
            CharacterController cc = xrOriginTransform.GetComponent<CharacterController>();

            if (cc != null)
            {
                float maxAllowedStep = cc.height + (cc.radius * 2f);
                if (cc.stepOffset >= maxAllowedStep)
                {
                    cc.stepOffset = Mathf.Max(0.1f, maxAllowedStep - 0.1f);
                }

                cc.enabled = false; // Desativa o CharacterController antes de mover
            }

            if (activate)
            {
                // Entrou no fliperama
                if (pcActivePoint != null)
                {
                    xrOriginTransform.position = pcActivePoint.position;
                    xrOriginTransform.rotation = pcActivePoint.rotation;
                }
                else
                {
                    xrOriginTransform.position = pcActivePosition;
                    xrOriginTransform.rotation = Quaternion.Euler(pcActiveRotationEuler);
                }
            }
            else
            {
                // Saiu do fliperama
                if (originalPoint != null)
                {
                    xrOriginTransform.position = originalPoint.position;
                    xrOriginTransform.rotation = originalPoint.rotation;
                }
                else
                {
                    xrOriginTransform.position = originalPosition;
                    xrOriginTransform.rotation = Quaternion.Euler(originalRotationEuler);
                }
            }

            if (cc != null)
            {
                cc.enabled = true; // Reativa o CharacterController
            }
        }

        // 4. Executa a troca de cena (se o campo "Scene To Load" estiver preenchido)
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}