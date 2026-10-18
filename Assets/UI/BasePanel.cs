using UnityEngine;
using UnityEngine.Events;

public abstract class BasePanel : MonoBehaviour
{
    [Tooltip("淡入速度（每秒增加多少 alpha）")]
    public float ShowSpeed = 10f;

    [Tooltip("淡出速度（每秒减少多少 alpha）")]
    public float HideSpeed = 10f;

    private CanvasGroup canvasGroup;
    private bool isShow;

    // 被 ShowMe 打开过。没打开过的面板不应该自己往下淡
    private bool shownOnce;

    //当淡出成功时调用委托函数（只会执行一次）
    private UnityAction hideCallBack;

    /// <summary>正在显示，或者正在淡入。</summary>
    public bool IsShowing { get { return isShow; } }

    /// <summary>已经打开过、现在正在淡出。这段时间它还在 UImanager 的字典里，等着被删掉。</summary>
    public bool IsHiding { get { return shownOnce && !isShow; } }

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        Init();
    }
    /// <summary>
    /// 主要用于初始化 按钮事件监听等等内容
    /// </summary>
    public abstract void Init();

    public virtual void ShowMe()
    {
        shownOnce = true;
        isShow = true;

        // 重新打开一个正在淡出的面板时，必须把待执行的删除回调丢掉，
        // 否则它一到时间就会把刚打开的面板删掉
        hideCallBack = null;

        canvasGroup.alpha = 0;
        SetInteractable(true);
    }

    public virtual void HideMe(UnityAction callback)
    {
        isShow = false;
        //记录传入的当淡出成功后会执行的委托函数
        hideCallBack = callback;

        // 淡出期间先断掉交互，避免玩家点到正在消失的面板
        SetInteractable(false);
    }
    // Update is called once per frame
    void Update()
    {
        // 还没被 ShowMe 打开过，什么都别做
        if (!shownOnce) return;

        //淡入
        if (isShow)
        {
            if (canvasGroup.alpha >= 1) return;

            canvasGroup.alpha = Mathf.Min(1, canvasGroup.alpha + ShowSpeed * Time.deltaTime);
            return;
        }

        //淡出
        canvasGroup.alpha = Mathf.Max(0, canvasGroup.alpha - HideSpeed * Time.deltaTime);
        if (canvasGroup.alpha > 0) return;

        // 淡出结束。回调只执行一次，执行完立刻摘掉，
        // 否则 alpha 已经是 0 了，之后每一帧都会再调一次
        UnityAction callback = hideCallBack;
        hideCallBack = null;
        if (callback != null) callback();
    }

    void SetInteractable(bool on)
    {
        if (canvasGroup == null) return;

        canvasGroup.interactable = on;
        canvasGroup.blocksRaycasts = on;
    }
}
