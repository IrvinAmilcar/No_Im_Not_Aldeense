/*
 * Arquivo: PlayerInteraction.cs
 * Pasta: Core
 * Descri��o: Fica no jogador. Detecta objetos interativos � frente
 * e chama os m�todos da interface IInteractable.
 */

using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configura��o de Intera��o")]
    public Camera playerCamera;
    public float interactionDistance = 3f;

    // Armazena o objeto que est� atualmente em foco
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
                // Se mirou em algo que n�o � interativo (ex: parede)
                ClearFocus();
            }
        }
        else
        {
            // Se n�o mirou em nada
            ClearFocus();
        }
    }

    void HandleInteraction()
    {
        // Se temos um objeto em foco e pressionamos Espa�o
        if (interactableInView != null && Input.GetKeyDown(KeyCode.Space))
        {
            interactableInView.Interact();
        }
    }

    /// Avisa ao objeto que ele n�o est� mais em foco.
    void ClearFocus()
    {
        if (interactableInView != null)
        {
            interactableInView.OnLoseFocus();
            interactableInView = null;
        }
    }
}