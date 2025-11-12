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

    // --- (NOVO: CAMPO DE TEXTO) ---
    [Header("Configuração do Diálogo")]
    [Tooltip("O texto que aparecerá na tela durante o diálogo do rádio.")]
    [TextArea(5, 10)]
    public string radioDialogueText = "Dia 1...\n...ninguém respondeu ainda...";
    
    [Header("Configuração dos Áudios")]
    public AudioClip attentionClip;
    public AudioClip radioDialogueClip;
    public AudioClip staticClip;

    [Header("Configuração da UI do Rádio")]
    public Sprite radioImageSprite;

    [Header("Referências de Controle (Do Player)")]
    [Tooltip("Arraste o script 'FirstPersonMovement' do seu Jogador para cá")]
    public MonoBehaviour playerController;
    [Tooltip("Arraste o script 'FirstPersonLook' (ou similar) da Câmera do Jogador")]
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
        
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            originalColor = objRenderer.material.color;
        }
    }

    void Start()
    {
        // Força o áudio de "ambiente" (atenção/estática) a ser 3D
        audioSource.spatialBlend = 1.0f; 
        
        // Começa o dia tocando o som de atenção
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
        
        // --- (MUDANÇA IMPORTANTE: ENVIANDO O TEXTO) ---
        DialogManager.Instance.ShowRadioDialog(
            radioDialogueText, // O novo parâmetro de texto
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