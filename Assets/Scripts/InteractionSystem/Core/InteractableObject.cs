using UnityEngine;
using System.Collections;

public class InteractableObject : MonoBehaviour
{
    [Header("Câmeras")]
    public Camera playerCamera;       // câmera principal (do jogador)
    public Camera peepholeCamera;     // câmera fixa do olho mágico

    [Header("Referências de controle")]
    public MonoBehaviour playerController;       // script de movimentação do player
    public MonoBehaviour cameraLookController;   // script de rotação da câmera

    [Header("Transição")]
    public float transitionSpeed = 2f; // tempo da transição do fade (opcional)

    private bool isPeeking = false;
    private bool isTransitioning = false;
    private CanvasGroup fadeCanvas;

    void Start()
    {
        // opcional: cria um fade preto suave na tela (pra não ser abrupto)
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
        // sair do modo espiar
        if (isPeeking && !isTransitioning && Input.GetKeyDown(KeyCode.Space))
        {
            StopPeeking();
        }
    }

    public void Interact()
    {
        if (!isPeeking && !isTransitioning)
            StartPeeking();
    }

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

        // bloqueia controles
        if (playerController != null) playerController.enabled = false;
        if (cameraLookController != null) cameraLookController.enabled = false;

        // fade suave
        yield return StartCoroutine(Fade(1f));

        // troca de câmeras
        playerCamera.enabled = false;
        peepholeCamera.enabled = true;

        // fade out
        yield return StartCoroutine(Fade(0f));

        isTransitioning = false;
    }

    IEnumerator SwitchToPlayerCamera()
    {
        isTransitioning = true;

        yield return StartCoroutine(Fade(1f));

        // volta pro player
        peepholeCamera.enabled = false;
        playerCamera.enabled = true;

        // reativa controles
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
