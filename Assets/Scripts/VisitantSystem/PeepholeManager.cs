using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PeepholeManager : MonoBehaviour
{
    public static PeepholeManager Instance { get; private set; }

    [Header("Referências UI")]
    [Tooltip("O objeto Pai de toda a UI")]
    public GameObject canvasRoot;

    [Tooltip("A imagem onde aparecerá o visitante")]
    public Image visitorImage;

    [Tooltip("O texto verde onde aparece a fala")]
    public TextMeshProUGUI dialogueText;

    [Tooltip("O texto com o nome do personagem (Será desativado automaticamente)")]
    public TextMeshProUGUI nameText;

    public Transform buttonsContainer;

    [Header("Prefabs")]
    public GameObject choiceButtonPrefab;

    // --- Estado Interno ---
    private VisitorProfile currentProfile;
    private DialogueNode currentNode;
    public bool IsCurrentVisitorHuman { get; private set; }

    // Variável para saber se foi entrada ou saída na hora de finalizar
    private bool? pendingEntryDecision = null;

    void Awake()
    {
        Instance = this;
        if (canvasRoot != null) canvasRoot.SetActive(false);
    }

    public void StartEncounter(VisitorProfile profile)
    {
        currentProfile = profile;
        DetermineHumanity();

        // Liga a UI
        canvasRoot.SetActive(true);

        // Libera o Mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Configura Imagem
        if (profile.characterSprite != null)
        {
            visitorImage.sprite = profile.characterSprite;
            visitorImage.gameObject.SetActive(true);
        }
        else
        {
            visitorImage.gameObject.SetActive(false);
        }

        if (nameText != null)
        {
            nameText.gameObject.SetActive(false);
        }

        // Carrega o nó inicial do Twine
        LoadNode(profile.startNodeID);
    }

    private void DetermineHumanity()
    {
        switch (currentProfile.humanityType)
        {
            case HumanityType.AlwaysHuman: IsCurrentVisitorHuman = true; break;
            case HumanityType.AlwaysImpostor: IsCurrentVisitorHuman = false; break;
            case HumanityType.Random: IsCurrentVisitorHuman = Random.value > 0.5f; break;
        }
    }

    public void LoadNode(string nodeID)
    {
        currentNode = TwineStoryParser.Instance.GetNode(nodeID);
        if (currentNode == null) return;

        dialogueText.text = currentNode.text;

        // Verifica se é um nó de "Scan" ou de "Fim" para preparar a lógica
        CheckNodeEvents(nodeID);

        // Gera os botões baseados no nó atual
        GenerateButtons();
    }

    private void GenerateButtons()
    {
        // 1. Limpa botões antigos
        foreach (Transform child in buttonsContainer) Destroy(child.gameObject);

        // 2. Se o nó tem links (opções normais do Twine), cria os botões
        if (currentNode.links.Count > 0)
        {
            foreach (var link in currentNode.links)
            {
                CreateButton(link.label, () => LoadNode(link.targetNode));
            }
        }
        else
        {
            // 3. Se NÃO tem links, é um nó final.
            string buttonLabel = "Encerrar";
            UnityEngine.Events.UnityAction action;

            // CASO A: É um final com decisão (Entrou ou Saiu)
            if (pendingEntryDecision.HasValue)
            {
                buttonLabel = "Concluir";
                action = () => FinishEncounter(pendingEntryDecision.Value);
            }
            // CASO B: É um final narrativo (Homem Pálido, apenas vai embora)
            else
            {
                buttonLabel = "Sair do Olho Mágico";
                // --- AQUI ESTAVA O ERRO: Antes chamava ClosePeephole direto ---
                // Agora chamamos FinishNarrativeEncounter para limpar a fila
                action = FinishNarrativeEncounter;
            }

            CreateButton(buttonLabel, action);
        }
    }

    private void CreateButton(string label, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = Instantiate(choiceButtonPrefab, buttonsContainer);

        var tmpText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = label;
        }
        else
        {
            var legacyText = btnObj.GetComponentInChildren<Text>();
            if (legacyText != null) legacyText.text = label;
        }

        btnObj.GetComponent<Button>().onClick.AddListener(action);
    }

    private void CheckNodeEvents(string nodeID)
    {
        // Reseta decisão pendente ao carregar novo nó
        pendingEntryDecision = null;

        if (nodeID.Contains("-Scan"))
        {
            TryActivateScanner();
        }
        else if (nodeID.Contains("-Fim-Entra"))
        {
            pendingEntryDecision = true;
        }
        else if (nodeID.Contains("-Fim-Sai"))
        {
            pendingEntryDecision = false;
        }
    }

    private void TryActivateScanner()
    {
        if (currentProfile.isScannable) dialogueText.text += "\n\n[SCAN]: Iniciando...";
        else dialogueText.text += "\n\n[ERRO]: Interferência detectada.";
    }

    // Chamado para encontros com decisão (Entrar/Sair)
    public void FinishEncounter(bool allowedEntry)
    {
        Debug.Log(allowedEntry ? "Decisão: Visitante Entrou" : "Decisão: Visitante Saiu");
        NotifyManagerAndClose();
    }

    // --- CORREÇÃO: Novo método para encontros narrativos ---
    public void FinishNarrativeEncounter()
    {
        Debug.Log("Encontro Narrativo Finalizado (Sem decisão de entrada).");
        NotifyManagerAndClose();
    }

    // Método centralizado para avisar o manager e fechar
    private void NotifyManagerAndClose()
    {
        // 1. Avisa o DayCycleManager para remover da fila e atualizar lógica
        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.RegisterVisitorProcessed();

        // 2. Fecha a UI e reseta a câmera
        ClosePeephole();
    }

    public void ClosePeephole()
    {
        // 1. Desativa a UI
        canvasRoot.SetActive(false);

        // 2. Trava o Mouse novamente
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 3. Avisa o DoorInteraction para sair da câmera do olho mágico
        var door = FindObjectOfType<DoorInteraction>();
        if (door != null)
        {
            door.ExitInteraction();
        }
    }
}