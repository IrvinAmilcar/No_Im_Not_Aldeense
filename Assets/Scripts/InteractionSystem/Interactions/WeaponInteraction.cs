using UnityEngine;
using DialogSystem;

public class WeaponInteraction : MonoBehaviour, IInteractable
{
    [Header("Mensagens")]
    [TextArea] public string lockedMessage = "Melhor não tocar nisso agora. Preciso focar.";

    // Configuração Visual
    private Renderer objRenderer;
    private Color originalColor;
    public Color highlightColor = Color.red; // Vermelho para perigo/importância

    void Awake()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null) originalColor = objRenderer.material.color;
    }

    public void Interact()
    {
        // Verifica se estamos no momento final do jogo
        if (EndingManager.Instance != null && EndingManager.Instance.isGunUnlocked)
        {
            // DISPARA O FINAL DA ARMA
            EndingManager.Instance.TriggerGunEnding_Shoot();
        }
        else
        {
            // Mensagem padrão (Ainda não é hora)
            if (DialogManager.Instance != null)
            {
                DialogManager.Instance.ShowMessage(lockedMessage, 2f);
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