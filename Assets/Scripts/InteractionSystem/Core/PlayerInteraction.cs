/*
 * Arquivo: PlayerInteraction.cs
 * Pasta: Core
 * Descrição: Fica no jogador. Detecta objetos interativos à frente
 * e chama os métodos da interface IInteractable.
 */

using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuração de Interação")]
    public Camera playerCamera;
    public float interactionDistance = 3f;

    // Armazena o objeto que está atualmente em foco
    private IInteractable interactableInView;

    void Update()
    {
        DetectObjectInView();
        HandleInteraction();
    }

    void DetectObjectInView()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // Tenta pegar o componente que assina o contrato
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // Se for um objeto novo, troca o foco
                if (interactable != interactableInView)
                {
                    ClearFocus(); // Limpa o foco do objeto antigo
                    interactableInView = interactable;
                    interactableInView.OnFocus();
                }
            }
            else
            {
                // Se mirou em algo que não é interativo (ex: parede)
                ClearFocus();
            }
        }
        else
        {
            // Se não mirou em nada
            ClearFocus();
        }
    }

    void HandleInteraction()
    {
        // Se temos um objeto em foco e pressionamos Espaço
        if (interactableInView != null && Input.GetKeyDown(KeyCode.Space))
        {
            interactableInView.Interact();
        }
    }

    /// Avisa ao objeto que ele não está mais em foco.
    void ClearFocus()
    {
        if (interactableInView != null)
        {
            interactableInView.OnLoseFocus();
            interactableInView = null;
        }
    }
}