using UnityEngine;

/// <summary>
/// 挂在摄像机上（也可以挂在装着摄像机的空物体上）的水平跟随。
///
/// 规则：
///   1. 角色在摄像机中心 deadZone 之内时，摄像机一动不动（走路时画面不会一直跟着晃）；
///   2. 超过死区之后，摄像机才平滑地跟上去，只把角色保持在死区边缘，不会死死锁在角色身上；
///   3. 左右各有边界（minX / maxX），到边界就停住，避免镜头跑出场景。
///
/// 用法：把这个脚本拖到 Main Camera 上 → target 拖角色 → 在 Inspector 里调死区、平滑时间、左右边界。
/// 选中摄像机时，Scene 视图里会画出死区（黄线）和左右边界（红线），照着调就很快。
///
/// 注意：它读的是目标的 position（世界坐标）。如果你的角色是 Canvas 里的 UI，
/// 摄像机怎么动画面都不会变（Overlay 的 UI 不经过摄像机）——那种情况请用 BackgroundFollow 挪背景。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    [Tooltip("要跟的角色。留空脚本就什么都不做")]
    public Transform target;

    [Header("死区")]
    [Tooltip("角色离摄像机中心这么远之内，摄像机不动。数值按你场景的尺度填（角色高约 700 单位的话，死区填一二百）")]
    public float deadZone = 2f;

    [Header("平滑")]
    [Tooltip("追上目标大概要多久，越大越软")]
    public float smoothTime = 0.25f;

    [Tooltip("最大跟随速度，0 = 不限速")]
    public float maxSpeed = 0f;

    [Tooltip("开始游戏时直接把镜头对准角色，省掉开场那一次滑动")]
    public bool snapOnStart = true;

    [Header("左右限制（摄像机 x 的上下限）")]
    public bool useLimits = true;
    public float minX = -8f;
    public float maxX = 8f;

    [Header("Y 轴")]
    [Tooltip("横版一般不用跟 Y，保持镜头高度不变画面更稳")]
    public bool followY = false;

    [Tooltip("勾了 followY 才起作用：在角色 y 上再加这个偏移")]
    public float yOffset = 0f;

    float velocityX;
    float velocityY;
    float startY;

    void Start()
    {
        startY = transform.position.y;

        if (target == null)
        {
            Debug.LogWarning("[CameraFollow] 没有设置 target，脚本不会动");
            return;
        }

        if (snapOnStart) SetX(ClampX(target.position.x));
    }

    void LateUpdate()
    {
        if (target == null) return;

        float targetX = target.position.x;
        float currentX = transform.position.x;

        // 死区：角色没超出这个范围，目标位置就是当前位置（等于不动）
        float delta = targetX - currentX;
        float desiredX = currentX;
        if (Mathf.Abs(delta) > deadZone)
            desiredX = targetX - Mathf.Sign(delta) * deadZone;

        float speedLimit = maxSpeed > 0f ? maxSpeed : Mathf.Infinity;

        float x = Mathf.SmoothDamp(currentX, desiredX, ref velocityX, smoothTime, speedLimit, Time.deltaTime);
        x = ClampX(x);

        float y = startY;
        if (followY)
        {
            y = Mathf.SmoothDamp(transform.position.y, target.position.y + yOffset,
                                 ref velocityY, smoothTime, speedLimit, Time.deltaTime);
        }

        transform.position = new Vector3(x, y, transform.position.z);
    }

    float ClampX(float x)
    {
        if (!useLimits) return x;
        return Mathf.Clamp(x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));
    }

    void SetX(float x)
    {
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    void OnDrawGizmosSelected()
    {
        float y = transform.position.y;
        float half = 6f;

        // 死区
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(transform.position.x - deadZone, y - half, 0f),
                        new Vector3(transform.position.x - deadZone, y + half, 0f));
        Gizmos.DrawLine(new Vector3(transform.position.x + deadZone, y - half, 0f),
                        new Vector3(transform.position.x + deadZone, y + half, 0f));

        // 左右边界
        if (!useLimits) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(minX, y - half, 0f), new Vector3(minX, y + half, 0f));
        Gizmos.DrawLine(new Vector3(maxX, y - half, 0f), new Vector3(maxX, y + half, 0f));
    }
}
