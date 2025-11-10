using UnityEngine;

public class MouseInteraction : MonoBehaviour
{
    public Camera SimpleCameraController;
    public float interactionDistance = 3f; // distância máxima pra interação
    private GameObject objectInView;

    void Update()
    {
        DetectObjectInView();
        HandleInteraction();
    }

    void DetectObjectInView()
{
    // Cria um raio a partir da posição do mouse
    Ray ray = SimpleCameraController.ScreenPointToRay(Input.mousePosition);
    RaycastHit hit;

    if (Physics.Raycast(ray, out hit, interactionDistance))
    {
        GameObject hitObject = hit.collider.gameObject;

        if (hitObject.CompareTag("Interactable"))
        {
            objectInView = hitObject;
            HighlightObject(hitObject, true);
        }
        else
        {
            ClearHighlight();
        }
    }
    else
    {
        ClearHighlight();
    }
}


    void HandleInteraction()
    {
        if (objectInView != null && Input.GetKeyDown(KeyCode.Space))
        {
            // aqui chamamos o script específico do objeto (porta, por exemplo)
            var interactable = objectInView.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                interactable.Interact();
            }
        }
    }

    void HighlightObject(GameObject obj, bool highlight)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (highlight)
                renderer.material.color = Color.yellow; // realce simples
        }
    }

    void ClearHighlight()
    {
        if (objectInView != null)
        {
            Renderer renderer = objectInView.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.white; // volta ao normal
            objectInView = null;
        }
    }
}
