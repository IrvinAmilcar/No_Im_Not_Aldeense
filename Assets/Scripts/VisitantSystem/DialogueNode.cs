using System.Collections.Generic;

// Representa um "Bloco" de texto do Twine (uma passagem)
[System.Serializable]
public class DialogueNode
{
    public string title;       // O título da passagem (Ex: "Amanda-Conversa1")
    public string text;        // O texto do diálogo
    public List<DialogueLink> links = new List<DialogueLink>(); // As opções/botões

    // Helper para saber se é um nó final (sem botões de resposta)
    public bool IsEndNode => links.Count == 0;
}

// Representa um link [[Texto do Botão->Destino]]
[System.Serializable]
public class DialogueLink
{
    public string label;       // O que aparece no botão
    public string targetNode;  // O ID do nó para onde vai
}