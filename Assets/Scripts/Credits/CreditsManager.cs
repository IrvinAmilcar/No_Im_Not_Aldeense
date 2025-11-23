using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using DG.Tweening;

public class CreditsManager : MonoBehaviour
{
    [Header("--- ARRASTE AQUI ---")]
    public RectTransform contentContainer; // O objeto pai "CreditsContent"
    public TextMeshProUGUI textComponent;  // O objeto "Text (TMP)" com o texto escrito
    public CanvasGroup blackScreenCanvasGroup;
    public AudioSource audioSource;

    [Header("--- CONFIGURAÇÕES ---")]
    public string mainMenuSceneName = "MainMenu"; // Nome EXATO da cena do menu
    public float scrollSpeed = 150f; // Aumentei para ir mais rápido
    public float startDelay = 1f;
    public bool allowSkip = true;

    private bool isSkipping = false;

    void Awake()
    {
        Time.timeScale = 1f; // Garante que o jogo não está pausado
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 1f;
            blackScreenCanvasGroup.blocksRaycasts = true;
        }
    }

    IEnumerator Start()
    {
        // Validação de Segurança
        if (contentContainer == null || textComponent == null)
        {
            Debug.LogError("⛔ ERRO: Você esqueceu de arrastar o 'Content Container' ou o 'Text Component' no Inspector do CreditsManager!");
            yield break;
        }

        // Inicia Música
        if (audioSource != null)
        {
            audioSource.volume = 0;
            audioSource.Play();
            audioSource.DOFade(1f, 2f);
        }

        // Espera 1 frame para a Unity calcular o tamanho do texto
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        // Posiciona o texto logo abaixo da tela
        float screenHeight = Screen.height;
        float textHeight = textComponent.preferredHeight;

        // Posição inicial Y
        float startY = -(screenHeight / 2) - (contentContainer.rect.height / 2);
        contentContainer.anchoredPosition = new Vector2(0, startY);

        Debug.Log($"✅ START: Altura do Texto: {textHeight}px. Altura da Tela: {screenHeight}px.");

        StartCoroutine(CreditsSequence(textHeight, screenHeight));
    }

    private IEnumerator CreditsSequence(float textHeight, float screenHeight)
    {
        // 1. Fade In (Tela Preta some)
        if (blackScreenCanvasGroup != null)
            yield return blackScreenCanvasGroup.DOFade(0f, 2f).WaitForCompletion();

        yield return new WaitForSeconds(startDelay);

        // 2. Calcula a distância e o tempo
        // O texto precisa subir sua própria altura + a altura da tela para sumir completamente
        float distanceToEnd = textHeight + screenHeight + 100f; // +100 de margem

        // Posição final Y
        float endY = contentContainer.anchoredPosition.y + distanceToEnd;

        // Tempo = Distância / Velocidade
        float duration = distanceToEnd / scrollSpeed;

        Debug.Log($"🚀 ROLANDO: Distância total: {distanceToEnd}px. Tempo estimado: {duration:F1} segundos.");

        // 3. Move o texto
        yield return contentContainer.DOAnchorPosY(endY, duration)
            .SetEase(Ease.Linear)
            .SetId("creditsScroll")
            .WaitForCompletion();

        Debug.Log("🏁 SCROLL FINALIZADO: Esperando 2 segundos para sair...");

        // 4. Espera final e sai
        yield return new WaitForSeconds(2f);
        LoadMenu();
    }

    void Update()
    {
        // Pular créditos
        if (allowSkip && !isSkipping && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)))
        {
            Debug.Log("⏩ JOGADOR PULOU OS CRÉDITOS.");
            LoadMenu();
        }
    }

    private void LoadMenu()
    {
        if (isSkipping) return; // Evita chamar duas vezes
        isSkipping = true;

        StartCoroutine(ExitRoutine());
    }

    private IEnumerator ExitRoutine()
    {
        DOTween.Kill("creditsScroll"); // Para o scroll

        // Fade Out Áudio e Vídeo
        if (audioSource != null) audioSource.DOFade(0f, 1.5f);
        if (blackScreenCanvasGroup != null)
            yield return blackScreenCanvasGroup.DOFade(1f, 1.5f).WaitForCompletion();

        Debug.Log($"🔄 CARREGANDO CENA: {mainMenuSceneName}");

        // Verifica se a cena existe
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError($"❌ ERRO CRÍTICO: A cena '{mainMenuSceneName}' não foi encontrada no Build Settings!");
        }
    }
}