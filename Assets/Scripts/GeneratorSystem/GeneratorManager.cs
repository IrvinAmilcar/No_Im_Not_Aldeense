/*
 * Arquivo: GeneratorManager.cs
 * Descrição: Singleton que gerencia o estado da energia do gerador
 * e persiste entre as cenas (dias).
 */

using UnityEngine;

public class GeneratorManager : MonoBehaviour
{
    public static GeneratorManager Instance { get; private set; }

    // Propriedade pública para ler a energia, mas privada para definir
    public int CurrentEnergy { get; private set; }

    private int currentDay = 1;

    void Awake()
    {
        // Configuração padrão do Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Garante que o gerenciador não seja destruído ao trocar de cena (dia)
        DontDestroyOnLoad(gameObject);

        // Define o estado inicial do gerador
        InitializeGenerator();
    }

    private void InitializeGenerator()
    {
        // Começa com 100% no primeiro dia
        CurrentEnergy = 100;
        currentDay = 1;
        Debug.Log("GeneratorManager Iniciado. Dia 1. Energia: 100%");
    }

    /// <summary>
    /// Chamado por qualquer script de transição de nível/dia.
    /// Reduz a energia do gerador.
    /// </summary>
    public void TransitionToNextDay()
    {
        currentDay++;
        
        // Regra principal: perde 50% por dia
        // Usando o método RemoveEnergy para manter a lógica centralizada
        RemoveEnergy(50); 
        
        Debug.Log($"Transição para o Dia {currentDay}. Energia restante: {CurrentEnergy}%");

        // TODO: Aqui você pode carregar a próxima cena
        // Ex: SceneManager.LoadScene($"Day{currentDay}");
    }

    // --- Métodos de Extensão (Base para "Acidentes") ---

    /// <summary>
    /// Adiciona uma quantidade de energia ao gerador, com limite de 100.
    /// (Base para eventos futuros)
    /// </summary>
    /// <param name="amountToAdd">Energia para adicionar</param>
    public void AddEnergy(int amountToAdd)
    {
        CurrentEnergy += amountToAdd;
        if (CurrentEnergy > 100)
        {
            CurrentEnergy = 100;
        }
        Debug.Log($"Energia ADICIONADA: {amountToAdd}%. Nova energia: {CurrentEnergy}%");
        
        // Futuramente, pode mostrar uma UI de feedback positivo
        // DialogManager.Instance.ShowMessage($"Energia recuperada: +{amountToAdd}%", 2f);
    }

    /// <summary>
    /// Remove uma quantidade de energia do gerador, com limite de 0.
    /// (Base para eventos futuros e transição de dia)
    /// </summary>
    /// <param name="amountToRemove">Energia para remover</param>
    public void RemoveEnergy(int amountToRemove)
    {
        CurrentEnergy -= amountToRemove;
        if (CurrentEnergy < 0)
        {
            CurrentEnergy = 0;
        }
        Debug.Log($"Energia REMOVIDA: {amountToRemove}%. Nova energia: {CurrentEnergy}%");

        // Futuramente, pode mostrar uma UI de feedback negativo
        // DialogManager.Instance.ShowMessage($"Alerta: Perda de energia: -{amountToRemove}%", 2f);
    }

    /// <summary>
    /// Método público simples para qualquer script verificar a energia.
    /// </summary>
    /// <returns>A porcentagem atual de energia (0-100)</returns>
    public int GetCurrentEnergy()
    {
        return CurrentEnergy;
    }
}