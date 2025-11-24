using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // DOTween

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance { get; private set; }

    [Header("Referências da UI (Canvas Overlay)")]
    [Tooltip("O painel (quadradinho) inteiro.")]
    public RectTransform promptPanel;

    [Tooltip("O texto dentro do painel (ex: '[Espaço] Abrir').")]
    public TextMeshProUGUI promptText;

    [Tooltip("Canvas Group para controlar a opacidade.")]
    public CanvasGroup canvasGroup;

    [Header("Configuração DOTween")]
    public float fadeDuration = 0.2f;
    public Ease showEase = Ease.OutBack;

    private Camera mainCam;
    private Transform currentTarget;
    private Vector3 currentOffset;
    private bool isVisible = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        mainCam = Camera.main;

        // Garante que comece invisível e zerado
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            promptPanel.localScale = Vector3.zero;
        }
    }

    void LateUpdate()
    {
        // Segue o objeto na tela (Tracking)
        if (currentTarget != null && promptPanel != null && isVisible)
        {
            // Pega a posição do objeto + o ajuste de altura (offset)
            Vector3 worldPos = currentTarget.position + currentOffset;

            // Converte para a tela
            Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

            // Se o objeto está atrás da câmera (Z negativo), esconde
            if (screenPos.z < 0)
            {
                if (canvasGroup.alpha > 0) canvasGroup.alpha = 0;
            }
            else
            {
                // Se estava escondido por estar atrás, mostra de volta (sem animação lenta)
                if (canvasGroup.alpha < 1) canvasGroup.alpha = 1;

                promptPanel.position = screenPos;
            }
        }
    }

    public void ShowPrompt(Transform target, string text, Vector3 offset)
    {
        if (isVisible && currentTarget == target) return; // Já está mostrando esse

        currentTarget = target;
        currentOffset = offset;

        if (promptText != null) promptText.text = text;

        // Posiciona imediatamente antes de aparecer (para não "pular" na tela)
        if (mainCam != null)
        {
            Vector3 screenPos = mainCam.WorldToScreenPoint(target.position + offset);
            promptPanel.position = screenPos;
        }

        // Animação DOTween (Aparecer)
        isVisible = true;
        canvasGroup.DOKill();
        promptPanel.DOKill();

        canvasGroup.DOFade(1, fadeDuration);
        promptPanel.DOScale(1f, fadeDuration).SetEase(showEase);
    }

    public void HidePrompt()
    {
        if (!isVisible) return;

        isVisible = false;
        currentTarget = null;

        // Animação DOTween (Sumir)
        canvasGroup.DOKill();
        promptPanel.DOKill();

        canvasGroup.DOFade(0, fadeDuration);
        promptPanel.DOScale(0.8f, fadeDuration); // Diminui um pouco ao sumir
    }
}