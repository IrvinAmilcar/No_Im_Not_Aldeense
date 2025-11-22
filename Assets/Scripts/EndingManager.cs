using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class EndingManager : MonoBehaviour
{
    public static EndingManager Instance { get; private set; }

    [Header("Referências UI e Cena")]
    public CanvasGroup blackScreenCanvasGroup;
    public TextMeshProUGUI cinematicText;
    public string creditsSceneName = "CreditsScene";

    [Header("Fontes de Áudio")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [System.Serializable]
    public struct EndingStep
    {
        [TextArea(1, 3)] public string textToShow;
        public AudioClip audioClip;
        public float duration;
    }

    [System.Serializable]
    public class EndingConfig
    {
        public string endingName;
        public AudioClip backgroundMusic;
        public List<EndingStep> sequenceSteps;
    }

    [Header("--- CONFIGURAÇÃO DOS FINAIS ---")]
    public EndingConfig finalA_Porta;
    public EndingConfig finalB_Arma;
    public EndingConfig finalC_GameOver; // --- NOVO: Final C ---

    [Header("Estado do Jogo")]
    public bool isGunUnlocked = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 0f;
            blackScreenCanvasGroup.blocksRaycasts = false;
        }
        if (cinematicText != null) cinematicText.text = "";
    }

    // --- GATILHOS PÚBLICOS ---

    public void TriggerBadEnding_OpenDoor()
    {
        StartCoroutine(PlayEndingSequence(finalA_Porta));
    }

    public void UnlockGunInteraction()
    {
        isGunUnlocked = true;
    }

    public void TriggerGunEnding_Shoot()
    {
        StartCoroutine(PlayEndingSequence(finalB_Arma));
    }

    // --- NOVO: GATILHO DO GERADOR ---
    public void TriggerGameOver_Blackout()
    {
        Debug.Log("Iniciando Final C: Game Over (Gerador)");
        StartCoroutine(PlayEndingSequence(finalC_GameOver));
    }

    // --- SEQUÊNCIA PRINCIPAL (Igual ao anterior) ---
    private IEnumerator PlayEndingSequence(EndingConfig config)
    {
        PrepareForCutscene();

        blackScreenCanvasGroup.blocksRaycasts = true;
        yield return FadeCanvas(0, 1, 2.0f);

        if (config.backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = config.backgroundMusic;
            musicSource.loop = false;
            musicSource.Play();
        }

        yield return new WaitForSeconds(1f);

        foreach (EndingStep step in config.sequenceSteps)
        {
            if (step.audioClip != null && sfxSource != null)
                sfxSource.PlayOneShot(step.audioClip);

            if (!string.IsNullOrEmpty(step.textToShow))
            {
                cinematicText.text = step.textToShow;
                yield return FadeText(0, 1, 0.5f);
            }
            else
            {
                cinematicText.text = "";
            }

            yield return new WaitForSeconds(step.duration);

            if (!string.IsNullOrEmpty(step.textToShow))
            {
                yield return FadeText(1, 0, 0.5f);
            }
        }

        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(creditsSceneName);
    }

    private void PrepareForCutscene()
    {
        if (PeepholeManager.Instance != null)
            PeepholeManager.Instance.ClosePeephole();

        // Desabilita controle de câmera (ajuste o nome do namespace se necessário)
        var camController = FindObjectOfType<UnityTemplateProjects.SimpleCameraController>();
        if (camController != null) camController.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (GameAudioManager.Instance != null)
        {
            // Para todos os sons, inclusive a batida na porta
            GameAudioManager.Instance.StopKnocking();
            // O resto da lógica de parar sons...
        }
    }

    private IEnumerator FadeCanvas(float start, float end, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            blackScreenCanvasGroup.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }
        blackScreenCanvasGroup.alpha = end;
    }

    private IEnumerator FadeText(float start, float end, float duration)
    {
        float t = 0f;
        Color c = cinematicText.color;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            c.a = Mathf.Lerp(start, end, t);
            cinematicText.color = c;
            yield return null;
        }
        c.a = end;
        cinematicText.color = c;
    }
}