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

        // --- MUDANÇA: DESATIVA O NOME ---
        // Como solicitado, não mostramos o nome separadamente na UI
        if (nameText != null)
        {
            nameText.gameObject.SetActive(false);
        }

        // Carrega Twine
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
        GenerateButtons();
        CheckNodeEvents(nodeID);
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
            CreateButton("[Fechar Olho Mágico]", ClosePeephole);
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
        if (nodeID.Contains("-Scan")) TryActivateScanner();
        else if (nodeID.Contains("-Fim-Entra")) StartCoroutine(FinishEncounterDelayed(true));
        else if (nodeID.Contains("-Fim-Sai")) StartCoroutine(FinishEncounterDelayed(false));
    }

    private void TryActivateScanner()
    {
        if (currentProfile.isScannable) dialogueText.text += "\n\n[SCAN]: Iniciando...";
        else dialogueText.text += "\n\n[ERRO]: Interferência detectada.";
    }

    IEnumerator FinishEncounterDelayed(bool allowedEntry)
    {
        yield return new WaitForSeconds(2f);
        ClosePeephole();

        Debug.Log(allowedEntry ? "Visitante Entrou" : "Visitante Saiu");

        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.RegisterVisitorProcessed();
    }

    public void ClosePeephole()
    {
        canvasRoot.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}