using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class EndingManager : MonoBehaviour
{
    public static EndingManager Instance { get; private set; }

    [Header("--- ARRASTE SEUS CONTROLES AQUI ---")]
    [Tooltip("Arraste aqui o SCRIPT PRINCIPAL que faz o jogador andar (ex: FirstPersonController).")]
    public MonoBehaviour playerMovementScript;

    [Tooltip("Arraste aqui o SCRIPT que faz o jogador interagir (ex: PlayerInteraction ou PlayerInteractor).")]
    public MonoBehaviour playerInteractionScript; // <--- CORREÇÃO: Agora aceita qualquer script!

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

    [Header("Configuração dos Finais")]
    public EndingConfig finalA_Porta;
    public EndingConfig finalB_Arma;
    public EndingConfig finalC_GameOver;

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

    // --- GATILHOS ---
    public void TriggerBadEnding_OpenDoor() { StartCoroutine(PlayEndingSequence(finalA_Porta)); }
    public void UnlockGunInteraction() { isGunUnlocked = true; }
    public void TriggerGunEnding_Shoot() { StartCoroutine(PlayEndingSequence(finalB_Arma)); }
    public void TriggerGameOver_Blackout() { StartCoroutine(PlayEndingSequence(finalC_GameOver)); }

    // --- SEQUÊNCIA PRINCIPAL ---
    private IEnumerator PlayEndingSequence(EndingConfig config)
    {
        // 1. TRAVA TUDO
        PrepareForCutscene();

        // 2. Tela Preta
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.blocksRaycasts = true;
            yield return FadeCanvas(0, 1, 2.0f);
        }

        // 3. Música
        if (config.backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = config.backgroundMusic;
            musicSource.loop = false;
            musicSource.Play();
        }

        yield return new WaitForSeconds(1f);

        // 4. Passos
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

    // --- MÉTODO DE TRAVAMENTO UNIVERSAL ---
    private void PrepareForCutscene()
    {
        // 1. Fecha UI do olho mágico
        if (PeepholeManager.Instance != null)
            PeepholeManager.Instance.ClosePeephole();

        // 2. DESLIGA MOVIMENTO (Genérico)
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        // 3. DESLIGA INTERAÇÃO (Genérico)
        if (playerInteractionScript != null)
        {
            playerInteractionScript.enabled = false;
        }

        // 4. TRAVA O MOUSE
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 5. PARA MÚSICA GLOBAL
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.StopKnocking();
            GameAudioManager.Instance.StopMainMusic();
        }

        // 6. Silencia Rádio
        var radio = FindObjectOfType<RadioInteraction>();
        if (radio != null) radio.ForceSilenceRadio();

        // 7. Silencia Gerador
        if (GeneratorManager.Instance != null) GeneratorManager.Instance.RemoveEnergy(0);
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