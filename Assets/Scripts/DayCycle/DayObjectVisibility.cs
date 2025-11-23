using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controla a visibilidade, interatividade e iluminação de um objeto com base no dia atual do jogo.
/// O script deve estar em um GameObject que PERMANECE ATIVO (no Inspector) para que possa
/// receber o evento de mudança de dia.
/// </summary>
public class DayObjectVisibility : MonoBehaviour
{
    [Header("Configuração de Visibilidade")]
    [Tooltip("Índices dos dias em que este objeto DEVE estar ativo (0 = Dia 1, 1 = Dia 2, 2 = Dia 3, etc.).")]
    public int[] visibleDayIndices = new int[] { 0 }; 

    // Referências para controlar os componentes
    private Renderer[] renderers;
    private Collider[] colliders;
    private Light[] lights; // NOVO: Referência para componentes de luz

    void Awake()
    {
        // 1. Encontra todos os componentes a serem controlados
        // O parâmetro 'true' garante que encontramos os desativados também.
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        lights = GetComponentsInChildren<Light>(true); // NOVO: Busca Luzes

        // Chamada inicial (caso Awake ocorra depois do Start do DayCycleManager)
        if (DayCycleManager.Instance != null)
        {
            UpdateVisibility(DayCycleManager.Instance.GetCurrentDayIndex());
        }
    }

    void OnEnable()
    {
        // 2. Assina o evento do DayCycleManager
        DayCycleManager.OnDayStarted += UpdateVisibility;
    }

    void OnDisable()
    {
        // 3. Desassina o evento
        DayCycleManager.OnDayStarted -= UpdateVisibility;
    }

    /// <summary>
    /// Chamado pelo DayCycleManager sempre que um novo dia é iniciado.
    /// </summary>
    private void UpdateVisibility(int currentDayIndex)
    {
        bool shouldBeActive = false;

        // Verifica se o índice do dia atual está na lista de dias visíveis
        foreach (int visibleDay in visibleDayIndices)
        {
            if (visibleDay == currentDayIndex)
            {
                shouldBeActive = true;
                break;
            }
        }

        // NOVO: Ativa/desativa todos os componentes
        SetComponentsActive(shouldBeActive);
    }

    private void SetComponentsActive(bool state)
    {
        // Controla os Renderers (tornando-o visível ou invisível)
        foreach (Renderer r in renderers)
        {
            r.enabled = state;
        }

        // Controla os Colliders (tornando-o interativo ou não)
        foreach (Collider c in colliders)
        {
            c.enabled = state;
        }
        
        // NOVO: Controla os Lights (tornando-o ligado ou desligado)
        foreach (Light l in lights)
        {
            l.enabled = state;
        }
    }
}