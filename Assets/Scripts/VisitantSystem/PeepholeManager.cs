using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DialogSystem;

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

    [Header("Configuração de Gameplay")]
    public int energyReward = 15;
    public int energyPenalty = 30;

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

        // --- NOVO: TOCA MÚSICA ESPECÍFICA DO VISITANTE (Se houver) ---
        if (profile.specialEncounterMusic != null && GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayVisitorMusic(profile.specialEncounterMusic);
        }

        peepholePanel.SetActive(true);
        if (dialogueUIContainer) dialogueUIContainer.SetActive(true);

        if (de3000Manager) de3000Manager.ForceHide();

        if (visitorImage)
        {
            visitorImage.sprite = profile.characterSprite;
            visitorImage.gameObject.SetActive(profile.characterSprite != null);
        }

        LoadNode(profile.startNodeID);
    }

    private bool DetermineHumanity(VisitorProfile profile)
    {
        // 1. Casos Fixos
        if (profile.humanityType == HumanityType.AlwaysHuman) return true;
        if (profile.humanityType == HumanityType.AlwaysImpostor) return false;

        // 2. Chance Fixa (45% Impostor)
        float impostorChance = 0.45f;
        bool isHuman = Random.value > impostorChance;
        Debug.Log($"[Sistema] Visitante Aleatório gerado. É Humano? {isHuman}");

        return isHuman;
    }

    public void LoadNode(string nodeID)
    {
        // 1. Gatilhos de Fim de Jogo (Prioridade)
        if (nodeID == "HomemPalido-GameOver-Final")
        {
            ClosePeephole();
            if (EndingManager.Instance != null)
                EndingManager.Instance.TriggerGameOver_Blackout();
            return;
        }

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

        // 2. Gatilhos de Ação (-Scan, -Entra, -Sai)
        if (nodeID.Contains("-Scan"))
        {
            OpenDE3000();
            return;
        }
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

        // 3. Carregamento Normal de Texto
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

        if (dialogueUIContainer) dialogueUIContainer.SetActive(true);
        if (dialogueText) dialogueText.text = currentNode.text;

        GenerateButtons();
    }

    void GenerateButtons()
    {
        if (choiceButtonsContainer == null || currentNode == null || choiceButtonPrefab == null) return;

        foreach (Transform child in choiceButtonsContainer) Destroy(child.gameObject);

        if (currentNode.links.Count > 0)
        {
            foreach (var link in currentNode.links)
            {
                CreateButton(link.label, () => LoadNode(link.targetNode));
            }
        }
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
        if (dialogueUIContainer) dialogueUIContainer.SetActive(false);

        if (de3000Manager != null)
        {
            de3000Manager.ActivateDevice(currentProfile, IsCurrentVisitorHuman);
        }
        else
        {
            de3000Manager = FindObjectOfType<DE3000Manager>();
            if (de3000Manager) de3000Manager.ActivateDevice(currentProfile, IsCurrentVisitorHuman);
        }
    }

    public void ReturnToDialogue()
    {
        if (dialogueUIContainer) dialogueUIContainer.SetActive(true);
        if (dialogueText) dialogueText.text = "Análise concluída. O que devo fazer?";
        GenerateButtons();
    }

    // --- AQUI ESTÁ A LÓGICA DE COMBUSTÍVEL ---
    private void EndEncounter(bool letIn)
    {
        ClosePeephole();

        var door = FindObjectOfType<DoorInteraction>();
        if (door != null) door.ExitInteraction();

        if (letIn)
        {
            // Se o jogador deixou entrar, aplicamos as consequências
            if (GeneratorManager.Instance != null)
            {
                if (IsCurrentVisitorHuman)
                {
                    Debug.Log("HUMANO entrou. Recompensa de energia.");
                    GeneratorManager.Instance.AddEnergy(energyReward);

                    if (DialogManager.Instance != null)
                        DialogManager.Instance.ShowMessage($"Você aceitou um humano.\nGerador +{energyReward}%", 3f);
                }
                else
                {
                    Debug.Log("IMPOSTOR entrou. Penalidade de energia.");
                    GeneratorManager.Instance.RemoveEnergy(energyPenalty);

                    if (DialogManager.Instance != null)
                        DialogManager.Instance.ShowMessage($"IMPOSTOR DETECTADO!\nEle sabotou o gerador: -{energyPenalty}%", 4f);
                }
            }
        }
        else
        {
            // Se mandou embora, não acontece nada com o gerador
            if (DialogManager.Instance != null)
                DialogManager.Instance.ShowMessage("Você recusou a entrada.", 2f);
        }

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
    }

    public void ClosePeephole()
    {
        peepholePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Sempre tenta restaurar a música ao fechar o olho mágico.
        // O GameAudioManager é inteligente o suficiente para não reiniciar a música se ela já for a correta.
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.RestoreDayMusic();
        }
    }
}