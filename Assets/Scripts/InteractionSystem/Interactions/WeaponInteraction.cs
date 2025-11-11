using UnityEngine;
using UnityEngine.SceneManagement;
using DialogSystem; // Importante: Precisamos do namespace do nosso sistema!

public class WeaponInteraction : MonoBehaviour, IInteractable
{
    [Header("Lógica do Dia Final")]
    [Tooltip("O índice (Build Index) da cena que é considerada o 'Dia Final'")]
    public int finalDaySceneIndex = 5;

    [Header("Mensagem (Antes do Dia Final)")]
    [Tooltip("Mensagem a ser exibida se o jogador interagir ANTES do dia final.")]
    [TextArea(3, 5)]
    public string messageIfNotFinalDay = "Melhor não tocar nisso agora. Preciso focar.";

    [Tooltip("Por quantos segundos a mensagem deve aparecer.")]
    public float messageDuration = 2.5f;

    // --- Implementação da Interface IInteractable ---

    public void Interact()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (currentSceneIndex == finalDaySceneIndex)
        {
            // --- LÓGICA DO DIA FINAL ---
            Debug.LogWarning("INTERAÇÃO DA ARMA NO DIA FINAL!");
            // Ex: GameManager.Instance.PlayerHasWeapon = true;
            // Ex: gameObject.SetActive(false);
        }
        else
        {
            // --- LÓGICA ANTES DO DIA FINAL ---
            // AQUI ESTÁ A MUDANÇA: Usamos o novo DialogManager
            
            // Verificamos se o Instance não é nulo antes de usá-lo
            if (DialogManager.Instance != null)
            {
                DialogManager.Instance.ShowMessage(messageIfNotFinalDay, messageDuration);
            }
            else
            {
                Debug.LogError("DialogManager.Instance não foi encontrado na cena! " +
                               "Certifique-se de adicionar o DialogManager a um objeto (ex: 'Managers').");
            }
        }
    }

    public void OnFocus()
    {
        // TODO: Adicionar feedback visual (ex: destacar o material da arma)
        // Ex: outlineEffect.enabled = true;
    }

    public void OnLoseFocus()
    {
        // TODO: Remover o feedback visual
        // Ex: outlineEffect.enabled = false;
    }
}
