using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance { get; private set; }

    [Header("Referências da UI")]
    public RectTransform promptPanel;
    public CanvasGroup canvasGroup;

    [Header("Componentes de Texto (Separados)")]
    [Tooltip("Texto que fica na parte PRETA (Topo). Ex: 'Abrir'.")]
    public TextMeshProUGUI titleText;

    [Tooltip("Texto que fica na parte BRANCA (Baixo). Ex: '[ESPAÇO]'.")]
    public TextMeshProUGUI keyText;

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
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            promptPanel.localScale = Vector3.zero;
        }
    }

    void LateUpdate()
    {
        if (currentTarget != null && promptPanel != null && isVisible)
        {
            Vector3 worldPos = currentTarget.position + currentOffset;
            Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0)
            {
                if (canvasGroup.alpha > 0) canvasGroup.alpha = 0;
            }
            else
            {
                if (canvasGroup.alpha < 1) canvasGroup.alpha = 1;
                promptPanel.position = screenPos;
            }
        }
    }

    // --- MUDANÇA: Recebe Título e Tecla separados ---
    public void ShowPrompt(Transform target, string actionName, string keyString, Vector3 offset)
    {
        if (isVisible && currentTarget == target) return;

        currentTarget = target;
        currentOffset = offset;

        // Define os textos separadamente
        if (titleText != null) titleText.text = actionName; // Ex: "Radio"
        if (keyText != null) keyText.text = keyString;      // Ex: "[ESPAÇO]"

        if (mainCam != null)
        {
            Vector3 screenPos = mainCam.WorldToScreenPoint(target.position + offset);
            promptPanel.position = screenPos;
        }

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

        canvasGroup.DOKill();
        promptPanel.DOKill();

        canvasGroup.DOFade(0, fadeDuration);
        promptPanel.DOScale(0.8f, fadeDuration);
    }
}