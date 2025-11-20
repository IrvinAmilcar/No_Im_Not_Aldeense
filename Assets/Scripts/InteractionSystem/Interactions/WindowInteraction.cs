using UnityEngine;

public class WindowInteraction : BasePeekInteraction
{
    private bool waitingForExit = false;

    // --- CORREÇÃO DO ERRO: Este método é OBRIGATÓRIO agora ---
    protected override void OnPeekReady()
    {
        // A câmera já focou na janela. Agora habilitamos a saída.
        waitingForExit = true;

        // Opcional: Mostra dica visual se tiver o sistema de diálogo
        if (DialogSystem.DialogManager.Instance != null)
        {
            DialogSystem.DialogManager.Instance.ShowMessage("Pressione [ESPAÇO] para sair", 2f);
        }
    }

    void Update()
    {
        // Se estamos olhando e o jogador aperta espaço...
        if (waitingForExit && Input.GetKeyDown(KeyCode.Space))
        {
            waitingForExit = false;
            StopPeeking(); // Chama o método da base para voltar a câmera
        }
    }
}