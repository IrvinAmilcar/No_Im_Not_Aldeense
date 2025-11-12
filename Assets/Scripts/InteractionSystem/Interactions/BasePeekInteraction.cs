/*
 * Descricao: Classe "mae" abstrata que contem TODA a lógica
 * de "espiar" (trocar camera, fade, highlight).
 */
using UnityEngine;
using System.Collections;
using DialogSystem; // <-- Mantenha isso

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
    [Tooltip("As páginas de diálogo para mostrar ao espiar.")]
    [SerializeField] protected string[] dialoguePages;
    [Tooltip("Tempo em segundos para esperar antes de mostrar o diálogo")]
    public float dialogueStartDelay = 1.5f; 

    // Variáveis protegidas (acessíveis pelos "filhos")
    protected Renderer objRenderer;
    protected Color originalColor;
    protected bool isPeeking = false;
    protected bool isTransitioning = false;

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

        // --- (AQUI ESTÁ A CORREÇÃO 1) ---
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
        // --- FIM DA CORREÇÃO 1 ---
    }
    
    IEnumerator SwitchToPlayerCamera()
    {
        isTransitioning = true; 

        yield return StartCoroutine(CameraFader.Instance.Fade(1f, transitionSpeed));

        targetViewCamera.enabled = false; 
        playerCamera.enabled = true;

        if (playerController != null) playerController.enabled = true;
        if (cameraLookController != null) cameraLookController.enabled = true;

        yield return StartCoroutine(CameraFader.Instance.Fade(0f, transitionSpeed));

        isTransitioning = false;
    }

    // --- (NOVA FUNÇÃO AUXILIAR) ---
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
    // --- FIM DA NOVA FUNÇÃO ---
}