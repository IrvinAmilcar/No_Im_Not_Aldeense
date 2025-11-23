using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using DG.Tweening;

public class CinematicManager : MonoBehaviour
{
    [Header("Configurações")]
    public string gameSceneName = "GameScene";
    public float typingSpeed = 30f; // Caracteres por segundo

    [Header("Vídeo Intro (Dentro da TV)")]
    public GameObject introScreenObject;
    public VideoPlayer videoPlayer;

    [Header("Cena Repórter")]
    public GameObject layoutManager;
    public TextMeshProUGUI reporterTextBlock;
    public RectTransform tvContainer;

    [Header("Áudio")]
    public AudioSource audioSource;
    public AudioClip newsTheme;

    // --- DIÁLOGOS LIMPOS (Sem Vinicius, Sem acumular) ---
    private string[] dialogueLines = new string[]
    {
        "...e essa foi a cobertura das chuvas no Agreste.",
        "Agora, uma notícia de última hora.",
        "Cientistas da USP confirmaram que haverá um eclipse total atípico que afetará toda região nordeste hoje...",
        "...a partir das 17:00.",
        "A natureza do evento... parece ser desconhecida.",
        "Autoridades pedem que todos permaneçam em locais seguros e tranquem suas portas."
    };

    void Start()
    {
        // 1. Configuração Inicial do Vídeo
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        }

        // Garante que o cenário está visível
        if (layoutManager != null) layoutManager.SetActive(true);
        if (introScreenObject != null) introScreenObject.SetActive(true);
        if (reporterTextBlock != null) reporterTextBlock.text = "";

        StartCoroutine(SequenceRoutine());
    }

    IEnumerator SequenceRoutine()
    {
        // --- FASE 1: VÍDEO DA VINHETA ---
        if (videoPlayer != null && videoPlayer.clip != null)
        {
            videoPlayer.Prepare();

            float timeout = 2f;
            while (!videoPlayer.isPrepared && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (videoPlayer.isPrepared)
            {
                videoPlayer.Play();
                yield return new WaitForSeconds((float)videoPlayer.clip.length);
            }
        }

        // --- FASE 2: CORTE PARA O REPÓRTER ---

        // Revela o repórter
        if (introScreenObject != null) introScreenObject.SetActive(false);

        // Música
        if (audioSource != null && newsTheme != null)
        {
            audioSource.clip = newsTheme;
            audioSource.Play();
        }

        yield return new WaitForSeconds(0.5f);

        // --- LOOP DE DIÁLOGO (SOBRESCREVENDO) ---
        foreach (string line in dialogueLines)
        {
            // 1. Limpa o texto anterior para o novo começar do zero
            reporterTextBlock.text = "";

            // 2. Digita a nova linha (DOTween Pro)
            // Como o texto começa vazio, ele vai digitar a frase inteira
            yield return reporterTextBlock.DOText(line, typingSpeed)
                .SetSpeedBased()
                .SetEase(Ease.Linear)
                .WaitForCompletion();

            // 3. Tempo de leitura (ajuste aqui se achar rápido/lento demais)
            yield return new WaitForSeconds(2.0f);
        }

        // --- FASE 3: FINAL ---
        yield return new WaitForSeconds(1f);
        if (tvContainer != null) tvContainer.DOShakeAnchorPos(0.5f, 30f, 50, 90, false, true);
        yield return new WaitForSeconds(1f);

        DOTween.KillAll();
        SceneManager.LoadScene(gameSceneName);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DOTween.KillAll();
            SceneManager.LoadScene(gameSceneName);
        }
    }
}