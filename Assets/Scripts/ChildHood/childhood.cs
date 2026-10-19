using Kongyou.Dialogue;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    readonly DialogueLine[] lines =
    {
        new DialogueLine("Young Me", "Mom, were you an only child?"),
        new DialogueLine("Mother", "No. I had an older sister."),
        new DialogueLine("Young Me", "Where is she now?"),
        new DialogueLine("Mother", "...She died when she was little."),
        new DialogueLine("Young Me", "Why does Grandma never talk about her?"),
        new DialogueLine("Mother", "Some things are too painful to talk about."),
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

