using UnityEngine;
using DialogSystem; // Necessário para mostrar mensagens na tela

public class BedroomDoorInteraction : MonoBehaviour, IInteractable
{
    [Header("Mensagens")]
    [TextArea] public string cantSleepMessage = "Ainda não posso dormir. Sinto que alguém ainda vai bater na porta...";

    // Configuração visual padrão
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
        // Verifica se o DayCycleManager existe antes de tentar usar
        if (DayCycleManager.Instance == null)
        {
            Debug.LogError("DayCycleManager não encontrado na cena!");
            return;
        }

        // Verifica se pode dormir (fila vazia)
        if (DayCycleManager.Instance.CanAdvanceDay())
        {
            // Inicia a sequência de dormir (Corrotina)
            StartCoroutine(DayCycleManager.Instance.AdvanceToNextDaySequence());
        }
        else
        {
            // Feedback que ainda tem gente para atender
            if (DialogManager.Instance != null)
            {
                DialogManager.Instance.ShowMessage(cantSleepMessage, 3f);
            }
            else
            {
                Debug.Log(cantSleepMessage); // Fallback se não tiver DialogManager
            }
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