/*
 * Arquivo: Scripts/InteractionSystem/Interactions/DoorInteraction.cs
 * Descrição: Herda de BasePeekInteraction. 
 * CONTÉM a lógica de chamar o visitante e conectar com o PeepholeManager.
 */
using UnityEngine;

public class DoorInteraction : BasePeekInteraction
{
    // Chamado automaticamente quando a câmera termina de focar na porta
    protected override void OnPeekReady()
    {
        // Verifica se temos o gerente do dia
        if (DayCycleManager.Instance != null)
        {
            // AQUI está a lógica de visitantes: chama a UI do olho mágico
            DayCycleManager.Instance.CheckDoorForVisitor();
        }
        else
        {
            Debug.LogError("ERRO: DayCycleManager não encontrado!");
            // Se der erro, sai do modo espiar para não travar o jogo
            StopPeeking();
        }
    }

    // --- IMPORTANTE: Como voltar ao normal? ---
    // Este método agora é chamado pelo PeepholeManager quando o jogador clica em "Encerrar" ou "Fechar"
    public void ExitInteraction()
    {
        // Apenas sai da câmera (retorna para o PlayerController)
        StopPeeking();
    }
}