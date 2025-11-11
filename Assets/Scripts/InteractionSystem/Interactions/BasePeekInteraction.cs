/*
 * Arquivo: BasePeekInteraction.cs
 * Pasta: Interactions
 * Descrição: Classe "mãe" abstrata que contém TODA a lógica
 * de "espiar" (trocar câmera, fade, highlight).
 */
using UnityEngine;
using System.Collections;

// Esta classe implementa o contrato IInteractable
public abstract class BasePeekInteraction : MonoBehaviour, IInteractable
{
    [Header("Configuração do Peek")]
    public Camera playerCamera;

    // Campo genérico! No Inpector, você arrasta a câmera
    // do olho mágico (para a porta) ou da janela (para a janela).
    public Camera targetViewCamera;

    [Header("Referências de Controle")]
    public MonoBehaviour playerController;
    public MonoBehaviour cameraLookController;

    [Header("Configuração de Efeito")]
    public float transitionSpeed = 2f;
    public Color highlightColor = Color.yellow;

    // Variáveis protegidas (acessíveis pelos "filhos")
    protected Renderer objRenderer;
    protected Color originalColor;
    protected bool isPeeking = false;
    protected bool isTransitioning = false;

    // Start é "virtual" para que os filhos possam sobrescrevê-lo
    // se precisarem de lógica extra no Start.
    protected virtual void Start()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            originalColor = objRenderer.material.color;
        }

        // Garante que a câmera alvo está desligada
        if (targetViewCamera != null)
        {
            targetViewCamera.enabled = false;
        }
    }

    // Update é "virtual" pelo mesmo motivo
    protected virtual void Update()
    {
        if (isPeeking && !isTransitioning && Input.GetKeyDown(KeyCode.Space))
        {
            StopPeeking();
        }
    }

    // --- Implementação dos Métodos IInteractable ---

    // "virtual" significa que os filhos podem mudar esse comportamento
    public virtual void Interact()
    {
        if (!isPeeking && !isTransitioning)
            StartPeeking();
    }

    public virtual void OnFocus()
    {
        if (objRenderer != null)
            objRenderer.material.color = highlightColor;
    }

    public virtual void OnLoseFocus()
    {
        if (objRenderer != null)
            objRenderer.material.color = originalColor;
    }

    // --- Lógica Centralizada do Peek ---

    protected void StartPeeking()
    {
        isPeeking = true;
        StartCoroutine(SwitchToTargetCamera());
    }

    protected void StopPeeking()
    {
        isPeeking = false;
        StartCoroutine(SwitchToPlayerCamera());
    }

    // Note que este método agora usa o CameraFader.Instance
    IEnumerator SwitchToTargetCamera()
    {
        isTransitioning = true;
        if (playerController != null) playerController.enabled = false;
        if (cameraLookController != null) cameraLookController.enabled = false;

        // Chama o Fader Singleton
        yield return StartCoroutine(CameraFader.Instance.Fade(1f, transitionSpeed));

        playerCamera.enabled = false;
        targetViewCamera.enabled = true; // Usa a câmera alvo

        // Chama o Fader Singleton
        yield return StartCoroutine(CameraFader.Instance.Fade(0f, transitionSpeed));

        isTransitioning = false;
    }

    IEnumerator SwitchToPlayerCamera()
    {
        isTransitioning = true;

        yield return StartCoroutine(CameraFader.Instance.Fade(1f, transitionSpeed));

        targetViewCamera.enabled = false; // Usa a câmera alvo
        playerCamera.enabled = true;

        if (playerController != null) playerController.enabled = true;
        if (cameraLookController != null) cameraLookController.enabled = true;

        yield return StartCoroutine(CameraFader.Instance.Fade(0f, transitionSpeed));

        isTransitioning = false;
    }

    // O MÉTODO FADE FOI REMOVIDO DAQUI
    // Ele agora está centralizado no CameraFader.cs
}