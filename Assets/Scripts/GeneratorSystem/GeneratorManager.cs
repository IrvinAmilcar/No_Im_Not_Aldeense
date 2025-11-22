using UnityEngine;
using System.Collections;
using DialogSystem;

public class GeneratorManager : MonoBehaviour
{
    public static GeneratorManager Instance { get; private set; }

    public int CurrentEnergy { get; private set; }
    private int currentDay = 1;

    [Header("Configuração de Falha")]
    public AudioSource generatorAudioSource; // O AudioSource DO GERADOR (3D sound)
    public AudioClip generatorBreakSound;    // Som de "TEC... PFFFF" (desligando)

    [Header("Configuração do Game Over")]
    [Tooltip("O Perfil do Visitante (Homem Pálido) que aparecerá no Game Over.")]
    public VisitorProfile paleManProfile;

    [Tooltip("O ID do nó no Twine que contém o texto 'Parece que sua sorte acabou...'")]
    public string gameOverNodeID = "HomemPalido-GameOver";

    // Estado de controle
    public bool IsBroken { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeGenerator();
    }

    private void InitializeGenerator()
    {
        CurrentEnergy = 100;
        currentDay = 1;
        IsBroken = false;
    }

    // Chamado ao dormir
    public void TransitionToNextDay()
    {
        if (IsBroken) return;

        currentDay++;
        RemoveEnergy(50);

        Debug.Log($"Transição para o Dia {currentDay}. Energia restante: {CurrentEnergy}%");
    }

    public void AddEnergy(int amount)
    {
        if (IsBroken) return;
        CurrentEnergy = Mathf.Clamp(CurrentEnergy + amount, 0, 100);
    }

    public void RemoveEnergy(int amount)
    {
        if (IsBroken) return;

        CurrentEnergy -= amount;

        // --- PONTO CRÍTICO: VERIFICAÇÃO DE FALHA ---
        if (CurrentEnergy <= 0)
        {
            CurrentEnergy = 0;
            StartCoroutine(TriggerFailureSequence());
        }
    }

    private IEnumerator TriggerFailureSequence()
    {
        IsBroken = true;
        Debug.Log("GERADOR FALHOU! INICIANDO SEQUÊNCIA DE GAME OVER.");

        // 1. Som de Quebrar (Efeito 3D do objeto)
        if (generatorAudioSource != null && generatorBreakSound != null)
        {
            generatorAudioSource.Stop();
            generatorAudioSource.PlayOneShot(generatorBreakSound);
        }

        // 2. PARA A MÚSICA AMBIENTE (O CORAÇÃO DA CORREÇÃO)
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.StopMainMusic();
        }

        // 3. Apagão Visual
        if (LightGlobalControl.Instance != null)
        {
            LightGlobalControl.Instance.TriggerBlackout();
        }

        // 4. Feedback na Tela
        if (DialogManager.Instance != null)
            DialogManager.Instance.ShowMessage("O gerador... morreu.", 3f);

        // 5. Suspense (4 segundos no escuro total e silêncio)
        yield return new WaitForSeconds(4f);

        // 6. Injeta o Homem Pálido na porta
        ForcePaleManArrival();
    }

    private void ForcePaleManArrival()
    {
        if (DayCycleManager.Instance != null && paleManProfile != null)
        {
            // Cria uma cópia do perfil para garantir que o diálogo comece no nó certo
            VisitorProfile gameOverProfile = Instantiate(paleManProfile);
            gameOverProfile.startNodeID = gameOverNodeID;
            gameOverProfile.isScannable = false; // Sem scan, é o fim.

            // Manda o DayCycleManager limpar tudo e colocar ele na porta
            DayCycleManager.Instance.TriggerGameOverEvent(gameOverProfile);
        }
        else
        {
            Debug.LogError("[GeneratorManager] Erro: Faltando DayCycleManager ou Perfil do Homem Pálido!");
        }
    }

    public int GetCurrentEnergy()
    {
        return CurrentEnergy;
    }
}