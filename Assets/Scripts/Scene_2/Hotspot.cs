using Kongyou.Dialogue;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 世界空间里的"可点击的交互点"（比如长背景上的那两处）。
///
/// 用法：
///   1. 在背景对象上右键 → Create Empty，建两个空物体（作为背景的子物体，这样以后挪背景它们跟着走）；
///   2. 在 Scene 视图里把它们分别拖到那两处位置上；
///   3. 每个都挂上本脚本——没有 Collider2D 的话它会自动补一个 BoxCollider2D，
///      然后在 Inspector 里把 Size 调到刚好盖住那个位置（差不多 1x2 就行）；
///   4. 在 Lines 里填点击后要播的台词（直接在 Inspector 里加几条），要做别的事就挂 onInteract。
///   5. 想让"点了门出现 fish_eye，说完切回来"有两种做法：
///      · 【推荐，能盖住角色和背景】把 Overlay 拖一个全屏 UI 覆盖层进来，
///        它会把世界里的东西全盖住、但盖不住文案（文案系统的 Canvas 排序更高）；
///      · 【只换背景图】把背景拖进 World Background 或 UI Background，Swap Sprite 拖临时图。
///        这种做法只是把背景这张图换掉，站在前面的角色还在，盖不住。
///
/// 点击的判定是"鼠标点是否落在自己的 Collider2D 里"，不经过 UI，
/// 所以镜头平移、缩放、角色走动都不会错位。
/// </summary>
[DisallowMultipleComponent]
public class Hotspot : MonoBehaviour
{
    [Header("点一下要播的台词（留空就只触发 onInteract）")]
    public DialogueLine[] lines;

    [Header("第二次点击（可选）")]
    [Tooltip("第二次点击要播的台词。留空就沿用第一次的台词")]
    public DialogueLine[] secondLines;

    [Tooltip("第二次点击显示的覆盖层。留空就沿用第一次的覆盖层")]
    public GameObject secondOverlay;

    [Tooltip("第二次点击要换的背景图。留空就沿用第一次的图")]
    public Sprite secondSwapSprite;

    [Header("点一下还要做什么（可选：切场景、开面板、给道具…）")]
    public UnityEvent onInteract;

    [Tooltip("只准触发一次（比如已经捡走的东西）")]
    public bool onlyOnce;

    [Tooltip("点中时在 Console 打一条日志，方便确认位置有没有摆对")]
    public bool logOnClick = true;

    [Header("说话时临时换背景（可选）")]
    [Tooltip("要临时换图的背景：世界空间的话填这里（SpriteRenderer）")]
    public SpriteRenderer worldBackground;

    [Tooltip("要临时换图的背景：Canvas 里的 UI 的话填这里（Image）")]
    public Image uiBackground;

    [Tooltip("临时换成哪张图。留空就按下面的路径去 Resources 里加载")]
    public Sprite swapSprite;

    [Tooltip("Swap Sprite 留空时用这个路径加载，比如 BackGrounds/fish_eye")]
    public string swapResourceName = "BackGrounds/fish_eye";

    [Tooltip("如果这个交互点不播台词，换图停留几秒后自动切回")]
    public float holdSeconds = 2f;

    [Header("全屏覆盖层（推荐：能盖住角色和背景，盖不住文案）")]
    [Tooltip("要全屏盖住的对象：一般是一个 Canvas(Screen Space - Overlay, Sort Order 100) 下的全屏 Image。" +
             "把它的 GameObject 拖到这里，并在场景里默认让它隐藏")]
    public GameObject overlay;

    [Tooltip("覆盖层上的 CanvasGroup。填了就会淡入淡出，不填就是硬开硬关")]
    public CanvasGroup overlayGroup;

    [Tooltip("淡入时长（秒）")]
    public float overlayFadeIn = 0.12f;

    [Tooltip("淡出时长（秒）。想让画面慢慢回来就调大一点")]
    public float overlayFadeOut = 0.35f;

