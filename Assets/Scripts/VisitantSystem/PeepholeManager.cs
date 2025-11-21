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

        canvasRoot.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (profile.characterSprite != null)
        {
            visitorImage.sprite = profile.characterSprite;
            visitorImage.gameObject.SetActive(true);
        }
        else
        {
            visitorImage.gameObject.SetActive(false);
        }

        if (nameText != null) nameText.gameObject.SetActive(false);

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
        // --- DETECÇÃO DE FINAL DE JOGO (DIA 5) ---

        // 1. Final A: Escolheu abrir para o Irvin Impostor
        if (nodeID == "IrvinT-Fim-Abre")
        {
            if (EndingManager.Instance != null)
            {
                EndingManager.Instance.TriggerBadEnding_OpenDoor();
                return; // Para a execução aqui, o EndingManager assume
            }
        }

        // 2. Final B: Escolheu recusar o Irvin Impostor
        if (nodeID == "IrvinT-Fim-Nega")
        {
            // Fecha o olho mágico
            ClosePeephole();

            // Libera a arma e avisa o jogador
            if (EndingManager.Instance != null)
            {
                EndingManager.Instance.UnlockGunInteraction();
            }
            if (DialogSystem.DialogManager.Instance != null)
            {
                DialogSystem.DialogManager.Instance.ShowMessage("Pegue a arma! Rápido!", 3f);
            }
            return; // Sai da função
        }

        // --- Lógica Normal ---

        currentNode = TwineStoryParser.Instance.GetNode(nodeID);
        if (currentNode == null) return;

        dialogueText.text = currentNode.text;
        CheckNodeEvents(nodeID);
        GenerateButtons();
    }

    private void GenerateButtons()
    {
        foreach (Transform child in buttonsContainer) Destroy(child.gameObject);

        if (currentNode.links.Count > 0)
        {
            foreach (var link in currentNode.links)
            {
                CreateButton(link.label, () => LoadNode(link.targetNode));
            }
        }
        else
        {
            string buttonLabel = "Encerrar";
            UnityEngine.Events.UnityAction action;

            if (pendingEntryDecision.HasValue)
            {
                buttonLabel = "Concluir";
                action = () => FinishEncounter(pendingEntryDecision.Value);
            }
            else
            {
                buttonLabel = "Sair do Olho Mágico";
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
        pendingEntryDecision = null;

        if (nodeID.Contains("-Scan")) TryActivateScanner();
        else if (nodeID.Contains("-Fim-Entra")) pendingEntryDecision = true;
        else if (nodeID.Contains("-Fim-Sai")) pendingEntryDecision = false;
    }

    private void TryActivateScanner()
    {
        if (currentProfile.isScannable) dialogueText.text += "\n\n[SCAN]: Iniciando...";
        else dialogueText.text += "\n\n[ERRO]: Interferência detectada.";
    }

    public void FinishEncounter(bool allowedEntry)
    {
        Debug.Log(allowedEntry ? "Decisão: Visitante Entrou" : "Decisão: Visitante Saiu");
        NotifyManagerAndClose();
    }

    public void FinishNarrativeEncounter()
    {
        Debug.Log("Encontro Narrativo Finalizado.");
        NotifyManagerAndClose();
    }

    private void NotifyManagerAndClose()
    {
        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.RegisterVisitorProcessed();

        ClosePeephole();
    }

    public void ClosePeephole()
    {
        canvasRoot.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var door = FindObjectOfType<DoorInteraction>();
        if (door != null)
        {
            door.ExitInteraction();
        }
    }
}