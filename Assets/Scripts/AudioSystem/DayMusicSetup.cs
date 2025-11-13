using UnityEngine;

/// <summary>
/// CRIAÇÃO DESTE ARQUIVO:
/// No Unity, clique com o botão direito na pasta do projeto > Create > Audio > DayMusicSetup
/// </summary>
[CreateAssetMenu(fileName = "DayMusicSetup_", menuName = "Audio/Day Music Setup", order = 1)]
public class DayMusicSetup : ScriptableObject
{
    [Tooltip("A trilha sonora principal para este dia.")]
    public AudioClip mainDayTrack;

    [Tooltip("A música especial que toca durante eventos específicos do olho mágico (opcional).")]
    public AudioClip specialPeekTrack;
}