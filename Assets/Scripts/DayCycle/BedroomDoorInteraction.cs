using UnityEngine;
using DialogSystem;

public class BedroomDoorInteraction : MonoBehaviour, IInteractable
{
    [Header("Mensagens")]
    [TextArea] public string cantSleepMessage = "Ainda não posso dormir. Sinto que alguém ainda vai bater na porta...";
    [TextArea] public string gameOverMessage = "Não adianta se esconder... ele sabe que estou aqui.";

    private Renderer objRenderer;
    private Color originalColor;
    public Color highlightColor = Color.yellow;

    void Awake()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null) originalColor = objRenderer.material.color;
    }

    public void Interact()
    {
        if (DayCycleManager.Instance == null) return;

        // --- NOVA VERIFICAÇÃO: GAME OVER ---
        if (DayCycleManager.Instance.IsGameOver)
        {
            // Jogador tenta fugir para o quarto, mas não consegue
            if (DialogManager.Instance != null)
                DialogManager.Instance.ShowMessage(gameOverMessage, 4f);
            return;
        }

        // Verificação Normal
        if (DayCycleManager.Instance.CanAdvanceDay())
        {
            StartCoroutine(DayCycleManager.Instance.AdvanceToNextDaySequence());
        }
        else
        {
            if (DialogManager.Instance != null)
                DialogManager.Instance.ShowMessage(cantSleepMessage, 3f);
        }
    }

    public void OnFocus()
    {
        if (objRenderer != null) objRenderer.material.color = highlightColor;
    }

    public void OnLoseFocus()
    {
        if (objRenderer != null) objRenderer.material.color = originalColor;
    }
}