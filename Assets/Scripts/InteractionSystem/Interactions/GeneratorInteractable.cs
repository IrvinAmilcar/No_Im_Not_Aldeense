/*
 * Arquivo: GeneratorInteractable.cs
 * Descrição: Script para ser colocado no objeto 3D do Gerador.
 * Implementa IInteractable para responder ao PlayerInteraction.
 */

using UnityEngine;
using DialogSystem; // Precisamos disso para acessar o DialogManager

public class GeneratorInteractable : MonoBehaviour, IInteractable
{
    [Header("Configuração da Mensagem")]
    [Tooltip("Duração em segundos que a mensagem de energia ficará na tela.")]
    [SerializeField] private float messageDuration = 2.5f;

    [Tooltip("Mensagem de foco (opcional, caso você tenha um sistema de 'dicas' de UI)")]
    [SerializeField] private string focusMessage = "Verificar Gerador";

    [Header("Configuração de Efeito (Highlight)")]
    [Tooltip("Cor que o objeto terá quando o jogador olhar para ele.")]
    public Color highlightColor = Color.yellow;
    
    private Renderer objRenderer;
    private Color originalColor;

    void Awake()
    {
        // Pega o Renderer para o efeito de highlight
        // Primeiro, tenta pegar no próprio objeto
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            // Salva a cor original do material principal
            originalColor = objRenderer.material.color;
        }
        else
        {
            // Se não achar, tenta pegar em um filho (caso o mesh esteja em um sub-objeto)
            objRenderer = GetComponentInChildren<Renderer>();
            if (objRenderer != null)
            {
                originalColor = objRenderer.material.color;
            }
            else
            {
                // Se ainda não achar, avisa no console
                Debug.LogWarning($"O objeto Gerador ({gameObject.name}) não possui um Renderer. O highlight não vai funcionar.");
            }
        }
    }


    /// <summary>
    /// Chamado pelo PlayerInteraction quando o jogador pressiona "Interagir".
    /// </summary>
    public void Interact()
    {
        // 1. Pega a energia atual do Manager
        // Usamos GetCurrentEnergy() para garantir que estamos pegando o valor correto
        int currentEnergy = GeneratorManager.Instance.GetCurrentEnergy();

        // 2. Formata a mensagem
        string messageToShow = $"Energia do Gerador: {currentEnergy}%";

        // 3. Usa o Sistema 1 (Mensagem Simples) do DialogManager para mostrar
        // Exatamente como você descreveu: caixa preta semi-transparente
        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.ShowMessage(messageToShow, messageDuration);
        }
        else
        {
            Debug.LogError("DialogManager.Instance não foi encontrado! A mensagem do gerador não pode ser exibida.");
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