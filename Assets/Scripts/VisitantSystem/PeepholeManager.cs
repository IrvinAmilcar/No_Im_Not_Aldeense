using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Suporte a TextMeshPro

public class PeepholeManager : MonoBehaviour
{
    public static PeepholeManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject peepholePanel;         // Painel Pai
    public Image visitorImage;               // Sprite do Visitante
    public TextMeshProUGUI dialogueText;     // Texto da fala
    public Transform choiceButtonsContainer; // Container dos botões
    public GameObject choiceButtonPrefab;    // Prefab do botão

    [Header("Integração DE3000")]
    public GameObject dialogueUIContainer;   // Painel de texto/botões
    public DE3000Manager de3000Manager;      // Script do Scanner

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
        // 1. Destrava Mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        currentProfile = profile;
        IsCurrentVisitorHuman = DetermineHumanity(profile);

        // 2. Ativa UI e Reseta estados
        peepholePanel.SetActive(true);
        if (dialogueUIContainer) dialogueUIContainer.SetActive(true);

        // Garante que o DE3000 comece fechado
        if (de3000Manager) de3000Manager.DeactivateDevice();

        // 3. Configura Imagem
        if (visitorImage)
        {
            visitorImage.sprite = profile.characterSprite;
            visitorImage.gameObject.SetActive(profile.characterSprite != null);
        }

        // 4. Carrega nó inicial
        LoadNode(profile.startNodeID);
    }

    private bool DetermineHumanity(VisitorProfile profile)
    {
        switch (profile.humanityType)
        {
            case HumanityType.AlwaysHuman: return true;
            case HumanityType.AlwaysImpostor: return false;
            default: return Random.value > 0.5f;
        }
    }

    public void LoadNode(string nodeID)
    {
        // --- FINAIS ESPECÍFICOS (IRVIN) ---
        if (nodeID == "IrvinT-Fim-Abre")
        {
            if (EndingManager.Instance != null) EndingManager.Instance.TriggerBadEnding_OpenDoor();
            return;
        }
        if (nodeID == "IrvinT-Fim-Nega")
        {
            FinishNarrativeEncounter(); // Fecha o olho mágico antes
            if (EndingManager.Instance != null) EndingManager.Instance.UnlockGunInteraction();
            if (DialogSystem.DialogManager.Instance != null) DialogSystem.DialogManager.Instance.ShowMessage("Pegue a arma! Rápido!", 3f);
            return;
        }

        // --- COMANDOS VIA NOME DO NÓ ---
        if (nodeID.Contains("-Scan"))
        {
            OpenDE3000();
            return;
        }
        // Visitantes comuns (Amanda, etc) que têm nó de decisão explícita
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

        // --- CARREGAMENTO NORMAL ---
        currentNode = TwineStoryParser.Instance.GetNode(nodeID);
        if (currentNode == null)
        {
            Debug.LogError($"[PeepholeManager] Nó '{nodeID}' não encontrado.");
            return;
        }

        // Exibe o texto
        if (dialogueUIContainer) dialogueUIContainer.SetActive(true);
        if (dialogueText) dialogueText.text = currentNode.text;

        GenerateButtons();
    }

    void GenerateButtons()
    {
        if (choiceButtonsContainer == null) return;

        // Limpa botões antigos
        foreach (Transform child in choiceButtonsContainer) Destroy(child.gameObject);

        if (currentNode == null || choiceButtonPrefab == null) return;

        // --- LÓGICA RESTAURADA: Verifica se há links ---
        if (currentNode.links.Count > 0)
        {
            // CASO 1: Diálogo Normal (Tem opções)
            foreach (var link in currentNode.links)
            {
                CreateButton(link.label, () => LoadNode(link.targetNode));
            }
        }
        else
        {
            // CASO 2: Diálogo Narrativo / Sem Saída (Homem Pálido)
            // Se não tem links, cria um botão de "Encerrar"
            CreateButton("Sair do Olho Mágico", () => FinishNarrativeEncounter());
        }
    }

    // Método auxiliar para criar botões (evita repetição de código)
    void CreateButton(string label, UnityEngine.Events.UnityAction action)
    {
        GameObject btn = Instantiate(choiceButtonPrefab, choiceButtonsContainer);

        // Tenta TMP
        var tmpText = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null) tmpText.text = label;
        else
        {
            // Tenta Legacy Text
            var legacyText = btn.GetComponentInChildren<Text>();
            if (legacyText != null) legacyText.text = label;
        }

        btn.GetComponent<Button>().onClick.AddListener(action);
    }

    public void OpenDE3000()
    {
        if (dialogueUIContainer) dialogueUIContainer.SetActive(false);

        if (de3000Manager != null)
        {
            de3000Manager.ActivateDevice(currentProfile, IsCurrentVisitorHuman);
        }
        else
        {
            // Tenta recuperar caso a referência tenha caído
            de3000Manager = FindObjectOfType<DE3000Manager>();
            if (de3000Manager) de3000Manager.ActivateDevice(currentProfile, IsCurrentVisitorHuman);
        }
    }

    public void ReturnToDialogue()
    {
        if (dialogueUIContainer) dialogueUIContainer.SetActive(true);
        if (dialogueText) dialogueText.text = "Análise concluída. O que devo fazer?";

        // Recarrega os botões do nó onde paramos
        GenerateButtons();
    }

    // --- ENCERRAMENTOS ---

    // Caso 1: Decisão tomada (Entrou ou Saiu)
    private void EndEncounter(bool letIn)
    {
        ClosePeephole();

        // Libera a porta física no jogo 3D
        var door = FindObjectOfType<DoorInteraction>();
        if (door != null) door.ExitInteraction();

        // Lógica de Gameplay
        if (letIn)
        {
            Debug.Log(IsCurrentVisitorHuman ? "HUMANO entrou (Ganhou Combustível)." : "IMPOSTOR entrou (Perdeu Combustível).");
            // Adicione aqui: GeneratorManager.Instance.ModifyFuel(...);
        }

        NotifyDayCycle();
    }

    // Caso 2: Apenas conversa (Homem Pálido) - Restaurado do seu código antigo
    public void FinishNarrativeEncounter()
    {
        Debug.Log("Encontro Narrativo Finalizado (Ninguém entrou nem saiu).");

        ClosePeephole();

        var door = FindObjectOfType<DoorInteraction>();
        if (door != null) door.ExitInteraction();

        NotifyDayCycle();
    }

    // Método comum para avisar o DayCycleManager que acabou
    private void NotifyDayCycle()
    {
        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.RegisterVisitorProcessed(); // Avança a fila
        }
        else
        {
            Debug.LogError("DayCycleManager não encontrado! O jogo vai travar no mesmo visitante.");
        }
    }

    public void ClosePeephole()
    {
        peepholePanel.SetActive(false);

        // Trava mouse de volta para o jogo
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}