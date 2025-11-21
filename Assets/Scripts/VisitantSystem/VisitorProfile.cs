using UnityEngine;

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
    public Sprite characterSprite; // A imagem 2D que aparece no olho mágico

    [Header("Cérebro (Twine)")]
    [Tooltip("O nome EXATO da passagem inicial no arquivo .twee (ex: 'Irvin-Conversa1')")]
    public string startNodeID;

    [Header("Lógica de Jogo")]
    public HumanityType humanityType = HumanityType.Random;
    public bool isScannable = true;

    [Header("Estatísticas Base (DE3000)")]
    // Nomes corrigidos para bater com o DE3000Manager
    [Tooltip("Temperatura Média (Humanos ~36.5)")]
    public float meanTemp = 36.5f;

    [Tooltip("Variação da Temperatura (Humanos ~0.5)")]
    public float tempStdDev = 0.5f;

    [Tooltip("Probabilidade de Sucesso no Teste de Retina (0.0 a 1.0)")]
    public float retinalProbability = 0.8f;

    [Tooltip("Padrão Neural Base (Array de 5 floats)")]
    public float[] neuralPattern = { 0.2f, 0.8f, 0.3f, 0.7f, 0.2f };
}