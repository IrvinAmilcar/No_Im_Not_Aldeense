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
    // Este método deve ser chamado pelo botão "Fechar" do PeepholeManager (UI)
    // Ou você pode adicionar um listener via código no Start.

    public void ExitInteraction()
    {
        StopPeeking(); // Chama o método da base para desfazer a câmera

        // Garante que a UI feche também
        if (PeepholeManager.Instance != null)
        {
            PeepholeManager.Instance.ClosePeephole();
        }
    }
}