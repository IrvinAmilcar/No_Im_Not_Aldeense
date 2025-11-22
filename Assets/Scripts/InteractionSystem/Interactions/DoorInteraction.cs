using UnityEngine;
using DialogSystem;

public class DoorInteraction : BasePeekInteraction
{
    // --- CORREÇÃO DO BUG: Usamos 'override' para garantir que esta lógica rode ---
    public override void Interact()
    {
        // 1. Verifica se o DayCycleManager existe
        if (DayCycleManager.Instance == null) return;

        // 2. Verifica se TEM ALGUÉM ESPERANDO (IsVisitorWaiting)
        if (DayCycleManager.Instance.IsVisitorWaiting)
        {
            // Se tem gente, chama a lógica base (entrar na câmera)
            base.Interact();
        }
        else
        {
            // 3. Se NÃO tem ninguém, mostra mensagem e BLOQUEIA a entrada
            if (DialogManager.Instance != null)
            {
                DialogManager.Instance.ShowMessage("Não tem ninguém na porta agora.", 2f);
            }
        }
    }

    protected override void OnPeekReady()
    {
        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.CheckDoorForVisitor();
        }
    }

    public void ExitInteraction()
    {
        StopPeeking();
    }
}