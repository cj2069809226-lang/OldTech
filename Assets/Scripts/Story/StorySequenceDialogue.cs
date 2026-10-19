using System;
using System.Collections;
using Kongyou.Dialogue;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 通用剧情段播放器。
///
/// lines          每一句台词
/// lineBackgrounds 与 lines 一一对应；留空则保持上一张背景
/// linePortraits   与 lines 一一对应；留空则不显示人物
///
/// 背景和人物都是场景里的真实 UI Image，不在运行时生成。
/// 保证背景 Canvas 和人物 Canvas 的 Sorting Order 低于 DialogueSystem 的 500，
/// 这样人物会位于对话框后面。
/// </summary>
public class StorySequenceDialogue : MonoBehaviour
{
    [Header("播放")]
    public bool playOnStart = true;
    public DialogueLine[] lines;

    [Header("每句对应的背景（可留空，留空保持上一张）")]
    public Sprite[] lineBackgrounds;

    [Header("每句对应的人物（可留空，留空不显示人物）")]
    public Sprite[] linePortraits;

    [Tooltip("没有人物图时隐藏人物层；关闭则保留上一张人物")]
    public bool hideCharacterWhenNoPortrait = true;

    [Header("音频")]
    [Tooltip("进入场景时播放的环境音/音乐，留空则不切换")]
    public string sceneMusicName;

    [Tooltip("与 lines 一一对应；留空不播放")]
    public string[] lineSfxNames;
    [Range(0f, 1f)]
    public float lineSfxVolume = 0.9f;

    [Header("场景引用")]
    public Image backgroundImage;
    public Image characterImage;

    [Header("结束后（可选）")]
    public string nextSceneName;
    public float delayBeforeNextScene = 0.3f;

    [Tooltip("整段播放完成时触发，可在 Inspector 里绑定其他逻辑")]
    public UnityEvent onFinished;

    /// <summary>整段播放完成时触发；和 onFinished 二选一即可。</summary>
    public event Action Finished;

    public bool IsPlaying { get { return playing; } }
    public bool IsFinished { get; private set; }

    bool playing;
    DialogueSystem dialogue;

    void Start()
    {
        if (!string.IsNullOrEmpty(sceneMusicName))
            AudioManager.Instance.PlayMusic(sceneMusicName);

        if (playOnStart)
            Play();
    }

    public void Play()
    {
        if (playing) return;

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("[Story] 没有配置台词。", this);
            return;
        }

        playing = true;
        IsFinished = false;

        // Show 会立即触发第 0 句的 LineStarted，所以先手动应用第 0 句
        ApplyLine(0);
        PlayLineSfx(0);

        DialogueSystem.Show(lines);
        dialogue = DialogueSystem.Instance;

        if (dialogue != null)
        {
            dialogue.LineStarted += OnLineStarted;
            dialogue.Ended += OnEnded;
        }
    }

    void OnLineStarted(int index)
    {
        ApplyLine(index);
        PlayLineSfx(index);
    }

    void PlayLineSfx(int index)
    {
        if (lineSfxNames == null || index < 0 || index >= lineSfxNames.Length)
            return;

        string soundName = lineSfxNames[index];
        if (!string.IsNullOrEmpty(soundName))
            AudioManager.Instance.PlaySound(soundName, lineSfxVolume);
    }

    void ApplyLine(int index)
    {
        ApplyBackground(index);
        ApplyPortrait(index);
    }

    void ApplyBackground(int index)
    {
        if (backgroundImage == null) return;

        Sprite sprite = GetAt(lineBackgrounds, index);
        if (sprite != null)
            backgroundImage.sprite = sprite;
    }

    void ApplyPortrait(int index)
    {
        if (characterImage == null) return;

        Sprite sprite = GetAt(linePortraits, index);

        if (sprite == null)
        {
            if (hideCharacterWhenNoPortrait)
                characterImage.enabled = false;
            return;
        }

        characterImage.enabled = true;
        characterImage.sprite = sprite;
        characterImage.preserveAspect = true;
    }

    static Sprite GetAt(Sprite[] array, int index)
    {
        if (array == null || index < 0 || index >= array.Length)
            return null;

        return array[index];
    }

    void OnEnded()
    {
        playing = false;
        IsFinished = true;

        if (dialogue != null)
        {
            dialogue.LineStarted -= OnLineStarted;
            dialogue.Ended -= OnEnded;
        }

        if (Finished != null) Finished();
        if (onFinished != null) onFinished.Invoke();

        if (!string.IsNullOrEmpty(nextSceneName))
            StartCoroutine(LoadNextScene());
    }

    IEnumerator LoadNextScene()
    {
        if (delayBeforeNextScene > 0f)
            yield return new WaitForSecondsRealtime(delayBeforeNextScene);

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError("[Story] 场景没有加入 Build Settings：" + nextSceneName, this);
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    void OnDestroy()
    {
        if (dialogue != null)
        {
            dialogue.LineStarted -= OnLineStarted;
            dialogue.Ended -= OnEnded;
        }
    }
}
