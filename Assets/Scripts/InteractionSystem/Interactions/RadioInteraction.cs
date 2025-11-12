using UnityEngine;
using System.Collections;
using DialogSystem; 

[RequireComponent(typeof(AudioSource))]
public class RadioInteraction : MonoBehaviour, IInteractable
{
    [Header("Lógica do Rádio")]
    [TextArea(3, 5)]
    public string staticMessage = "Apenas barulho de estática...";
    public float staticMessageDuration = 2.0f;

    // --- (A GRANDE MUDANÇA) ---
    [Header("Configuração do Diálogo")]
    [Tooltip("O diálogo do rádio, dividido em 'páginas'. O jogador apertará Espaço para avançar.")]
    // --- (MUDANÇA 1: "private" e "SerializeField") ---
    // Agora é privado, mas ainda visível no Inspector para testes.
    // Use o novo método SetDailyDialogue para mudar isso via código.
    [SerializeField] private string[] radioDialoguePages = { 
        "Dia 1...", 
        "...ninguém respondeu ainda...",
        "Vou tentar de novo amanhã."
    };
    // --- FIM DA MUDANÇA ---
    
    [Header("Configuração dos Áudios")]
    public AudioClip attentionClip;
    public AudioClip radioDialogueClip;
    public AudioClip staticClip;

    [Header("Configuração da UI do Rádio")]
    public Sprite radioImageSprite;

    [Header("Referências de Controle (Do Player)")]
    public MonoBehaviour playerController;
    public MonoBehaviour cameraLookController;

    [Header("Configuração de Efeito (Highlight)")]
    public Color highlightColor = Color.yellow;
    
    private Renderer objRenderer;
    private Color originalColor;
    
    private AudioSource audioSource;
    private bool hasBeenUsedToday = false;
    private bool isInteracting = false; 

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // (Vou deixar a configuração de áudio 3D aqui, mesmo que não funcione ainda,
        // pois ela não quebra nada e será útil no futuro)
        audioSource.spatialBlend = 1.0f; 
        audioSource.minDistance = 1.0f;  
        audioSource.maxDistance = 15.0f; 
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            originalColor = objRenderer.material.color;
        }
    }

    void Start()
    {
        StartAudioLoop(attentionClip);
    }

    private void StartAudioLoop(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    // --- Implementação da Interface IInteractable ---

    public void Interact()
    {
        if (isInteracting) return;
        
        if (hasBeenUsedToday)
        {
            DialogManager.Instance.ShowMessage(staticMessage, staticMessageDuration);
        }
        else
        {
            StartCoroutine(RadioDialogueSequence());
        }
    }

    private IEnumerator RadioDialogueSequence()
    {
        isInteracting = true; 

        // 1. TRAVAR O JOGADOR
        if (playerController != null) playerController.enabled = false;
        if (cameraLookController != null) cameraLookController.enabled = false;
        
        // 2. Parar o áudio 3D (o som de "atenção")
        audioSource.Stop();

        // 3. Chamar o DialogManager
        bool dialogueFinished = false;
        
        // --- (MUDANÇA IMPORTANTE: ENVIANDO O ARRAY) ---
        DialogManager.Instance.ShowRadioDialog(
            radioDialoguePages, // Enviando o array de páginas
            radioImageSprite,
            radioDialogueClip,
            () => { dialogueFinished = true; } // Callback
        );

        // 4. Esperar o DialogManager nos avisar que terminou
        yield return new WaitUntil(() => dialogueFinished);
        
        // 5. Atualizar o estado do rádio
        hasBeenUsedToday = true;
        StartAudioLoop(staticClip); // Agora, comece o loop de estática (em 3D)

        // 6. DESTRAVAR O JOGADOR
        if (playerController != null) playerController.enabled = true;
        if (cameraLookController != null) cameraLookController.enabled = true;
        
        isInteracting = false; 
    }

    // --- (NOVO MÉTODO - Pedido 1) ---
    /// <summary>
    /// Define o diálogo do rádio para a próxima interação.
    /// Chame isso a partir do seu GameManager ou DayManager no início do dia.
    /// </summary>
    /// <param name="newPages">O novo array de strings do diálogo.</param>
    public void SetDailyDialogue(string[] newPages)
    {
        this.radioDialoguePages = newPages;
    }
    // --- FIM DA MUDANÇA ---

    public void OnFocus()
    {
        if (objRenderer != null)
            objRenderer.material.color = highlightColor;
    }

    public void OnLoseFocus()
    {
        if (objRenderer != null)
            objRenderer.material.color = originalColor;
    }
    
    public void ResetForNewDay()
    {
        hasBeenUsedToday = false;
        isInteracting = false;
        StartAudioLoop(attentionClip);
    }
}