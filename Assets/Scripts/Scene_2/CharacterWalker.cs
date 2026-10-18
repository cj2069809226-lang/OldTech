using Kongyou.Dialogue;
using UnityEngine;

/// <summary>
/// 左右走路：A/D 或 ←/→ 控制，键盘松开就停。
/// 动画交给子物体 Art 上的 Animator：移动时 Speed>0.1 播走路，站着播 Idle。
///
/// 两种摆法都支持：
///   · 挂在 Canvas 下面（UI）：改 anchoredPosition，moveSpeed 和左右边界按"画布像素"填；
///   · 不挂在 Canvas 下（世界空间 SpriteRenderer）：改 transform.position，那三个数按"世界单位"填。
/// 脚本会自己判断，不用你切换。
///
/// 层级约定（② 号菜单会自动搭好）：
///     Character   ← 本脚本 + 移动/翻面，缩放放这里
///     └ Art       ← Image + Animator + 动画，缩放保持 1
/// Art 的位置偏移是为了让她的脚底正好落在 Character 的原点上，
/// 所以大小只改 Character 的 Scale，别动 Art 的。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CharacterWalker : MonoBehaviour
{
    [Header("移动（单位是 Canvas 里的像素）")]
    [Tooltip("每秒走多少。UI 角色按画布像素填（角色高约 760 → 780 左右不打滑）；" +
             "世界空间角色按世界单位填（角色高约 1.7 单位 → 1.8 左右）")]
    public float moveSpeed = 420f;

    [Tooltip("最左能走到哪（anchoredPosition.x）")]
    public float leftLimit = -780f;

    [Tooltip("最右能走到哪（anchoredPosition.x）")]
    public float rightLimit = 780f;

    [Header("动画")]
    [Tooltip("留空会自动去子物体里找")]
    public Animator animator;

    [Tooltip("素材本身是朝左画的：往右走时自动水平翻转")]
    public bool artFacesLeft = true;

    [Tooltip("旁白正在播的时候不许走动，免得剧情里乱跑")]
    public bool blockWhileDialogue = true;

    RectTransform rect;
    bool uiMode;               // 挂在 Canvas 下面 = UI，改 anchoredPosition；否则改 transform.position
    float baseScaleX = 1f;
    float facing = -1f;        // 1 = 朝右，-1 = 朝左
    bool hasSpeedParameter;    // 动画控制器里有没有 Speed 参数（② 号菜单生成的都有）

    void Awake()
    {
        rect = (RectTransform)transform;
        uiMode = GetComponentInParent<Canvas>() != null;
        if (animator == null) animator = GetComponentInChildren<Animator>();

        baseScaleX = Mathf.Abs(rect.localScale.x);
        if (baseScaleX <= 0f) baseScaleX = 1f;

        facing = artFacesLeft ? -1f : 1f;
        SetScaleFor(facing);
        hasSpeedParameter = HasSpeedParameter(animator);
    }

    void Update()
    {
        float input = 0f;
        if (KeyInput.IsHeld(KeyCode.A) || KeyInput.IsHeld(KeyCode.LeftArrow)) input -= 1f;
        if (KeyInput.IsHeld(KeyCode.D) || KeyInput.IsHeld(KeyCode.RightArrow)) input += 1f;

        if (blockWhileDialogue && DialogueSystem.IsPlaying) input = 0f;

        if (!Mathf.Approximately(input, 0f))
        {
            float lo = Mathf.Min(leftLimit, rightLimit);
            float hi = Mathf.Max(leftLimit, rightLimit);

            if (uiMode)
            {
                float x = Mathf.Clamp(rect.anchoredPosition.x + input * moveSpeed * Time.deltaTime, lo, hi);
                rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            }
            else
            {
                Vector3 p = transform.position;
                float x = Mathf.Clamp(p.x + input * moveSpeed * Time.deltaTime, lo, hi);
                transform.position = new Vector3(x, p.y, p.z);
            }

            facing = input > 0f ? 1f : -1f;
            SetScaleFor(facing);
        }

        if (animator != null)
        {
            if (hasSpeedParameter)
            {
                animator.SetFloat("Speed", Mathf.Abs(input) * moveSpeed);
            }
            else
            {
                // 没有 Speed 参数（比如你自己新建的控制器只有一个循环动画）：
                // 那就站着的时候把动画暂停住，别原地踏步
                animator.speed = Mathf.Approximately(input, 0f) ? 0f : 1f;
            }
        }
    }

    static bool HasSpeedParameter(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Float
                && parameters[i].name == "Speed") return true;
        }

        return false;
    }

    /// <summary>把子物体 Art 的贴图水平翻转（素材朝左，往右走要镜像一下）。</summary>
    void SetScaleFor(float direction)
    {
        bool flip = artFacesLeft ? direction > 0f : direction < 0f;
        Vector3 scale = rect.localScale;
        rect.localScale = new Vector3(flip ? -baseScaleX : baseScaleX, scale.y, scale.z);
    }
}
