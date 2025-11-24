using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuração")]
    public Camera playerCamera;
    public float interactionDistance = 3f;
    public LayerMask interactionLayers = -1;

    private IInteractable currentInteractable;
    private InteractionPrompt currentPrompt;

    void Update()
    {
        // --- CORREÇÃO 1: Se a câmera estiver desligada (ex: Olho Mágico ativo), limpa tudo ---
        if (playerCamera != null && !playerCamera.enabled)
        {
            ClearFocus();
            return;
        }

        DetectObjectInView();
        HandleInteraction();
    }

    // --- CORREÇÃO 2: Se o script for desligado (ex: Final do Jogo), limpa tudo ---
    void OnDisable()
    {
        ClearFocus();
    }

    void DetectObjectInView()
    {
        // Se a câmera não existe ou está desligada, não faz nada
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayers))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            InteractionPrompt prompt = hit.collider.GetComponent<InteractionPrompt>();

            // Se mudou de objeto
            if (interactable != currentInteractable)
            {
                ClearFocus();

                if (interactable != null)
                {
                    currentInteractable = interactable;
                    currentInteractable.OnFocus();

                    currentPrompt = prompt;
                    if (currentPrompt != null) currentPrompt.Show();
                }
            }
        }
        else
        {
            ClearFocus();
        }
    }

    void HandleInteraction()
    {
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.Space))
        {
            currentInteractable.Interact();
        }
    }

    void ClearFocus()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
        }

        if (currentPrompt != null)
        {
            currentPrompt.Hide();
            currentPrompt = null;
        }
    }
}