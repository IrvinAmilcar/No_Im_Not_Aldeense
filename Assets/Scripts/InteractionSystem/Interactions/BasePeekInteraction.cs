/*
 * Descricao: Classe "mae" abstrata que contem TODA a lógica
 * de "espiar" (trocar camera, fade, highlight).
 * * --- VERSÃO FINAL ---
 * Adiciona um método público "SetMusicOverride" para controle externo
 * e mantém toda a lógica de diálogo original.
 */
using UnityEngine;
using System.Collections;
using DialogSystem;

// Esta classe implementa o contrato IInteractable
public abstract class BasePeekInteraction : MonoBehaviour, IInteractable
{
    [Header("Configuração do Peek")]
    public Camera playerCamera;
    public Camera targetViewCamera;

    [Header("Referências de Controle")]
    public MonoBehaviour playerController;
    public MonoBehaviour cameraLookController;

    [Header("Configuração de Efeito")]
    public float transitionSpeed = 2f;
    public Color highlightColor = Color.yellow;
    
    [Header("Configuração do Diálogo")]
    // --- NOVO CAMPO DE ID ---
    [Tooltip("ID único para o DayCycleManager identificar esta janela.")]
    public string windowID = "Janela_Principal";
    // ----------------------
    [Tooltip("As páginas de diálogo para mostrar ao espiar.")]
    [SerializeField] protected string[] dialoguePages;
    [Tooltip("Tempo em segundos para esperar antes de mostrar o diálogo")]
    public float dialogueStartDelay = 1.5f; 

    // --- VARIÁVEL DE ÁUDIO ---
    [Header("Configuração de Áudio Opcional")]
    [Tooltip("Valor padrão. Pode ser alterado via código por SetMusicOverride().")]
    [SerializeField] private bool overrideMusicOnPeek = false;
    // -----------------------------

    // Variáveis protegidas (acessíveis pelos "filhos")
    protected Renderer objRenderer;
    protected Color originalColor;
    protected bool isPeeking = false;
    protected bool isTransitioning = false;

    /// <summary>
    /// Permite que um sistema externo (como um GameManager) 
    /// decida se a PRÓXIMA interação de "peek" deve
    /// disparar a música especial.
    /// </summary>
    public void SetMusicOverride(bool shouldOverride)
    {
        this.overrideMusicOnPeek = shouldOverride;
    }

    /// <summary>
    /// Define o diálogo para a próxima interação de "peek".
    /// Chamado por um sistema externo (ex: DayCycleManager).
    /// </summary>
    /// <param name="newPages">O novo array de strings do diálogo.</param>
    public void SetDailyDialogue(string[] newPages)
    {
        this.dialoguePages = newPages;
    }

    // Start é "virtual"
    protected virtual void Start()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            originalColor = objRenderer.material.color;
        }

        if (targetViewCamera != null)
        {
            targetViewCamera.enabled = false;
        }
    }

    // --- Implementação dos Metodos IInteractable ---
    public virtual void Interact()
    {
        if (!isPeeking && !isTransitioning)
            StartPeeking();
    }
    public virtual void OnFocus()
    {
        if (objRenderer != null)
            objRenderer.material.color = highlightColor;
    }
    public virtual void OnLoseFocus()
    {
        if (objRenderer != null)
            objRenderer.material.color = originalColor;
    }

    // --- Logica Centralizada do Peek ---

    protected void StartPeeking()
    {
        StartCoroutine(SwitchToTargetCamera());
    }

    protected void StopPeeking()
    {
        if (!isPeeking || isTransitioning) return;

        isPeeking = false; 
        StartCoroutine(SwitchToPlayerCamera());
    }
    
    IEnumerator SwitchToTargetCamera()
    {
        isTransitioning = true;
        isPeeking = true; 
        if (playerController != null) playerController.enabled = false;
        if (cameraLookController != null) cameraLookController.enabled = false;

        // Se esta interação deve trocar a música, chama o manager
        if (overrideMusicOnPeek && GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.StartSpecialPeekMusic();
        }

        // 1. Fade out
        yield return StartCoroutine(CameraFader.Instance.Fade(1f, transitionSpeed));
        // 2. Troca a câmera
        playerCamera.enabled = false;
        targetViewCamera.enabled = true; 
        // 3. Fade in
        yield return StartCoroutine(CameraFader.Instance.Fade(0f, transitionSpeed));

        isTransitioning = false;

        // 4. Espera o delay
        yield return new WaitForSeconds(dialogueStartDelay);

        // 5. Verifica se temos algum diálogo VÁLIDO
        if (HasValidDialoguePages())
        {
            // 6. Chama o DialogManager e passa o "callback"
            DialogManager.Instance.ShowPeekDialogue(dialoguePages, () => {
                // 7. Callback: Quando o diálogo terminar, chame StopPeeking()
                StopPeeking();
            });
        }
        else
        {
            // 8. Comportamento antigo:
            // Se NÃO HÁ diálogo VÁLIDO, apenas espere o Espaço para fechar.
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
            StopPeeking();
        }
    }
    
    IEnumerator SwitchToPlayerCamera()
    {
        isTransitioning = true; 

        // Restaura a música principal ANTES do fade de volta
        if (overrideMusicOnPeek && GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.StopSpecialPeekMusic();
        }

        yield return StartCoroutine(CameraFader.Instance.Fade(1f, transitionSpeed));

        targetViewCamera.enabled = false; 
        playerCamera.enabled = true;

        if (playerController != null) playerController.enabled = true;
        if (cameraLookController != null) cameraLookController.enabled = true;

        yield return StartCoroutine(CameraFader.Instance.Fade(0f, transitionSpeed));

        isTransitioning = false;
    }

    /// <summary>
    /// Verifica se o array de diálogo não é nulo, não está vazio
    /// e contém pelo menos uma página que não é uma string vazia.
    /// </summary>
    private bool HasValidDialoguePages()
    {
        if (dialoguePages == null || dialoguePages.Length == 0)
        {
            return false; // Não tem array ou o array está vazio
        }

        // Verifica se TODAS as páginas são vazias ou nulas
        foreach (string page in dialoguePages)
        {
            if (!string.IsNullOrEmpty(page))
            {
                return true; // Encontrou uma página com texto!
            }
        }

        // Se saiu do loop, é porque o array existe mas só tem strings vazias
        return false;
    }
}