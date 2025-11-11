/*
 * Arquivo: WindowInteraction.cs
 * Pasta: Interactions
 * Descrição: Lógica específica para interagir com a janela (espiar).
 * Implementa a interface IInteractable.
 */

using UnityEngine;
using System.Collections;

public class WindowInteraction : MonoBehaviour, IInteractable
{
    [Header("Câmeras (Espiar Janela)")]
    public Camera playerCamera;

    // VARIÁVEL CORRIGIDA:
    // Renomeamos de 'peepholeCamera' para 'windowViewCamera'
    public Camera windowViewCamera; // A câmera fixa da janela

    [Header("Referências de Controle")]
    public MonoBehaviour playerController;
    public MonoBehaviour cameraLookController;

    [Header("Transição")]
    public float transitionSpeed = 2f;

    [Header("Highlight")]
    public Color highlightColor = Color.cyan;

    // ... (variáveis privadas: objRenderer, originalColor, etc.) ...
    private Renderer objRenderer;
    private Color originalColor;
    private bool isPeeking = false;
    private bool isTransitioning = false;
    private CanvasGroup fadeCanvas;

    void Start()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            originalColor = objRenderer.material.color;
        }

        // ... (criação do CanvasGroup do fade) ...
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
        if (isPeeking && !isTransitioning && Input.GetKeyDown(KeyCode.Space))
        {
            StopPeeking();
        }
    }

    // --- MÉTODOS IInteractable ---
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

    // --- LÓGICA DO PEEK (Corrigido) ---
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

        // USO CORRIGIDO:
        windowViewCamera.enabled = true; // Ativa a câmera da JANELA

        yield return StartCoroutine(Fade(0f));
        isTransitioning = false;
    }

    IEnumerator SwitchToPlayerCamera()
    {
        isTransitioning = true;

        yield return StartCoroutine(Fade(1f));

        // USO CORRIGIDO:
        windowViewCamera.enabled = false; // Desativa a câmera da JANELA

        playerCamera.enabled = true;
        if (playerController != null) playerController.enabled = true;
        if (cameraLookController != null) cameraLookController.enabled = true;
        yield return StartCoroutine(Fade(0f));

        isTransitioning = false;
    }

    IEnumerator Fade(float targetAlpha)
    {
        // ... (lógica do fade sem alteração) ...
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