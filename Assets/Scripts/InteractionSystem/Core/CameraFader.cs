/*
 * Arquivo: CameraFader.cs
 * Pasta: Core
 * Descrição: Controla um único CanvasGroup para fade in/out.
 * Deve existir apenas UMA vez na cena.
 */
using UnityEngine;
using System.Collections;

public class CameraFader : MonoBehaviour
{
    // --- O Padrão Singleton ---
    public static CameraFader Instance { get; private set; }

    private CanvasGroup fadeCanvas;

    void Awake()
    {
        // Configura o Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Opcional: mantém o fader entre cenas

        // --- Cria o CanvasGroup UMA ÚNICA VEZ ---
        GameObject fadeObj = new GameObject("CameraFade");
        fadeObj.transform.SetParent(this.transform); // Organiza o objeto

        fadeCanvas = fadeObj.AddComponent<CanvasGroup>();
        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Garante que está na frente
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

    /// <summary>
    /// Corrotina pública que realiza o fade.
    /// </summary>
    public IEnumerator Fade(float targetAlpha, float transitionSpeed)
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