using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using DG.Tweening;

public class CreditsManager : MonoBehaviour
{
    [Header("--- ARRASTE AQUI ---")]
    public RectTransform contentContainer;
    public TextMeshProUGUI textComponent;
    public CanvasGroup blackScreenCanvasGroup;
    public AudioSource audioSource;

    [Header("--- CONFIGURAÇÕES ---")]
    public string mainMenuSceneName = "MainMenu";
    public float scrollSpeed = 150f;
    public float startDelay = 1f;
    public bool allowSkip = true;

    private bool isSkipping = false;

    void Awake()
    {
        Time.timeScale = 1f;
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 1f;
            blackScreenCanvasGroup.blocksRaycasts = true;
        }
    }

    IEnumerator Start()
    {
        if (contentContainer == null || textComponent == null)
        {
            Debug.LogError("⛔ ERRO: Você esqueceu de arrastar o 'Content Container' ou o 'Text Component' no Inspector do CreditsManager!");
            yield break;
        }

        if (audioSource != null)
        {
            audioSource.volume = 0;
            audioSource.Play();
            audioSource.DOFade(1f, 2f);
        }

        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        float screenHeight = Screen.height;
        float textHeight = textComponent.preferredHeight;
        float startY = -(screenHeight / 2) - (contentContainer.rect.height / 2);
        contentContainer.anchoredPosition = new Vector2(0, startY);

        StartCoroutine(CreditsSequence(textHeight, screenHeight));
    }

    private IEnumerator CreditsSequence(float textHeight, float screenHeight)
    {
        if (blackScreenCanvasGroup != null)
            yield return blackScreenCanvasGroup.DOFade(0f, 2f).WaitForCompletion();

        yield return new WaitForSeconds(startDelay);

        float distanceToEnd = textHeight + screenHeight + 100f;
        float endY = contentContainer.anchoredPosition.y + distanceToEnd;
        float duration = distanceToEnd / scrollSpeed;

        yield return contentContainer.DOAnchorPosY(endY, duration)
            .SetEase(Ease.Linear)
            .SetId("creditsScroll")
            .WaitForCompletion();

        yield return new WaitForSeconds(2f);
        LoadMenu();
    }

    void Update()
    {
        if (allowSkip && !isSkipping && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)))
        {
            LoadMenu();
        }
    }

    private void LoadMenu()
    {
        if (isSkipping) return;
        isSkipping = true;

        // --- CORREÇÃO: DESTRAVA O MOUSE AQUI ---
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(ExitRoutine());
    }

    private IEnumerator ExitRoutine()
    {
        DOTween.Kill("creditsScroll");

        if (audioSource != null) audioSource.DOFade(0f, 1.5f);
        if (blackScreenCanvasGroup != null)
            yield return blackScreenCanvasGroup.DOFade(1f, 1.5f).WaitForCompletion();

        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError($"❌ ERRO: Cena '{mainMenuSceneName}' não encontrada!");
        }
    }
}