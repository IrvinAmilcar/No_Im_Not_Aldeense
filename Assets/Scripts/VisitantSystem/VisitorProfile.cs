using UnityEngine;

// Define os tipos de comportamento para a humanidade
public enum HumanityType
{
    Random,         // 50/50 ou baseado na dificuldade do dia
    AlwaysHuman,    // Ex: Claudia, Irvin Dia 1
    AlwaysImpostor  // Ex: Irvin Dia 5
}

[CreateAssetMenu(fileName = "NovoVisitante", menuName = "Game/Visitor Profile")]
public class VisitorProfile : ScriptableObject
{
    [Header("Identidade")]
    public string characterName;   // Ex: "Irvin"

    [Header("Visual")]
    public Sprite characterSprite; // A imagem 2D que aparece no olho mágico

    [Header("Cérebro (Twine)")]
    [Tooltip("O nome EXATO da passagem inicial no arquivo .twee (ex: 'Irvin-Conversa1')")]
    public string startNodeID;

    [Header("Lógica de Jogo")]
    [Tooltip("Define se este visitante é fixo ou aleatório.")]
    public HumanityType humanityType = HumanityType.Random;

    [Tooltip("Se falso, o DE3000 não consegue ler este visitante (Ex: Homem Pálido).")]
    public bool isScannable = true;

    // --- Dados Estatísticos Base (Para o DE3000 usar depois) ---
    // Estes são os valores "Ideais" se for humano. O sistema do DE3000 vai distorcer isso se for impostor.
    [Header("Stats Base (Se Humano)")]
    public float baseTemperature = 36.5f;
    public float baseRetinalAccuracy = 0.8f;
}