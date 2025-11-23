/*
 * Descricao: Lógica de interação com o Rádio.
 * O Rádio só pode ser ligado uma vez por dia.
 * Liga/Desliga a música principal e exibe uma mensagem de diálogo.
 */
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

    // --- (CAMPO CENTRAL: O diálogo que será mudado pelo DayCycleManager) ---
    [Header("Configuração do Diálogo")]
    [Tooltip("O diálogo do rádio, dividido em 'páginas'. O jogador apertará Espaço para avançar.")]
    [SerializeField] private string[] radioDialoguePages = { 
        "Dia 1...", 
        "...ninguém respondeu ainda...",
        "Vou tentar de novo amanhã."
    };
    
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

        // 1. ABAIXAR A MÚSICA AMBIENTE (do GameAudioManager)
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.DuckMainMusic();
        }

        // 2. TRAVAR O JOGADOR
        if (playerController != null) playerController.enabled = false;
        if (cameraLookController != null) cameraLookController.enabled = false;
        
        // 3. TOCAR O ÁUDIO DO DIÁLOGO (neste AudioSource 3D)
        audioSource.Stop(); // Para o som de "attention"
        audioSource.loop = false; // Diálogo não é loop
        if (radioDialogueClip != null)
        {
            audioSource.PlayOneShot(radioDialogueClip);
        }

        // 4. Chamar o DialogManager (SEM ENVIAR O ÁUDIO)
        bool dialogueFinished = false;
        
        DialogManager.Instance.ShowRadioDialog(
            radioDialoguePages, // Enviando o array de páginas
            radioImageSprite,
            () => { dialogueFinished = true; } // Callback
        );

        // 5. Esperar o DialogManager nos avisar que terminou
        yield return new WaitUntil(() => dialogueFinished);
        
        // 6. Atualizar o estado do rádio
        hasBeenUsedToday = true;
        StartAudioLoop(staticClip); // Agora, comece o loop de estática (em 3D)

        // 7. DESTRAVAR O JOGADOR
        if (playerController != null) playerController.enabled = true;
        if (cameraLookController != null) cameraLookController.enabled = true;

        // 8. RESTAURAR A MÚSICA AMBIENTE
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.RestoreMainMusicVolume();
        }
        
        isInteracting = false; 
    }

    // --- (MÉTODO REQUERIDO PELO DAYCYCLEMANAGER) ---
    /// <summary>
    /// Define o diálogo do rádio para a próxima interação.
    /// Chamado isso a partir do seu GameManager ou DayManager no início do dia.
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

    public void ForceSilenceRadio()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }
        // Desativa este script para impedir interação
        this.enabled = false;
    }
}