using UnityEngine;
using System.Collections;

public abstract class BasePeekInteraction : MonoBehaviour, IInteractable
{
    [Header("Configuração de Câmeras")]
    public Camera playerCamera;
    public Camera targetViewCamera;

    [Header("Referências do Jogador")]
    public MonoBehaviour playerController;
    public MonoBehaviour cameraLookController;

    [Header("Efeitos")]
    public float transitionSpeed = 2f;
    public Color highlightColor = Color.yellow;

    protected Renderer objRenderer;
    protected Color originalColor;
    protected bool isPeeking = false;

    protected virtual void Start()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null) originalColor = objRenderer.material.color;

        if (targetViewCamera != null) targetViewCamera.enabled = false;
    }

    public void Interact()
    {
        if (!isPeeking) StartPeeking();
    }

    public void OnFocus()
    {
        if (objRenderer != null) objRenderer.material.color = highlightColor;
    }

    public void OnLoseFocus()
    {
        if (objRenderer != null) objRenderer.material.color = originalColor;
    }

    protected void StartPeeking()
    {
        isPeeking = true;
        StartCoroutine(ChangeCameraSequence(true));
    }

    public void StopPeeking()
    {
        if (isPeeking)
        {
            StartCoroutine(ChangeCameraSequence(false));
        }
    }

    private IEnumerator ChangeCameraSequence(bool entering)
    {
        // 1. Fade Out
        if (CameraFader.Instance != null)
            yield return StartCoroutine(CameraFader.Instance.Fade(1f, transitionSpeed));

        // 2. Troca Câmeras e Inputs
        if (entering)
        {
            playerCamera.enabled = false;
            targetViewCamera.enabled = true;
            TogglePlayerControls(false);
        }
        else
        {
            targetViewCamera.enabled = false;
            playerCamera.enabled = true;
            TogglePlayerControls(true);
        }

        // 3. Fade In
        if (CameraFader.Instance != null)
            yield return StartCoroutine(CameraFader.Instance.Fade(0f, transitionSpeed));

        // 4. Avisa os filhos
        if (entering)
            OnPeekReady();
        else
        {
            isPeeking = false;
            OnPeekExit();
        }
    }

    protected abstract void OnPeekReady();
    protected virtual void OnPeekExit() { }

    private void TogglePlayerControls(bool state)
    {
        // AQUI ESTAVA O ERRO: Removemos a lógica de cursor daqui.
        // O BasePeekInteraction só liga/desliga os scripts de movimento.
        // Quem cuida do cursor agora é o PeepholeManager (para a porta) ou ninguém (para a janela).

        if (playerController != null) playerController.enabled = state;
        if (cameraLookController != null) cameraLookController.enabled = state;
    }
}