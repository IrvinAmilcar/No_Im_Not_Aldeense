using UnityEngine;

public enum HumanityType
{
    Random,
    AlwaysHuman,
    AlwaysImpostor
}

[CreateAssetMenu(fileName = "NovoVisitante", menuName = "Game/Visitor Profile")]
public class VisitorProfile : ScriptableObject
{
    [Header("Identidade")]
    public string characterName;
    public Sprite characterSprite;

    [Header("Áudio Personalizado")]
    [Tooltip("Se deixado vazio, usará o som padrão de batida do DayCycleManager.")]
    public AudioClip specificKnockSound; // --- NOVO CAMPO ---

    [Header("Cérebro (Twine)")]
    [Tooltip("O nome EXATO da passagem inicial no arquivo .twee")]
    public string startNodeID;

    [Header("Lógica de Jogo")]
    public HumanityType humanityType = HumanityType.Random;
    public bool isScannable = true;

    [Header("Estatísticas Base (DE3000)")]
    [Tooltip("Temperatura Média (Humanos ~36.5)")]
    public float meanTemp = 36.5f;

    [Tooltip("Variação da Temperatura (Humanos ~0.5)")]
    public float tempStdDev = 0.5f;

    [Tooltip("Probabilidade de Sucesso no Teste de Retina (0.0 a 1.0)")]
    public float retinalProbability = 0.8f;

    [Tooltip("Padrão Neural Base (Array de 5 floats)")]
    public float[] neuralPattern = { 0.2f, 0.8f, 0.3f, 0.7f, 0.2f };
}