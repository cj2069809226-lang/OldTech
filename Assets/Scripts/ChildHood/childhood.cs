using Kongyou.Dialogue;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class childhood : MonoBehaviour
{
    [Header("触发")]
    public bool playOnStart = false;

    [Header("每句话对应的人物图（按 0~5 顺序拖入）")]
    public Sprite[] linePortraits = new Sprite[6];

    [Tooltip("某句没有人物图时隐藏人物层；关闭则保留上一张")]
    public bool hideWhenNoPortrait = false;

    [Header("女孩照片闪回")]
    public int flashLineIndex = 3;
    public Image flashOverlay;
    public float flashDuration = 0.25f;

    [Header("回忆结束")]
    public string returnSceneName = "Photo";
    public float returnDelay = 0.2f;

    readonly DialogueLine[] lines =
    {
        new DialogueLine("Young Me", "Mom, were you an <b>only child</b>?", 1f, 1.1f),
        new DialogueLine("Mother", "No. I had an <b>older sister</b>.", 0.82f, 0.94f),
        new DialogueLine("Young Me", "<pause=0.25>Where is she now?", 1f, 1.1f),
        new DialogueLine("Mother", "<pause=0.4>...She <b>died</b> when she was little.", 0.65f, 0.85f),
        new DialogueLine("Young Me", "Why does Grandma <b>never talk about her</b>?", 0.95f, 1.05f),
        new DialogueLine("Mother", "Some things are <b>too painful</b> to talk about.", 0.7f, 0.88f),
    };

    bool playing;
    DialogueSystem dialogue;

    void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        if (playing) return;
        playing = true;

        AudioManager.Instance.PlayMusic("雷雨声");

        // Show 会立刻触发第 0 句的 LineStarted，所以先手动设置第 0 张
        ApplyPortrait(0);

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
        ApplyPortrait(index);

        if (index == flashLineIndex && flashOverlay != null)
            StartCoroutine(FlashRoutine());
    }

    void OnEnded()
    {
        playing = false;

        if (dialogue != null)
        {
            dialogue.LineStarted -= OnLineStarted;
            dialogue.Ended -= OnEnded;
        }

        if (!string.IsNullOrEmpty(returnSceneName))
            StartCoroutine(ReturnToScene());
    }

    IEnumerator ReturnToScene()
    {
        if (returnDelay > 0f)
            yield return new WaitForSecondsRealtime(returnDelay);

        if (!Application.CanStreamedLevelBeLoaded(returnSceneName))
        {
            Debug.LogError("[ChildHood] 场景没有加入 Build Settings：" + returnSceneName);
            yield break;
        }

        SceneManager.LoadScene(returnSceneName);
    }

    void ApplyPortrait(int index)
    {
        Image display = GetDisplay();
        if (display == null) return;

        Sprite sprite = linePortraits != null &&
                        index >= 0 &&
                        index < linePortraits.Length
            ? linePortraits[index]
            : null;

        if (sprite == null)
        {
            if (hideWhenNoPortrait)
                display.enabled = false;
            return;
        }

        display.enabled = true;
        display.sprite = sprite;
        display.preserveAspect = true;
    }

    Image GetDisplay()
    {
        Image display = BackGroundManager.Instance.backGroundImage;
        if (display != null) return display;

        // 跨场景后 BackGroundManager 里缓存的旧 Image 可能已被销毁
        GameObject go = GameObject.Find("BackGround");
        if (go != null)
        {
            display = go.GetComponent<Image>();
            BackGroundManager.Instance.backGroundImage = display;
        }

        return display;
    }

    IEnumerator FlashRoutine()
    {
        flashOverlay.gameObject.SetActive(true);

        Color color = flashOverlay.color;
        color.a = 0.8f;
        flashOverlay.color = color;
        yield return null;

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(0.8f, 0f, Mathf.Clamp01(t / flashDuration));
            flashOverlay.color = color;
            yield return null;
        }

        color.a = 0f;
        flashOverlay.color = color;
        flashOverlay.gameObject.SetActive(false);
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

