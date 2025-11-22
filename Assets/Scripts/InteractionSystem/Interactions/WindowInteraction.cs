using UnityEngine;
using System.Collections;
using DialogSystem;

public class WindowInteraction : BasePeekInteraction
{
    // A corrotina ShowPeekDialogueCoroutine do DialogManager já cuida da espera e do input de espaço.
    
    protected override void OnPeekReady()
    {
        // Inicia a corrotina de diálogo e espera que ela termine antes de liberar o jogador.
        StartCoroutine(WindowDialogueSequence());
    }
    
    // Corrotina para controlar o fluxo: diálogo -> fim -> volta para o jogo.
    private IEnumerator WindowDialogueSequence()
    {
        dialogueActive = true; 
        
        // 1. Inicia o diálogo paginado da Janela (Sistema 3 do DialogManager)
        bool dialogueFinished = false;
        
        // Usa o diálogo armazenado na BasePeekInteraction (dialoguePages)
        // O callback 'dialogueFinished = true' é chamado quando o jogador aperta [ESPAÇO] na última página.
        DialogManager.Instance.ShowPeekDialogue(
            dialoguePages, 
            () => { dialogueFinished = true; } // Callback
        );

        // 2. Esperar o DialogManager nos avisar que terminou
        yield return new WaitUntil(() => dialogueFinished);
        
        // 3. Destravar e voltar
        dialogueActive = false;
        
        // O StopPeeking cuida do Fade Out/In e de reabilitar os controles do jogador.
        StopPeeking();
    }
    
    // O método Update() antigo que checava Input.GetKeyDown(KeyCode.Space) não é mais necessário,
    // pois a lógica de avanço de página e saída está toda dentro do DialogManager.
}