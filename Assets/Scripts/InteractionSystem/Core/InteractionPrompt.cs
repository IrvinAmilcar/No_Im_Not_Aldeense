using UnityEngine;

public class InteractionPrompt : MonoBehaviour
{
    [Header("Configuração")]
    public string actionText = "Abrir";

    [Tooltip("Ajuste a altura do ícone.")]
    public Vector3 promptOffset = new Vector3(0, 1.5f, 0);

    public void Show()
    {
        if (InteractionUIManager.Instance != null)
        {
            // Envia separado: (Alvo, Título, Tecla, Offset)
            InteractionUIManager.Instance.ShowPrompt(
                this.transform,
                actionText,       // Vai para a área PRETA
                "[ESPAÇO]",       // Vai para a área BRANCA
                promptOffset
            );
        }
    }

    public void Hide()
    {
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.HidePrompt();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + promptOffset, 0.1f);
    }
}