    [Header("出现时先藏起来的东西（可选）")]
    [Tooltip("显示这段时间里要隐藏的对象：把背景、角色这些拖进来就行。" +
             "结束时会按它们原本的显示/隐藏状态还原。覆盖层自己会被跳过，不用管")]
    public GameObject[] hideWhileShowing;

    [Header("Click Area")]
    [Tooltip("Multiplier for the auto-generated hit box. 1 = original size, 2 = double size.")]
    [Min(0.1f)]
    public float hitAreaScale = 2f;

    Collider2D hitArea;
    bool used;
    bool firstStageDone;
    bool secondStageDone;

    void Awake()
    {
        hitArea = GetComponent<Collider2D>();
        if (hitArea != null) return;

        // 没有碰撞盒就点不到，这里自动补一个：有 SpriteRenderer 就按图片大小给
        var box = gameObject.AddComponent<BoxCollider2D>();
        float scale = hitAreaScale > 0f ? hitAreaScale : 2f;

        float width = 1f;
        float height = 1f;
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.sprite != null)
        {
            width = renderer.sprite.bounds.size.x;
            height = renderer.sprite.bounds.size.y;
        }

        box.size = new Vector2(width, height) * scale;
        hitArea = box;
        Debug.Log("[Hotspot] " + name + " hit area = " + box.size + " (scale " + scale + ")", this);
    }

    void Update()
    {
        if (hitArea == null) return;
        if (onlyOnce && used) return;
        if (!KeyInput.MousePressedThisFrame()) return;
        if (DialogueSystem.IsPlaying) return;      // 旁白正在播的时候不抢点击

        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.LogWarning("[Hotspot] 找不到 Main Camera（摄像机的 Tag 要是 MainCamera）", this);
            enabled = false;
            return;
        }

        Vector2 screen = KeyInput.MouseScreenPosition();
        Vector3 world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));

        if (!hitArea.OverlapPoint(world)) return;

        Interact();
    }

    /// <summary>触发这个交互点（也可以从别的地方直接调）。</summary>
    public void Interact()
    {
        bool hasSecond =
            (secondLines != null && secondLines.Length > 0) ||
            secondOverlay != null ||
            secondSwapSprite != null;

        if (secondStageDone) return;
        if (!hasSecond && onlyOnce && used) return;

        bool useSecond = hasSecond && firstStageDone;

        DialogueLine[] stageLines = useSecond ? secondLines : lines;
        GameObject stageOverlay = useSecond ? secondOverlay : overlay;
        Sprite stageSwap = useSecond ? secondSwapSprite : swapSprite;

        if (logOnClick) Debug.Log("[Hotspot] 点到了：" + name, this);

        if (stageLines != null && stageLines.Length > 0)
            DialogueSystem.Show(stageLines);

        // 恢复流程不能挂在 Hotspot 自己身上：
        // hideWhileShowing 里可能会隐藏 Hotspot 的父物体，协程会跟着停掉。
        if (stageOverlay != null || HasSwapTarget())
        {
            CoroutineRunner.Instance.StartCoroutine(
                VisualRoutine(stageLines, stageOverlay, stageSwap)
            );
        }

        if (onInteract != null) onInteract.Invoke();

        firstStageDone = true;
        if (!hasSecond || useSecond) used = true;
        if (useSecond) secondStageDone = true;
    }

    bool HasSwapTarget()
    {
        return worldBackground != null || uiBackground != null;
    }

    bool HasVisualChange()
    {
        return overlay != null || HasSwapTarget();
    }

    Sprite CurrentBackgroundSprite()
    {
        if (worldBackground != null) return worldBackground.sprite;
        if (uiBackground != null) return uiBackground.sprite;
        return null;
    }

    void SetBackgroundSprite(Sprite sprite)
    {
        if (worldBackground != null) worldBackground.sprite = sprite;
        if (uiBackground != null) uiBackground.sprite = sprite;
    }

    /// <summary>
    /// 盖好/换好画面 → 等台词播完（没台词就等 holdSeconds）→ 收回来。
    /// 覆盖层和换背景可以同时用，也可以只用其中一个。
    /// </summary>
    IEnumerator VisualRoutine(
        DialogueLine[] stageLines,
        GameObject stageOverlay,
        Sprite stageSwap)
    {
        // ---------- 进入 ----------
        Sprite original = CurrentBackgroundSprite();
        Sprite loadedSwap = null;

        if (HasSwapTarget())
        {
            loadedSwap = stageSwap != null ? stageSwap : Resources.Load<Sprite>(swapResourceName);
            if (loadedSwap == null)
                Debug.LogError("[Hotspot] " + name + " 找不到要换的图：Resources/" + swapResourceName, this);
            else
                SetBackgroundSprite(loadedSwap);
        }

        CanvasGroup stageGroup = null;
        if (stageOverlay != null)
        {
            stageGroup = stageOverlay.GetComponent<CanvasGroup>();
            if (stageGroup == null) stageGroup = overlayGroup;

            stageOverlay.SetActive(true);
            if (stageGroup != null) yield return FadeOverlay(stageGroup, 1f, overlayFadeIn);
        }

        // 把其他东西藏起来：只留覆盖层和文案
        bool[] wasActive = null;
        if (hideWhileShowing != null && hideWhileShowing.Length > 0)
        {
            wasActive = new bool[hideWhileShowing.Length];

            for (int i = 0; i < hideWhileShowing.Length; i++)
            {
                GameObject go = hideWhileShowing[i];
                if (go == null) continue;
                if (stageOverlay != null && go == stageOverlay) continue;      // 别把覆盖层自己藏了

                wasActive[i] = go.activeSelf;
                go.SetActive(false);
            }
        }

        // ---------- 等 ----------
        if (stageLines != null && stageLines.Length > 0)
        {
            // 兜底：万一对话还没开始（Show 比这一帧更晚才调用），最多等 0.5 秒
            float waitStart = 0.5f;
            while (!DialogueSystem.IsPlaying && waitStart > 0f)
            {
                waitStart -= Time.unscaledDeltaTime;
                yield return null;
            }

            // End() 一调用 IsPlaying 就变 false（对话框还在淡出），所以再多等一点点让画面干净
            while (DialogueSystem.IsPlaying) yield return null;
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, holdSeconds));
        }

        // ---------- 收回 ----------
        if (wasActive != null)
        {
            for (int i = 0; i < wasActive.Length; i++)
            {
                GameObject go = hideWhileShowing[i];
                if (go != null) go.SetActive(wasActive[i]);
            }
        }

        if (stageOverlay != null)
        {
            if (stageGroup != null) yield return FadeOverlay(stageGroup, 0f, overlayFadeOut);
            stageOverlay.SetActive(false);
        }

        if (loadedSwap != null) SetBackgroundSprite(original);
    }

    IEnumerator FadeOverlay(CanvasGroup group, float target, float duration)
    {
        if (group == null) yield break;

        if (duration <= 0f)
        {
            group.alpha = target;
            yield break;
        }

        float start = group.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
            yield return null;
        }

        group.alpha = target;
    }

    void OnDrawGizmosSelected()
    {
        float width = 1f;
        float height = 1f;
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.sprite != null)
        {
            width = renderer.sprite.bounds.size.x;
            height = renderer.sprite.bounds.size.y;
        }

        float scale = hitAreaScale > 0f ? hitAreaScale : 2f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(width * scale, height * scale, 0f));
    }

    void OnDrawGizmos()
    {
        // 在 Scene 视图里画个小标记，方便把那两处摆准
        Gizmos.color = used ? new Color(0.6f, 0.6f, 0.6f, 1f) : new Color(1f, 0.8f, 0.2f, 1f);
        Gizmos.DrawWireCube(transform.position, new Vector3(0.25f, 0.25f, 0f));
    }
}
