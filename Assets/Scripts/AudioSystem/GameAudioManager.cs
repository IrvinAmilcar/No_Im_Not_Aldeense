using UnityEngine;
using System.Collections;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("Configuração dos Audio Sources")]
    [SerializeField] private AudioSource mainTrackSource;
    [SerializeField] private AudioSource peekTrackSource;

    // Fonte exclusiva para batidas na porta
    [SerializeField] private AudioSource knockingSource;

    [Header("Configuração de Intervalo de Batidas")]
    [SerializeField] private float minKnockInterval = 2.0f; // Tempo mínimo de silêncio entre batidas
    [SerializeField] private float maxKnockInterval = 4.0f; // Tempo máximo de silêncio entre batidas

    [Header("Configuração de Ducking (Rádio)")]
    [SerializeField] private float duckedVolume = 0.3f;
    [SerializeField] private float volumeFadeTime = 0.5f;

    private DayMusicSetup currentDayMusic;
    private float originalMainVolume;
    private Coroutine volumeFadeCoroutine;

    // Variável para controlar a corrotina de batidas
    private Coroutine knockingCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (mainTrackSource != null) originalMainVolume = mainTrackSource.volume;
    }

    public void LoadDayMusic(DayMusicSetup setup)
    {
        currentDayMusic = setup;
        if (currentDayMusic == null) return;

        if (mainTrackSource != null && currentDayMusic.mainDayTrack != null)
        {
            mainTrackSource.Stop();
            mainTrackSource.clip = currentDayMusic.mainDayTrack;
            mainTrackSource.Play();
        }

        if (peekTrackSource != null)
        {
            peekTrackSource.clip = currentDayMusic.specialPeekTrack;
        }
    }

    // --- MÉTODOS DE CONTROLE DE BATIDA (ATUALIZADO) ---

    public void PlayKnocking(AudioClip clip)
    {
        // Garante que paramos qualquer batida anterior antes de começar uma nova
        StopKnocking();

        if (knockingSource != null && clip != null)
        {
            knockingSource.clip = clip;
            knockingSource.loop = false; // IMPORTANTE: Desligamos o loop nativo para controlar o intervalo

            // Inicia a sequência de batidas com intervalo
            knockingCoroutine = StartCoroutine(KnockingSequence(clip));
        }
    }

    public void StopKnocking()
    {
        // Para a corrotina se ela estiver rodando
        if (knockingCoroutine != null)
        {
            StopCoroutine(knockingCoroutine);
            knockingCoroutine = null;
        }

        // Para o som imediatamente
        if (knockingSource != null)
        {
            knockingSource.Stop();
        }
    }

    // Corrotina para criar o intervalo entre batidas
    private IEnumerator KnockingSequence(AudioClip clip)
    {
        while (true)
        {
            // 1. Toca o som
            knockingSource.Play();

            // 2. Espera o som terminar de tocar (comprimento do clipe)
            yield return new WaitForSeconds(clip.length);

            // 3. Espera um intervalo de silêncio aleatório antes de bater de novo
            float interval = Random.Range(minKnockInterval, maxKnockInterval);
            yield return new WaitForSeconds(interval);
        }
    }

    // --- MÉTODOS DE PEEK E DUCKING (MANTIDOS) ---
    public void StartSpecialPeekMusic()
    {
        if (peekTrackSource == null || peekTrackSource.clip == null) return;
        if (mainTrackSource != null) mainTrackSource.Pause();
        peekTrackSource.Play();
    }

    public void StopSpecialPeekMusic()
    {
        if (peekTrackSource == null || peekTrackSource.clip == null) return;
        peekTrackSource.Stop();
        if (mainTrackSource != null) mainTrackSource.UnPause();
    }

    public void DuckMainMusic() { StartVolumeFade(duckedVolume, volumeFadeTime); }
    public void RestoreMainMusicVolume() { StartVolumeFade(originalMainVolume, volumeFadeTime); }

    private void StartVolumeFade(float targetVolume, float duration)
    {
        if (volumeFadeCoroutine != null) StopCoroutine(volumeFadeCoroutine);
        if (mainTrackSource != null) volumeFadeCoroutine = StartCoroutine(FadeVolume(mainTrackSource, targetVolume, duration));
    }

    private IEnumerator FadeVolume(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            yield return null;
        }
        source.volume = targetVolume;
        volumeFadeCoroutine = null;
    }
}