/*
 * Arquivo: DoorInteraction.cs
 * Pasta: Interactions
 * Descrição: Lógica específica para interagir com a porta (olho mágico).
 * Implementa a interface IInteractable.
 */

using UnityEngine;
using System.Collections;

// 1. Mudamos o nome da classe
// 2. Adicionamos ", IInteractable" para assinar o contrato
public class DoorInteraction : MonoBehaviour, IInteractable
{
    [Header("Câmeras (Olho Mágico)")]
    public Camera playerCamera;
    public Camera peepholeCamera;

    [Header("Referências de Controle")]
    public MonoBehaviour playerController;
    public MonoBehaviour cameraLookController;

    [Header("Transição")]
    public float transitionSpeed = 2f;

    [Header("Highlight")]
    public Color highlightColor = Color.yellow; // Cor do highlight

    // Variáveis privadas
    private Renderer objRenderer;
    private Color originalColor;
    private bool isPeeking = false;
    private bool isTransitioning = false;
    private CanvasGroup fadeCanvas;

    void Start()
    {
        // Pega o Renderer para o highlight
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            originalColor = objRenderer.material.color;
        }

        // --- Resto do seu código Start original ---
        GameObject fadeObj = new GameObject("CameraFade");
        fadeCanvas = fadeObj.AddComponent<CanvasGroup>();
        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeObj.AddComponent<CanvasRenderer>();

        RectTransform rect = fadeObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image img = fadeObj.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        fadeCanvas.alpha = 0f;
    }

    void Update()
    {
        // --- Seu código Update original ---
        if (isPeeking && !isTransitioning && Input.GetKeyDown(KeyCode.Space))
        {
            StopPeeking();
        }
    }

    // ---------------------------------------------------
    // MÉTODOS OBRIGATÓRIOS DA INTERFACE "IInteractable"
    // ---------------------------------------------------

    public void Interact()
    {
        if (!isPeeking && !isTransitioning)
            StartPeeking();
    }

    public void OnFocus()
    {
        if (objRenderer != null)
            objRenderer.material.color = highlightColor;
    }

    public void OnLoseFocus()
    {
        if (objRenderer != null)
            objRenderer.material.color = originalColor;
    }

    // ---------------------------------------------------
    // MÉTODOS ORIGINAIS (Lógica do Olho Mágico)
    // ---------------------------------------------------

    void StartPeeking()
    {
        isPeeking = true;
        StartCoroutine(SwitchToPeepholeCamera());
    }

    void StopPeeking()
    {
        isPeeking = false;
        StartCoroutine(SwitchToPlayerCamera());
    }

    IEnumerator SwitchToPeepholeCamera()
    {
        isTransitioning = true;

        if (playerController != null) playerController.enabled = false;
        if (cameraLookController != null) cameraLookController.enabled = false;

        yield return StartCoroutine(Fade(1f));
        playerCamera.enabled = false;
        peepholeCamera.enabled = true;
        yield return StartCoroutine(Fade(0f));

        isTransitioning = false;
    }

    IEnumerator SwitchToPlayerCamera()
    {
        isTransitioning = true;

        yield return StartCoroutine(Fade(1f));
        peepholeCamera.enabled = false;
        playerCamera.enabled = true;
        if (playerController != null) playerController.enabled = true;
        if (cameraLookController != null) cameraLookController.enabled = true;
        yield return StartCoroutine(Fade(0f));

        isTransitioning = false;
    }

    IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvas.alpha;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * transitionSpeed;
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        fadeCanvas.alpha = targetAlpha;
    }
}