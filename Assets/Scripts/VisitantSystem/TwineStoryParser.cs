using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class TwineStoryParser : MonoBehaviour
{
    public static TwineStoryParser Instance { get; private set; }

    [Header("Arquivo da História")]
    [Tooltip("Arraste seu arquivo .twee ou .txt aqui")]
    public TextAsset twineFile;

    private Dictionary<string, DialogueNode> storyNodes = new Dictionary<string, DialogueNode>();

    void Awake()
    {
        Instance = this;
        if (twineFile != null)
        {
            ParseTwine(twineFile.text);
        }
        else
        {
            Debug.LogError("TwineStoryParser: Nenhum arquivo .twee atribuído!");
        }
    }

    private void ParseTwine(string content)
    {
        storyNodes.Clear();
        string[] rawPassages = Regex.Split(content, @"^::\s*", RegexOptions.Multiline);

        foreach (string raw in rawPassages)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("Story")) continue;
            ParsePassage(raw);
        }

        Debug.Log($"TwineParser: Carregou {storyNodes.Count} passagens.");
    }

    private void ParsePassage(string rawData)
    {
        DialogueNode node = new DialogueNode();

        var reader = new System.IO.StringReader(rawData);
        string header = reader.ReadLine();

        if (string.IsNullOrEmpty(header)) return;

        if (header.Contains("{"))
            node.title = header.Substring(0, header.IndexOf("{")).Trim();
        else
            node.title = header.Trim();

        string body = reader.ReadToEnd();

        // 1. Extrair Links [[Texto->Destino]]
        var linkMatches = Regex.Matches(body, @"\[\[(.*?)\->(.*?)\]\]");
        foreach (Match m in linkMatches)
        {
            node.links.Add(new DialogueLink
            {
                label = m.Groups[1].Value.Trim(),
                targetNode = m.Groups[2].Value.Trim()
            });
        }

        // 2. Limpeza de Texto
        string cleanBody = body;

        // REMOVE OS LINKS do corpo do texto
        cleanBody = Regex.Replace(cleanBody, @"\[\[.*?\]\]", "");

        // --- NOVO: REMOVE O NOME EM NEGRITO DO INÍCIO DA FALA ---
        // Procura por padrões como "**Amanda:** " ou "**Irvin:** " e remove
        cleanBody = Regex.Replace(cleanBody, @"\*\*.*?\*\*:\s*", "");

        // Remove formatação Markdown restante (**Negrito**, //Italico//)
        cleanBody = cleanBody.Replace("**", "");

        node.text = cleanBody.Trim();

        if (!storyNodes.ContainsKey(node.title))
        {
            storyNodes.Add(node.title, node);
        }
    }

    public DialogueNode GetNode(string title)
    {
        if (storyNodes.ContainsKey(title)) return storyNodes[title];
        Debug.LogWarning($"TwineParser: Nó não encontrado -> {title}");
        return null;
    }
}