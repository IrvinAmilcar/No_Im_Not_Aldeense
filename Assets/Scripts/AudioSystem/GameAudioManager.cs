using UnityEngine;
using System.Collections;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("Configuração dos Audio Sources")]
    [Tooltip("Arraste o AudioSource que toca a música de fundo aqui. (Deve ser 2D)")]
    [SerializeField] private AudioSource mainTrackSource;
    [SerializeField] private AudioSource peekTrackSource;
    [SerializeField] private AudioSource knockingSource;

    [Header("Configuração de Intervalo de Batidas")]
    [SerializeField] private float minKnockInterval = 2.0f;
    [SerializeField] private float maxKnockInterval = 4.0f;

    [Header("Configuração de Ducking")]
    [SerializeField] private float duckedVolume = 0.3f;
    [SerializeField] private float volumeFadeTime = 0.5f;

    private DayMusicSetup currentDayMusic; // Memória da música do dia
    private float originalMainVolume = 1.0f;
    private Coroutine volumeFadeCoroutine;
    private Coroutine knockingCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (mainTrackSource != null)
        {
            originalMainVolume = mainTrackSource.volume;
            // Segurança: se começar mudo por engano, define 1
            if (originalMainVolume <= 0.01f) originalMainVolume = 1.0f;
        }
    }

    public void LoadDayMusic(DayMusicSetup setup)
    {
        currentDayMusic = setup; // Salva a referência!

        if (mainTrackSource == null) return;

        // Reseta volume e para corrotinas de fade
        if (volumeFadeCoroutine != null) StopCoroutine(volumeFadeCoroutine);
        mainTrackSource.volume = originalMainVolume;

        if (setup != null && setup.mainDayTrack != null)
        {
            // Só troca se for diferente para não reiniciar a música do nada
            if (mainTrackSource.clip != setup.mainDayTrack)
            {
                mainTrackSource.Stop();
                mainTrackSource.clip = setup.mainDayTrack;
                mainTrackSource.loop = true;
                mainTrackSource.Play();
            }
            else if (!mainTrackSource.isPlaying)
            {
                mainTrackSource.Play();
            }
        }
        else
        {
            mainTrackSource.Stop(); // Dia sem música
        }
    }

    // --- CORREÇÃO: MÚSICA DE VISITANTE ---
    public void PlayVisitorMusic(AudioClip visitorClip)
    {
        if (mainTrackSource == null || visitorClip == null) return;

        // Para fades antigos para não baixar o volume do terror
        if (volumeFadeCoroutine != null) StopCoroutine(volumeFadeCoroutine);

        // Força o volume original (caso estivesse baixo por causa do rádio)
        mainTrackSource.volume = originalMainVolume;

        if (mainTrackSource.clip != visitorClip)
        {
            Debug.Log($"[Audio] Trocando para música do visitante: {visitorClip.name}");
            mainTrackSource.Stop();
            mainTrackSource.clip = visitorClip;
            mainTrackSource.loop = true;
            mainTrackSource.Play();
        }
    }

    // --- CORREÇÃO: RESTAURAR MÚSICA ---
    public void RestoreDayMusic()
    {
        if (mainTrackSource == null) return;

        // Para fades antigos
        if (volumeFadeCoroutine != null) StopCoroutine(volumeFadeCoroutine);
        mainTrackSource.volume = originalMainVolume;

        // Se existe uma música configurada para o dia atual
        if (currentDayMusic != null && currentDayMusic.mainDayTrack != null)
        {
            // Se a música atual não é a do dia (ex: ainda está tocando a do visitante), troca de volta
            if (mainTrackSource.clip != currentDayMusic.mainDayTrack || !mainTrackSource.isPlaying)
            {
                Debug.Log($"[Audio] Restaurando música do dia: {currentDayMusic.mainDayTrack.name}");
                mainTrackSource.Stop();
                mainTrackSource.clip = currentDayMusic.mainDayTrack;
                mainTrackSource.loop = true;
                mainTrackSource.Play();
            }
        }
        else
        {
            // Se o dia não tem música, garante silêncio ao sair do visitante
            if (mainTrackSource.isPlaying) mainTrackSource.Stop();
        }
    }

    public void StopMainMusic()
    {
        if (mainTrackSource != null) mainTrackSource.Stop();
    }

    // --- MÉTODOS DE BATIDA (MANTIDOS) ---
    public void PlayKnocking(AudioClip clip)
    {
        StopKnocking();
        if (knockingSource != null && clip != null)
        {
            knockingSource.clip = clip;
            knockingSource.loop = false;
            knockingCoroutine = StartCoroutine(KnockingSequence(clip));
        }
    }

    public void StopKnocking()
    {
        if (knockingCoroutine != null) { StopCoroutine(knockingCoroutine); knockingCoroutine = null; }
        if (knockingSource != null) knockingSource.Stop();
    }

    private IEnumerator KnockingSequence(AudioClip clip)
    {
        while (true)
        {
            knockingSource.Play();
            yield return new WaitForSeconds(clip.length);
            float interval = Random.Range(minKnockInterval, maxKnockInterval);
            yield return new WaitForSeconds(interval);
        }
    }

    // --- PEEK / DUCKING ---
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