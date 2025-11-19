using UnityEngine;
using System.Collections;
using DialogSystem; 

// Implementa IInteractable para funcionar com o PlayerInteraction
public class BedroomDoorInteraction : MonoBehaviour, IInteractable
{
    [Header("Configuração de Bloqueio")]
    [Tooltip("A mensagem de aviso se o jogador tentar dormir antes da hora.")]
    [TextArea(1, 3)]
    public string notReadyMessage = "Ainda é cedo, preciso processar mais visitantes.";
    public float messageDuration = 2.5f;

    [Header("Configuração de Efeito (Highlight)")]
    public Color highlightColor = Color.green;
    
    private Renderer objRenderer;
    private Color originalColor;

    void Awake()
    {
        // Lógica para pegar o Renderer e a cor original
        objRenderer = GetComponent<Renderer>();
        if (objRenderer == null)
        {
            objRenderer = GetComponentInChildren<Renderer>();
        }

        if (objRenderer != null)
        {
            originalColor = objRenderer.material.color;
        }
    }

    /// <summary>
    /// Chamado pelo PlayerInteraction quando o jogador pressiona "Interagir".
    /// </summary>
    public void Interact()
    {
        if (DayCycleManager.Instance != null)
        {
            // 1. Verifica se as condições para avançar o dia foram atendidas
            if (DayCycleManager.Instance.CanAdvanceDay())
            {
                // 2. Inicia a transição do dia (Fade Out, lógica, Fade In)
                StartCoroutine(DayCycleManager.Instance.AdvanceToNextDaySequence());
            }
            else
            {
                // 3. Mostra a mensagem de bloqueio
                if (DialogManager.Instance != null)
                {
                    DialogManager.Instance.ShowMessage(notReadyMessage, messageDuration);
                }
                else
                {
                    Debug.LogWarning("DialogManager não está ativo para mostrar a mensagem de bloqueio.");
                }
            }
        }
        else
        {
             Debug.LogError("DayCycleManager.Instance não foi encontrado! A porta do quarto não pode funcionar.");
        }
    }

    /// <summary>
    /// Chamado pelo PlayerInteraction quando o jogador olha para o objeto.
    /// </summary>
    public void OnFocus()
    {
        // Aplica a cor de destaque
        if (objRenderer != null)
        {
            objRenderer.material.color = highlightColor;
        }
    }

    /// <summary>
    /// Chamado pelo PlayerInteraction quando o jogador para de olhar para o objeto.
    /// </summary>
    public void OnLoseFocus()
    {
        // Restaura a cor original
        if (objRenderer != null)
        {
            objRenderer.material.color = originalColor;
        }
    }
}