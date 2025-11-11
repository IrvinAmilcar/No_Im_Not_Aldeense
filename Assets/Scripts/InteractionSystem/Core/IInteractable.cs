/*
 * Arquivo: IInteractable.cs
 * Descriçao: Interface (contrato) que define os métodos básicos 
 * de qualquer objeto interativo no jogo.
 */

public interface IInteractable
{
    
    /// Chamado quando o jogador pressiona a tecla de interação.
    
    void Interact();

  
    /// Chamado pelo PlayerInteraction quando o objeto entra em foco (hover).
   
    void OnFocus();

    
    /// Chamado pelo PlayerInteraction quando o objeto sai de foco.
    
    void OnLoseFocus();
}