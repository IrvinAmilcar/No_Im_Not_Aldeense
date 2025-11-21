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
    public AudioSource sfxSource;   // Para os sons de passos, porta, etc.
    public AudioSource musicSource; // Para a música de fundo (Ambiente/Tensão)

    // --- ESTRUTURA DE SEQUÊNCIA (O Segredo da Sincronia) ---
    [System.Serializable]
    public struct EndingStep
    {
        [TextArea(1, 3)] public string textToShow; // O texto deste passo
        public AudioClip audioClip;                // O som deste passo (ex: porta quebrando)
        public float duration;                     // Quanto tempo ficar neste passo antes do próximo
    }

    [System.Serializable]
    public class EndingConfig
    {
        public string endingName;
        public AudioClip backgroundMusic; // Música de fundo contínua
        public List<EndingStep> sequenceSteps; // A lista de ações passo-a-passo
    }

    [Header("--- CONFIGURAÇÃO DOS FINAIS ---")]
    public EndingConfig finalA_Porta; // Final onde você abre a porta
    public EndingConfig finalB_Arma;  // Final onde você usa a arma

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
        Debug.Log("Iniciando Final A: Porta");
        StartCoroutine(PlayEndingSequence(finalA_Porta));
    }

    public void UnlockGunInteraction()
    {
        isGunUnlocked = true;
        Debug.Log("FINAL: Arma desbloqueada.");
    }

    public void TriggerGunEnding_Shoot()
    {
        Debug.Log("Iniciando Final B: Arma");
        StartCoroutine(PlayEndingSequence(finalB_Arma));
    }

    // --- SEQUÊNCIA PRINCIPAL ---

    private IEnumerator PlayEndingSequence(EndingConfig config)
    {
        // 1. PREPARAÇÃO (Trava tudo)
        PrepareForCutscene();

        // 2. FADE PARA PRETO (Lento e tenso)
        blackScreenCanvasGroup.blocksRaycasts = true;
        yield return FadeCanvas(0, 1, 2.0f);

        // 3. TOCA MÚSICA DE FUNDO (Se houver)
        if (config.backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = config.backgroundMusic;
            musicSource.loop = false;
            musicSource.Play();
        }

        yield return new WaitForSeconds(1f); // Pequeno respiro no escuro

        // 4. LOOP DA SEQUÊNCIA (Passo a Passo)
        foreach (EndingStep step in config.sequenceSteps)
        {
            // A. Toca o som do passo (se houver)
            if (step.audioClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(step.audioClip);
            }

            // B. Atualiza e mostra o texto (se houver)
            if (!string.IsNullOrEmpty(step.textToShow))
            {
                cinematicText.text = step.textToShow;
                yield return FadeText(0, 1, 0.5f); // Fade In Texto
            }
            else
            {
                cinematicText.text = ""; // Se não tiver texto, garante tela preta
            }

            // C. Espera o tempo definido no Inspector
            // (Isso permite que o som termine ou crie tensão)
            yield return new WaitForSeconds(step.duration);

            // D. Esconde o texto antes do próximo passo
            if (!string.IsNullOrEmpty(step.textToShow))
            {
                yield return FadeText(1, 0, 0.5f); // Fade Out Texto
            }
        }

        yield return new WaitForSeconds(1f); // Pausa final

        // 5. CRÉDITOS
        SceneManager.LoadScene(creditsSceneName);
    }

    // --- AUXILIARES ---

    private void PrepareForCutscene()
    {
        if (PeepholeManager.Instance != null)
            PeepholeManager.Instance.ClosePeephole();

        var camController = FindObjectOfType<UnityTemplateProjects.SimpleCameraController>();
        if (camController != null) camController.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (GameAudioManager.Instance != null)
        {
            var allSources = FindObjectsOfType<AudioSource>();
            foreach (var source in allSources)
            {
                if (source != sfxSource && source != musicSource)
                    source.Stop();
            }
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