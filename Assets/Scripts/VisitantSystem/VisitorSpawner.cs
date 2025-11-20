using UnityEngine;
using System.Collections.Generic;

public class VisitorSpawner : MonoBehaviour
{
    public static VisitorSpawner Instance { get; private set; }

    private Queue<VisitorProfile> dailyQueue = new Queue<VisitorProfile>();
    private VisitorProfile currentActiveVisitor;

    void Awake()
    {
        Instance = this;
    }

    public void SetDailyQueue(List<VisitorProfile> visitors)
    {
        dailyQueue.Clear();
        foreach (var v in visitors)
        {
            dailyQueue.Enqueue(v);
        }
        Debug.Log($"Fila do dia definida. {dailyQueue.Count} visitantes pendentes.");

        // Toca batida na porta se tiver alguém?
        if (dailyQueue.Count > 0)
        {
            // AudioManager.Instance.PlayKnockSound();
        }
    }

    public bool HasVisitorAtDoor()
    {
        return dailyQueue.Count > 0 || currentActiveVisitor != null;
    }

    // Chamado quando o jogador clica no Olho Mágico
    public void CheckDoor()
    {
        // Se já tem alguém ativo (ex: você saiu do olho mágico sem decidir), mantém ele.
        if (currentActiveVisitor == null && dailyQueue.Count > 0)
        {
            currentActiveVisitor = dailyQueue.Dequeue();
        }

        if (currentActiveVisitor != null)
        {
            // INICIA A UI DO PEEPHOLE
            PeepholeManager.Instance.StartEncounter(currentActiveVisitor);
        }
        else
        {
            // Ninguém na porta
            DialogSystem.DialogManager.Instance.ShowMessage("Não tem ninguém lá fora agora.", 2f);
        }
    }

    // Chamado pelo DayCycleManager quando o visitante vai embora
    public void ClearCurrentVisitor()
    {
        currentActiveVisitor = null;

        // Se ainda tem gente na fila, toca som de batida depois de um tempo
        if (dailyQueue.Count > 0)
        {
            Invoke("PlayKnock", 5f); // Simula o próximo chegando
        }
    }

    void PlayKnock() { Debug.Log("Toc, Toc! Próximo visitante chegou."); }
}