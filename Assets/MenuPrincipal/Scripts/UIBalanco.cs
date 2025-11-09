using UnityEngine;

public class UIBalanco : MonoBehaviour
{
    [Header("Configuracoes de Movimento")]
    [SerializeField] private float raioMaximo = 20f; // O quao longe do centro o alvo pode ir
    [SerializeField] private float smoothTime = 2.5f;  // Quao "mole" e o movimento. VALORES MAIORES = MAIS SUAVE.

    [Header("Intervalo do Alvo")]
    [SerializeField] private float minTempoNovoAlvo = 2.0f; // Tempo minimo para mudar de alvo
    [SerializeField] private float maxTempoNovoAlvo = 5.0f; // Tempo maximo para mudar de alvo

    // Variaveis de Posicao
    private RectTransform rectTransform;
    private Vector2 posicaoInicial;
    private Vector2 posAlvoAtual;
    private Vector2 velocidadePosicao; // Controlado pelo SmoothDamp
    private float timerTrocaAlvo;

    // Variaveis de Rotacao (logica identica)
    [Header("Configuracoes de Rotacao")]
    [SerializeField] private float rotMaxima = 5f;
    [SerializeField] private float smoothTimeRot = 3.0f;
    private float rotacaoInicialZ;
    private float rotacaoAtual;
    private float rotAlvoAtual;
    private float velocidadeRotacao; // Controlado pelo SmoothDamp

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // Define a ancora
        posicaoInicial = rectTransform.anchoredPosition;
        rotacaoInicialZ = rectTransform.localEulerAngles.z;
        rotacaoAtual = rotacaoInicialZ; // Usei a variavel do seu script anterior, corrigindo para o contexto

        // Define o primeiro alvo
        DefinirNovoAlvo();
    }

    void Update()
    {
        // Contagem regressiva para trocar o alvo
        timerTrocaAlvo -= Time.deltaTime;
        if (timerTrocaAlvo <= 0)
        {
            DefinirNovoAlvo();
        }

        // --- Mover Suavemente ate o Alvo ---

        // POSICAO:
        // Move a posicao atual em direcao ao "posAlvoAtual"
        rectTransform.anchoredPosition = Vector2.SmoothDamp(
            rectTransform.anchoredPosition, // Onde estou
            posAlvoAtual,                   // Para onde quero ir
            ref velocidadePosicao,          // Minha velocidade atual
            smoothTime                      // Tempo para chegar la
        );

        // ROTACAO:
        // Move a rotacao atual em direcao ao "rotAlvoAtual"
        float rotacaoAtual = rectTransform.localEulerAngles.z; // Pega a rotacao atual
        float rotacaoSuave = Mathf.SmoothDampAngle(
            rotacaoAtual,                   // Onde estou
            rotAlvoAtual,                   // Para onde quero ir
            ref velocidadeRotacao,          // Minha velocidade atual
            smoothTimeRot                   // Tempo para chegar la
        );

        rectTransform.localEulerAngles = new Vector3(0, 0, rotacaoSuave);
    }

    void DefinirNovoAlvo()
    {
        // Define um novo ponto aleatorio DENTRO de um circulo
        Vector2 pontoAleatorio = Random.insideUnitCircle * raioMaximo;

        // O novo alvo e a ancora central + o ponto aleatorio
        posAlvoAtual = posicaoInicial + pontoAleatorio;

        // Define uma nova rotacao aleatoria
        rotAlvoAtual = rotacaoInicialZ + Random.Range(-rotMaxima, rotMaxima);

        // Reseta o timer
        timerTrocaAlvo = Random.Range(minTempoNovoAlvo, maxTempoNovoAlvo);
    }
}