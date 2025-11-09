using UnityEngine;
using System.Collections; // Necessário para Coroutines
using UnityEngine.UI;     // Necessário para o componente Image

public class SpriteSlideshowCoroutine : MonoBehaviour
{
    [Tooltip("A lista de sprites que serão exibidos no slideshow.")]
    public Sprite[] sprites;

    [Tooltip("O tempo (em segundos) que cada sprite ficará na tela.")]
    public float tempoPorSprite = 1.0f;

    // Armazena o componente que vai exibir o sprite (pode ser UI ou 2D)
    private Component renderizadorAlvo;
    private int indiceAtual = 0;

    void Start()
    {
        // Tenta encontrar o componente SpriteRenderer (para 2D)
        renderizadorAlvo = GetComponent<SpriteRenderer>();

        // Se não achar, tenta encontrar o componente Image (para UI)
        if (renderizadorAlvo == null)
        {
            renderizadorAlvo = GetComponent<Image>();
        }

        // Se não achar nenhum dos dois, avisa no console
        if (renderizadorAlvo == null)
        {
            Debug.LogError("SpriteSlideshow: Nenhum SpriteRenderer ou Image encontrado!");
            this.enabled = false; // Desativa o script
            return;
        }

        // Garante que temos sprites para mostrar
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("SpriteSlideshow: Nenhuma sprite foi definida no array.");
            this.enabled = false; // Desativa o script
            return;
        }

        // Inicia a rotina do slideshow
        StartCoroutine(SlideshowLoop());
    }

    /// <summary>
    /// Esta é a Coroutine que roda em loop.
    /// </summary>
    private IEnumerator SlideshowLoop()
    {
        // Loop infinito
        while (true)
        {
            // 1. Atualiza o sprite na tela
            AtualizarSprite();

            // 2. Avança para o próximo índice
            indiceAtual++;

            // 3. Se chegou ao fim da lista, volta ao início (loop)
            if (indiceAtual >= sprites.Length)
            {
                indiceAtual = 0;
            }

            // 4. Pausa a execução desta rotina pelo tempo determinado
            yield return new WaitForSeconds(tempoPorSprite);
        }
    }

    /// <summary>
    /// Função auxiliar que aplica o sprite atual ao componente correto.
    /// </summary>
    void AtualizarSprite()
    {
        if (renderizadorAlvo is SpriteRenderer sr)
        {
            sr.sprite = sprites[indiceAtual];
        }
        else if (renderizadorAlvo is Image img)
        {
            img.sprite = sprites[indiceAtual];
        }
    }
}