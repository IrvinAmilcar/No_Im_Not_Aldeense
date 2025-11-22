using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DialogSystem;
using System.Linq;

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }
    public static event System.Action<int> OnDayStarted;

    // Estruturas de Dados (Mantidas iguais)
    [System.Serializable] public struct WindowDayConfig { public string windowID; [TextArea(3, 5)] public string[] dialogue; }
    [System.Serializable]
    public struct DayConfig
    {
        public string dayName;
        public List<VisitorProfile> visitorsForThisDay;
        public DayMusicSetup musicSetup;
        [TextArea] public string wakeUpMessage;
        [TextArea(3, 5)] public string[] radioDialogue;
        public WindowDayConfig[] windowDialogues;
    }

    [Header("Configuração dos Dias")]
    public DayConfig[] allDays;

    [Header("Configuração de Visitantes")]
    public AudioClip knockingSound;
    public float minArrivalDelay = 2f;
    public float maxArrivalDelay = 5f;

    [Header("Referências")]
    public RadioInteraction radioInteraction;
    public BasePeekInteraction[] allPeekInteractions;

    // Estado Interno
    private int currentDayIndex = 0;
    private Queue<VisitorProfile> dailyQueue = new Queue<VisitorProfile>();
    private int visitorsProcessedToday = 0;

    public bool IsVisitorWaiting { get; private set; } = false;

    // --- NOVO: Estado de Game Over (Trava a porta do quarto) ---
    public bool IsGameOver { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() { StartDay(0); }

    private void StartDay(int dayIndex)
    {
        // Se o gerador já quebrou durante a noite, para tudo.
        if (GeneratorManager.Instance != null && GeneratorManager.Instance.IsBroken) return;

        currentDayIndex = dayIndex;
        visitorsProcessedToday = 0;
        dailyQueue.Clear();
        IsVisitorWaiting = false;
        IsGameOver = false;

        if (dayIndex >= allDays.Length) { Debug.Log("Fim de Jogo!"); return; }

        DayConfig config = allDays[dayIndex];

        foreach (var v in config.visitorsForThisDay) dailyQueue.Enqueue(v);

        if (GameAudioManager.Instance != null && config.musicSetup != null)
            GameAudioManager.Instance.LoadDayMusic(config.musicSetup);

        if (radioInteraction != null)
        {
            radioInteraction.ResetForNewDay();
            if (config.radioDialogue != null && config.radioDialogue.Length > 0)
                radioInteraction.SetDailyDialogue(config.radioDialogue);
            else
                radioInteraction.SetDailyDialogue(new string[] { "O rádio está mudo." });
        }

        if (allPeekInteractions == null || allPeekInteractions.Length == 0)
            allPeekInteractions = FindObjectsOfType<BasePeekInteraction>();

        if (allPeekInteractions != null && config.windowDialogues != null)
        {
            foreach (BasePeekInteraction peekInteraction in allPeekInteractions)
            {
                if (peekInteraction is DoorInteraction) continue;
                WindowDayConfig? dialogueConfig = config.windowDialogues.FirstOrDefault(d => d.windowID == peekInteraction.windowID);
                if (dialogueConfig.HasValue && dialogueConfig.Value.dialogue != null && dialogueConfig.Value.dialogue.Length > 0)
                    peekInteraction.SetDailyDialogue(dialogueConfig.Value.dialogue);
                else
                    peekInteraction.SetDailyDialogue(new string[] { $"[Janela {peekInteraction.windowID}]: Eu não vejo nada de novo por aqui." });
            }
        }

        OnDayStarted?.Invoke(currentDayIndex);
        StartCoroutine(ScheduleNextVisitor());
    }

    private IEnumerator ScheduleNextVisitor()
    {
        if (dailyQueue.Count == 0)
        {
            IsVisitorWaiting = false;
            yield break;
        }

        float delay = Random.Range(minArrivalDelay, maxArrivalDelay);
        yield return new WaitForSeconds(delay);

        // Verificação extra: Se deu Game Over durante o delay, cancela
        if (IsGameOver) yield break;

        IsVisitorWaiting = true;

        if (GameAudioManager.Instance != null)
        {
            VisitorProfile nextVisitor = dailyQueue.Peek();
            AudioClip sound = nextVisitor.specificKnockSound != null ? nextVisitor.specificKnockSound : knockingSound;
            GameAudioManager.Instance.PlayKnocking(sound);
        }
    }

    public void CheckDoorForVisitor()
    {
        if (GameAudioManager.Instance != null) GameAudioManager.Instance.StopKnocking();

        if (dailyQueue.Count > 0)
        {
            VisitorProfile nextVisitor = dailyQueue.Peek();
            PeepholeManager.Instance.StartEncounter(nextVisitor);
        }
    }

    public void RegisterVisitorProcessed()
    {
        // Se estiver em Game Over, não processa mais nada normal
        if (IsGameOver) return;

        if (dailyQueue.Count > 0) dailyQueue.Dequeue();

        visitorsProcessedToday++;
        IsVisitorWaiting = false;

        if (dailyQueue.Count == 0)
        {
            if (DialogManager.Instance != null)
                DialogManager.Instance.ShowMessage("O silêncio voltou... Acho que posso dormir agora.", 3f);
        }
        else
        {
            StartCoroutine(ScheduleNextVisitor());
        }
    }

    // --- MÉTODO DE GAME OVER (Chamado pelo GeneratorManager) ---
    public void TriggerGameOverEvent(VisitorProfile paleMan)
    {
        Debug.Log("DAYCYCLE: Modo Game Over Ativado.");

        // 1. Para tudo que estava acontecendo
        StopAllCoroutines();

        // 2. Limpa a fila e injeta o Homem Pálido
        dailyQueue.Clear();
        dailyQueue.Enqueue(paleMan);

        // 3. Define estados
        IsGameOver = true; // Bloqueia quarto
        IsVisitorWaiting = true; // Libera porta da frente

        // 4. Toca som de batida (Lento/Ameaçador se tiver no perfil)
        if (GameAudioManager.Instance != null)
        {
            AudioClip sound = paleMan.specificKnockSound != null ? paleMan.specificKnockSound : knockingSound;
            GameAudioManager.Instance.PlayKnocking(sound);
        }
    }

    public bool CanAdvanceDay() { return dailyQueue.Count == 0 && !IsGameOver; }

    public IEnumerator AdvanceToNextDaySequence()
    {
        if (CameraFader.Instance != null) yield return StartCoroutine(CameraFader.Instance.Fade(1f, 2f));
        else yield return new WaitForSeconds(1f);

        yield return new WaitForSeconds(2f);
        currentDayIndex++;

        if (GeneratorManager.Instance != null) GeneratorManager.Instance.TransitionToNextDay();

        // Se o gerador quebrar na transição, o GeneratorManager vai iniciar a sequencia
        // e o StartDay vai ser abortado pelo check "if (IsBroken)".
        StartDay(currentDayIndex);

        if (CameraFader.Instance != null) yield return StartCoroutine(CameraFader.Instance.Fade(0f, 2f));

        if (currentDayIndex < allDays.Length && !IsGameOver)
        {
            string msg = allDays[currentDayIndex].wakeUpMessage;
            if (!string.IsNullOrEmpty(msg) && DialogManager.Instance != null)
                DialogManager.Instance.ShowMessage(msg, 4f);
        }
    }

    public int GetCurrentDayIndex() { return currentDayIndex; }
}