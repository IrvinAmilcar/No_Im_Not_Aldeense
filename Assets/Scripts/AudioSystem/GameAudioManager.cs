using UnityEngine;
using System.Collections;

/// <summary>
/// COMO USAR:
/// 1. Crie um objeto vazio na sua cena (ex: "GameAudio").
/// 2. Adicione este script (GameAudioManager) a ele.
/// 3. Adicione DOIS componentes "Audio Source" neste mesmo objeto.
/// 4. Arraste o primeiro Audio Source para o campo "Main Track Source".
/// 5. Arraste o segundo Audio Source para o campo "Peek Track Source".
/// 6. Configure os Audio Sources no Inspector:
///    - Marque "Play On Awake" e "Loop" APENAS no "Main Track Source".
///    - Desmarque "Play On Awake" nos dois. (O script vai controlar)
///    - Marque "Loop" nos dois.
///    - Coloque "Spatial Blend" em 0 (para 2D) nos dois.
/// 7. Em algum script que gerencia o início do dia (ex: GameManager), chame:
///    GameAudioManager.Instance.LoadDayMusic(configuracaoDeMusicaParaEsteDia);
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static GameAudioManager Instance { get; private set; }

    [Header("Configuração dos Audio Sources")]
    [Tooltip("O AudioSource que tocará a música principal do dia.")]
    [SerializeField] private AudioSource mainTrackSource;
    
    [Tooltip("O AudioSource que tocará a música especial do 'peek'.")]
    [SerializeField] private AudioSource peekTrackSource;

    [Header("Configuração de Ducking (Rádio)")]
    [Tooltip("O volume (0-1) que a música principal deve ficar ao interagir com o rádio.")]
    [SerializeField] private float duckedVolume = 0.3f;
    [Tooltip("O tempo em segundos para a música abaixar ou voltar ao normal.")]
    [SerializeField] private float volumeFadeTime = 0.5f;

    private DayMusicSetup currentDayMusic;
    private float originalMainVolume;
    private Coroutine volumeFadeCoroutine;

    private void Awake()
    {
        // Implementação do Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Opcional: Não destrói este objeto ao carregar novas cenas (dias)
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Guarda o volume original para restaurar depois do "ducking"
        if (mainTrackSource != null)
        {
            originalMainVolume = mainTrackSource.volume;
        }
    }

    /// <summary>
    /// Ponto 4: Carrega a configuração de música para o dia atual.
    /// Chame isso a partir do seu GameManager quando um dia/cena começar.
    /// </summary>
    /// <param name="setup">O ScriptableObject contendo as faixas do dia.</param>
    public void LoadDayMusic(DayMusicSetup setup)
    {
        currentDayMusic = setup;

        if (currentDayMusic == null)
        {
            Debug.LogError("Setup de música nulo carregado!", this);
            return;
        }

        // Configura a música principal
        if (mainTrackSource != null && currentDayMusic.mainDayTrack != null)
        {
            mainTrackSource.Stop();
            mainTrackSource.clip = currentDayMusic.mainDayTrack;
            mainTrackSource.Play();
        }

        // Configura a música de "peek"
        if (peekTrackSource != null && currentDayMusic.specialPeekTrack != null)
        {
            peekTrackSource.clip = currentDayMusic.specialPeekTrack;
        }
        else if (peekTrackSource != null)
        {
            // Garante que não tem um clip antigo
            peekTrackSource.clip = null; 
        }
    }

    /// <summary>
    /// Ponto 2: Toca a música especial do "peek" (olho mágico).
    /// Pausa a música principal.
    /// </summary>
    public void StartSpecialPeekMusic()
    {
        // Só toca se tiver uma música especial configurada para este dia
        if (peekTrackSource == null || peekTrackSource.clip == null)
        {
            // Se não tem música especial, podemos optar por só pausar a principal
            // mainTrackSource.Pause(); 
            // ou não fazer nada. Por enquanto, não faz nada:
            return; 
        }

        if (mainTrackSource != null)
        {
            mainTrackSource.Pause();
        }
        
        peekTrackSource.Play();
    }

    /// <summary>
    /// Ponto 2: Para a música especial do "peek" e retoma a música principal.
    /// </summary>
    public void StopSpecialPeekMusic()
    {
        if (peekTrackSource == null || peekTrackSource.clip == null)
        {
            // Se não tinha música especial, talvez a principal tenha sido pausada?
            // mainTrackSource.UnPause();
            // Pela lógica de Start, se não tinha clip, nada foi pausado.
            return;
        }

        peekTrackSource.Stop();

        if (mainTrackSource != null)
        {
            mainTrackSource.UnPause();
        }
    }

    /// <summary>
    /// Ponto 3: Abaixa o volume da música principal (ex: para o rádio).
    /// </summary>
    public void DuckMainMusic()
    {
        StartVolumeFade(duckedVolume, volumeFadeTime);
    }

    /// <summary>
    /// Ponto 3: Restaura o volume normal da música principal.
    /// </summary>
    public void RestoreMainMusicVolume()
    {
        StartVolumeFade(originalMainVolume, volumeFadeTime);
    }

    // --- Métodos Privados ---

    private void StartVolumeFade(float targetVolume, float duration)
    {
        // Para qualquer fade de volume anterior que esteja acontecendo
        if (volumeFadeCoroutine != null)
        {
            StopCoroutine(volumeFadeCoroutine);
        }
        
        if(mainTrackSource != null)
        {
            volumeFadeCoroutine = StartCoroutine(FadeVolume(mainTrackSource, targetVolume, duration));
        }
    }

    private IEnumerator FadeVolume(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime; // Usamos unscaledDeltaTime para funcionar se o jogo estiver pausado
            source.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            yield return null;
        }

        source.volume = targetVolume;
        volumeFadeCoroutine = null;
    }
}