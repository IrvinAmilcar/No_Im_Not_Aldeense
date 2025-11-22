using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PeepholeManager : MonoBehaviour
{
    public static PeepholeManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject peepholePanel;
    public Image visitorImage;
    public TextMeshProUGUI dialogueText;
    public Transform choiceButtonsContainer;
    public GameObject choiceButtonPrefab;

    [Header("Integração DE3000")]
    public GameObject dialogueUIContainer;
    public DE3000Manager de3000Manager;

    // Estado Interno
    private VisitorProfile currentProfile;
    private DialogueNode currentNode;
    public bool IsCurrentVisitorHuman { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (peepholePanel) peepholePanel.SetActive(false);
    }

    public void StartEncounter(VisitorProfile profile)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        currentProfile = profile;
        IsCurrentVisitorHuman = DetermineHumanity(profile);

        peepholePanel.SetActive(true);
        if (dialogueUIContainer) dialogueUIContainer.SetActive(true);

        // --- CORREÇÃO: Usa ForceClose para evitar callbacks indesejados ---
        if (de3000Manager) de3000Manager.ForceClose();

        if (visitorImage)
        {
            visitorImage.sprite = profile.characterSprite;
            visitorImage.gameObject.SetActive(profile.characterSprite != null);
        }

        LoadNode(profile.startNodeID);
    }

    private bool DetermineHumanity(VisitorProfile profile)
    {
        // 1. Casos Fixos (Narrativos - Prioridade Máxima)
        // Se o perfil diz que é SEMPRE Humano ou SEMPRE Impostor, respeitamos isso.
        if (profile.humanityType == HumanityType.AlwaysHuman) return true;
        if (profile.humanityType == HumanityType.AlwaysImpostor) return false;

        // 2. Cálculo baseado no Dia (Dificuldade Progressiva)
        int currentDay = 1; // Valor padrão (Dia 1) se não encontrar o manager

        if (DayCycleManager.Instance != null)
        {
            // O índice começa em 0 (Dia 1 = index 0), então somamos 1 para o cálculo matemático
            currentDay = DayCycleManager.Instance.GetCurrentDayIndex() + 1;
        }

        // Chance base de ser IMPOSTOR no Dia 1
        float impostorChance = 0.1f; // 10%

        // Aumenta a chance conforme os dias passam
        // Fórmula: Chance = Base + (Dia * 0.15)
        // Dia 1: 0.10 + 0.15 = 0.25 (25%)
        // Dia 2: 0.10 + 0.30 = 0.40 (40%)
        // Dia 3: 0.10 + 0.45 = 0.55 (55%)
        // Dia 4: 0.10 + 0.60 = 0.70 (70%)
        // Dia 5: 0.10 + 0.75 = 0.85 (85%)
        impostorChance += (currentDay * 0.15f);

        // Trava no máximo em 90% para sempre ter uma chance de esperança
        impostorChance = Mathf.Clamp(impostorChance, 0.1f, 0.9f);

        Debug.Log($"[Sistema] Dia {currentDay}. Chance de Impostor: {impostorChance * 100:F0}%");

        // 3. Rolagem de Dados
        // Random.value retorna um float entre 0.0 e 1.0
        // Se o valor sorteado for MENOR que a chance de ser impostor, ele É um impostor.
        // Ex: Chance 0.4 (40%). Sorteou 0.3 -> É Impostor. Sorteou 0.8 -> É Humano.

        // Lógica Invertida: Se o valor for MAIOR que a chance de impostor, é Humano.
        // Ex: Chance Impostor 40%. Sobra 60% para Humano.
        // Se Random > 0.4, cai na faixa dos 60% (Humano).

        bool isHuman = Random.value > impostorChance;

        Debug.Log($"[Sistema] Visitante gerado. É Humano? {isHuman}");

        return isHuman;
    }

    public void LoadNode(string nodeID)
    {
        // 1. Carrega os dados do nó primeiro (IMPORTANTE: Isso corrige o bug dos botões)
        DialogueNode newNode = TwineStoryParser.Instance.GetNode(nodeID);

        if (newNode != null)
        {
            currentNode = newNode;
        }
        else
        {
            Debug.LogError($"[PeepholeManager] Nó '{nodeID}' não encontrado no arquivo Twine.");
            return;
        }

        // 2. Checa eventos especiais baseados no Nome do Nó

        // Evento: SCAN (Abre o DE3000 e pausa o fluxo visual)
        if (nodeID.Contains("-Scan"))
        {
            OpenDE3000();
            return; // Para aqui. O texto do nó não será mostrado agora, apenas a UI do DE3000.
        }

        // Evento: FINAIS (Entrar/Sair)
        if (nodeID.Contains("-Fim-Entra"))
        {
            EndEncounter(true);
            return;
        }
        if (nodeID.Contains("-Fim-Sai"))
        {
            EndEncounter(false);
            return;
        }

        // Evento: Finais Especiais (Irvin)
        if (nodeID == "IrvinT-Fim-Abre")
        {
            if (EndingManager.Instance != null) EndingManager.Instance.TriggerBadEnding_OpenDoor();
            return;
        }
        if (nodeID == "IrvinT-Fim-Nega")
        {
            FinishNarrativeEncounter();
            if (EndingManager.Instance != null) EndingManager.Instance.UnlockGunInteraction();
            if (DialogSystem.DialogManager.Instance != null) DialogSystem.DialogManager.Instance.ShowMessage("Pegue a arma! Rápido!", 3f);
            return;
        }

        // 3. Se não for evento especial, mostra o diálogo normal
        if (dialogueUIContainer) dialogueUIContainer.SetActive(true);
        if (dialogueText) dialogueText.text = currentNode.text;

        GenerateButtons();
    }

    void GenerateButtons()
    {
        if (choiceButtonsContainer == null || currentNode == null || choiceButtonPrefab == null) return;

        // Limpa botões antigos
        foreach (Transform child in choiceButtonsContainer) Destroy(child.gameObject);

        // Lógica para nós com opções
        if (currentNode.links.Count > 0)
        {
            foreach (var link in currentNode.links)
            {
                CreateButton(link.label, () => LoadNode(link.targetNode));
            }
        }
        // Lógica para nós puramente narrativos (Ex: Homem Pálido)
        else
        {
            CreateButton("Sair do Olho Mágico", () => FinishNarrativeEncounter());
        }
    }

    void CreateButton(string label, UnityEngine.Events.UnityAction action)
    {
        GameObject btn = Instantiate(choiceButtonPrefab, choiceButtonsContainer);

        var tmpText = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null) tmpText.text = label;
        else
        {
            var legacyText = btn.GetComponentInChildren<Text>();
            if (legacyText != null) legacyText.text = label;
        }

        btn.GetComponent<Button>().onClick.AddListener(action);
    }

    public void OpenDE3000()
    {
        // Esconde o diálogo
        if (dialogueUIContainer) dialogueUIContainer.SetActive(false);

        // Abre o Scanner
        if (de3000Manager != null)
        {
            de3000Manager.ActivateDevice(currentProfile, IsCurrentVisitorHuman);
        }
        else
        {
            // Fallback de segurança
            de3000Manager = FindObjectOfType<DE3000Manager>();
            if (de3000Manager) de3000Manager.ActivateDevice(currentProfile, IsCurrentVisitorHuman);
        }
    }

    // Chamado quando você fecha o DE3000
    public void ReturnToDialogue()
    {
        if (dialogueUIContainer) dialogueUIContainer.SetActive(true);

        // Define um texto de feedback
        if (dialogueText) dialogueText.text = "Análise concluída. O que devo fazer?";

        // Gera os botões do nó ATUAL (que agora é o Node-Scan, contendo apenas Entrar/Sair)
        GenerateButtons();
    }

    // --- ENCERRAMENTOS ---

    private void EndEncounter(bool letIn)
    {
        ClosePeephole();

        var door = FindObjectOfType<DoorInteraction>();
        if (door != null) door.ExitInteraction();

        if (letIn)
        {
            Debug.Log(IsCurrentVisitorHuman ? "HUMANO entrou." : "IMPOSTOR entrou.");
            // Adicione lógica de combustível aqui se tiver
        }

        // Avança a fila do dia
        NotifyDayCycle();
    }

    public void FinishNarrativeEncounter()
    {
        ClosePeephole();
        var door = FindObjectOfType<DoorInteraction>();
        if (door != null) door.ExitInteraction();
        NotifyDayCycle();
    }

    private void NotifyDayCycle()
    {
        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.RegisterVisitorProcessed();
        }
        else
        {
            Debug.LogError("DayCycleManager não encontrado na cena!");
        }
    }

    public void ClosePeephole()
    {
        peepholePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}