using UnityEngine;

public class InteractionPrompt : MonoBehaviour
{
    [Header("Configuração")]
    public string actionText = "Abrir"; // Ex: "Ver", "Pegar"

    [Tooltip("Ajuste a altura do ícone (Y) para ficar acima do objeto.")]
    public Vector3 promptOffset = new Vector3(0, 1.5f, 0);

    public void Show()
    {
        if (InteractionUIManager.Instance != null)
        {
            // Monta o texto final: "[SPACE] Abrir"
            // Você pode trocar "SPACE" por um ícone de botão se preferir depois
            string fullText = $"<size=80%>[ESPAÇO]</size>\n{actionText}";

            InteractionUIManager.Instance.ShowPrompt(this.transform, fullText, promptOffset);
        }
    }

    public void Hide()
    {
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.HidePrompt();
        }
    }

    // Desenha uma bolinha verde no editor para você ajustar a altura (Offset)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + promptOffset, 0.1f);
    }
}