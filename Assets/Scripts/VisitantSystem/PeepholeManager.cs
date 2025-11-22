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
        // 1. Casos Fixos (Narrativos)
        if (profile.humanityType == HumanityType.AlwaysHuman) return true;
        if (profile.humanityType == HumanityType.AlwaysImpostor) return false;

        // 2. Chance Fixa (Balanceamento)
        // Como a dificuldade agora está na ANÁLISE, não precisamos entupir o jogador de monstros.
        // Uma chance de 40% a 50% de ser impostor mantém o suspense constante.
        float impostorChance = 0.45f; // 45% de chance de ser impostor todos os dias.

        // Debug para você saber o que o jogo decidiu
        bool isHuman = Random.value > impostorChance;
        Debug.Log($"[Sistema] Visitante Aleatório gerado. É Humano? {isHuman}");

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