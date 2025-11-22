using UnityEngine;
using System.Collections;
using DialogSystem;

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

    [Header("Configuração de Diálogo (Peek)")]
    [Tooltip("ID único para este objeto, usado pelo DayCycleManager.")]
    public string windowID = "default_window_id";

    [SerializeField]
    protected string[] dialoguePages = new string[] { "Eu não vejo nada de novo por aqui." };

    protected Renderer objRenderer;
    protected Color originalColor;
    protected bool isPeeking = false;
    protected bool dialogueActive = false;

    protected virtual void Start()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null) originalColor = objRenderer.material.color;

        if (targetViewCamera != null) targetViewCamera.enabled = false;
    }

    // --- MUDANÇA AQUI: Adicionado 'virtual' para permitir override ---
    public virtual void Interact()
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
        if (CameraFader.Instance != null)
            yield return StartCoroutine(CameraFader.Instance.Fade(1f, transitionSpeed));

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

        if (CameraFader.Instance != null)
            yield return StartCoroutine(CameraFader.Instance.Fade(0f, transitionSpeed));

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
        if (playerController != null) playerController.enabled = state;
        if (cameraLookController != null) cameraLookController.enabled = state;
    }

    public void SetDailyDialogue(string[] newPages)
    {
        this.dialoguePages = newPages;
    }
}