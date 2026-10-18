using System.Collections;
using UnityEngine;

/// <summary>
/// 音效 / 音乐总管。写法跟 UImanager 一样：单例 + 静态 Instance，走到哪儿都能调。
///
///     AudioManager.Instance.PlaySound("door_knock");                    // 一个音效（可叠着响）
///     AudioManager.Instance.PlaySound("step", 0.8f);                    // 顺便调音量
///     AudioManager.Instance.PlaySoundRandomPitch("step");               // 带随机音高（脚步、连点）
///     AudioManager.Instance.PlayMusic("bgm_night");                     // 背景音乐（同一首不会重头播）
///     AudioManager.Instance.StopMusic();                                // 淡出音乐
///     AudioManager.Instance.MusicVolume = 0.4f;                         // 音量（0~1），改完立刻生效
///
/// 文件位置：
///     音效 Assets/Resources/Sounds/xxx.wav
///     音乐 Assets/Resources/Music/xxx.wav
///   （wav / mp3 / ogg 都行，代码里不写后缀。放错文件夹也会兜底找一下，找不到只打警告不会崩。）
///
/// 播放器挂在运行时自动创建的常驻物体 [Audio] 上（和 UImanager 自己找 Canvas 一个思路），
/// 不用你在场景里摆 AudioSource；两个 AudioSource 一个管音效、一个管音乐，都是 2D 播放
/// （音量不随距离衰减，适合你这种 UI 场景）。
/// 注意：场景里要有 AudioListener（主摄像机上默认就有），否则什么都听不到。
/// </summary>
public class AudioManager
{
    private static AudioManager instance = new AudioManager();
    public static AudioManager Instance => instance;

    private AudioManager() { }

    const string SfxFolder = "Sounds/";
    const string MusicFolder = "Music/";

    private float sfxVolume = 1f;
    private float musicVolume = 0.6f;

    /// <summary>每次播放都往 Console 打一条日志。排查"为什么没声音"时很有用：
    /// 没有日志 = 那行代码根本没执行到；有日志但听不见 = 静音/音量/监听端的问题。上线前可以关掉。</summary>
    public bool LogPlayback = true;

    private GameObject host;
    private AudioSource sfxSource;
    private AudioSource musicSource;
    private Coroutine fadeRoutine;
    private string currentMusic;

    /// <summary>音效音量（0~1）。</summary>
    public float SfxVolume
    {
        get { return sfxVolume; }
        set { sfxVolume = Mathf.Clamp01(value); }
    }

    /// <summary>音乐音量（0~1），改完立刻对正在播的音乐生效。</summary>
    public float MusicVolume
    {
        get { return musicVolume; }
        set
        {
            musicVolume = Mathf.Clamp01(value);
            if (musicSource != null) musicSource.volume = musicVolume;
        }
    }

    /// <summary>有没有音乐在播。</summary>
    public bool IsMusicPlaying
    {
        get { return musicSource != null && musicSource.isPlaying; }
    }

    // ---------------- 播放 ----------------

    /// <summary>播一个音效。可以多个同时响（点击、开门、脚步都适合）。</summary>
    public void PlaySound(string fileName, float volumeScale = 1f, float pitch = 1f)
    {
        AudioClip clip = LoadClip(SfxFolder, fileName);
        if (clip == null) return;

        if (LogPlayback)
            Debug.Log("[Audio] 播放音效：" + fileName + "（音量 "
                      + (sfxVolume * Mathf.Clamp01(volumeScale)).ToString("0.00") + "）");

        Sfx.pitch = pitch;
        Sfx.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * Mathf.Clamp01(volumeScale)));
    }

    /// <summary>带随机音高的音效。重复播同一个音（脚步等）用它，不然会像机关枪。</summary>
    public void PlaySoundRandomPitch(string fileName, float minPitch = 0.94f, float maxPitch = 1.06f,
                                     float volumeScale = 1f)
    {
        PlaySound(fileName, volumeScale, Random.Range(minPitch, maxPitch));
    }

    /// <summary>播背景音乐（循环）。同一首正在播就不重头播；restart = true 可以强制重播。</summary>
    public void PlayMusic(string fileName, bool restart = false)
    {
        AudioClip clip = LoadClip(MusicFolder, fileName);
        if (clip == null) return;

        if (!restart && currentMusic == fileName && IsMusicPlaying) return;

        if (LogPlayback) Debug.Log("[Audio] 播放音乐：" + fileName);

        if (fadeRoutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        Music.clip = clip;
        Music.loop = true;
        Music.volume = musicVolume;
        Music.Play();

        currentMusic = fileName;
    }

    /// <summary>淡出并停掉背景音乐。</summary>
    public void StopMusic(float fadeSeconds = 0.4f)
    {
        currentMusic = null;

        if (musicSource == null || !musicSource.isPlaying) return;

        if (LogPlayback) Debug.Log("[Audio] 停止音乐（淡出 " + fadeSeconds + " 秒）");

        if (fadeRoutine != null) CoroutineRunner.Instance.StopCoroutine(fadeRoutine);
        fadeRoutine = CoroutineRunner.Instance.StartCoroutine(FadeMusicOut(fadeSeconds));
    }

    /// <summary>立刻掐掉所有正在播的音效（比如切场景前）。</summary>
    public void StopAllSounds()
    {
        if (sfxSource != null) sfxSource.Stop();
    }

    // ---------------- 内部 ----------------

    private IEnumerator FadeMusicOut(float seconds)
    {
        float start = musicSource.volume;
        float t = 0f;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / seconds));
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = musicVolume;
        fadeRoutine = null;
    }

    private GameObject Host
    {
        get
        {
            if (host != null) return host;

            host = new GameObject("[Audio]");
            UnityEngine.Object.DontDestroyOnLoad(host);
            return host;
        }
    }

    private AudioSource Sfx
    {
        get
        {
            if (sfxSource != null) return sfxSource;

            sfxSource = Host.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;      // 2D
            return sfxSource;
        }
    }

    private AudioSource Music
    {
        get
        {
            if (musicSource != null) return musicSource;

            musicSource = Host.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;    // 2D
            return musicSource;
        }
    }

    private AudioClip LoadClip(string folder, string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;

        AudioClip clip = Resources.Load<AudioClip>(folder + fileName);
        if (clip != null) return clip;

        // 放错文件夹也认一下（很多人会把音频全丢进 Sounds）
        string otherFolder = folder == SfxFolder ? MusicFolder : SfxFolder;
        clip = Resources.Load<AudioClip>(otherFolder + fileName);
        if (clip != null) return clip;

        Debug.LogWarning("[Audio] 找不到音频：Resources/" + folder + fileName
                         + "（也找过 " + otherFolder + "）");
        return null;
    }
}
