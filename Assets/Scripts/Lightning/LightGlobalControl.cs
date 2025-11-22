using UnityEngine;
using System.Collections.Generic;
using DG.Tweening; // DOTween para o apagão suave

public class LightGlobalControl : MonoBehaviour
{
    public static LightGlobalControl Instance { get; private set; }

    [Header("Configuração da Casa")]
    [Tooltip("Arraste todas as luzes (Lâmpadas, Spots) da casa aqui.")]
    public List<Light> houseLights;

    [Tooltip("Cor da luz ambiente durante o apagão (quase preto).")]
    public Color blackoutColor = new Color(0.02f, 0.02f, 0.05f);

    void Awake()
    {
        Instance = this;
    }

    public void TriggerBlackout()
    {
        // 1. Apaga todas as luzes artificiais
        foreach (Light light in houseLights)
        {
            light.enabled = false;
        }

        // 2. Escurece o ambiente gradualmente (2 segundos)
        // RenderSettings controla a luz global da Unity
        DOTween.To(() => RenderSettings.ambientLight, x => RenderSettings.ambientLight = x, blackoutColor, 2f);

        Debug.Log("[LightControl] Blackout ativado.");
    }
}