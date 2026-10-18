using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UImanager
{
   private static UImanager instance=new UImanager();
    public static UImanager Instance => instance;

    //存储面板的容器
    private Dictionary<string,BasePanel> panelDic = new Dictionary<string,BasePanel>();
    //应该一开始就得到canvas对象
    private Transform canvasTrans;

    private UImanager() { }

    // 第一次真正用到时再去找 Canvas。
    // 放在构造函数里的话，场景还没加载好就会直接空引用
    private Transform CanvasTrans
    {
        get
        {
            if (canvasTrans == null)
            {
                GameObject canvas = GameObject.Find("Canvas");
                if (canvas == null)
                {
                    Debug.LogError("[UImanager] 场景里找不到名为 Canvas 的对象，面板没有地方可以挂");
                    return null;
                }

                canvasTrans = canvas.transform;
                //让canvas对象过场景不移除
                //我们都是通过动态创建和动态删除俩显示隐藏面板的，所以删除它影响不大
                GameObject.DontDestroyOnLoad(canvasTrans.gameObject);
            }

            return canvasTrans;
        }
    }

    //显示面板
    public T ShowPanel<T>() where T : BasePanel
    {
        //我们只需要保证泛型T的类型和面板名一致定一个这样的规则就非常方便使用
        string panelName=typeof(T).Name;

        //已经创建过了：如果它正在淡出，就把它重新叫回来，而不是又造一个新的
        BasePanel exist;
        if (panelDic.TryGetValue(panelName, out exist) && exist != null)
        {
            if (!exist.IsShowing) exist.ShowMe();
            return exist as T;
        }

        Transform parent = CanvasTrans;
        if (parent == null) return null;

        //显示面板就是创建面板预设体 设置父对象
        //根据得到的类名就是我们的预设头面板名，直接动态创建他即可
        GameObject prefab = Resources.Load<GameObject>("UI/" + panelName);
        if (prefab == null)
        {
            Debug.LogError("[UImanager] 找不到面板预设体：Resources/UI/" + panelName);
            return null;
        }

        GameObject panelObj = GameObject.Instantiate(prefab);
        panelObj.transform.SetParent(parent,false);

        //接着就是得到对应的面板脚本存储起来
        T panel=panelObj.GetComponent<T>();
        if (panel == null)
        {
            Debug.LogError("[UImanager] 预设体 " + panelName + " 上没有挂 " + panelName + " 这个脚本");
            GameObject.Destroy(panelObj);
            return null;
        }

        //把面板脚本存储到对应容器中，之后方便我们获取他
        panelDic.Add(panelName, panel);
        //调用显示自己的逻辑
        panel.ShowMe();
        return panel;
    }

    //隐藏面板
    //参数一：如果希望淡出就默认传true，直接删除传false
    public void HidePanel<T>(bool isFade=true) where T : BasePanel
    {
        //根据泛型得到面板名字
        string panelName = typeof(T).Name;

        BasePanel panel;
        if (!panelDic.TryGetValue(panelName, out panel) || panel == null) return;

        if (isFade)
        {
            // 注意这里捕获的是局部变量 panel。
            // 淡出结束时字典里可能已经没有它了，再去查字典会直接抛异常
            panel.HideMe(() => DestroyPanel(panelName, panel));
        }
        else
        {
            DestroyPanel(panelName, panel);
        }
    }

    //删除面板：先把它从字典里摘掉，再销毁物体。
    //如果淡出途中它又被 ShowPanel 重新打开了，这里什么都不做
    private void DestroyPanel(string panelName, BasePanel panel)
    {
        if (panel == null) return;

        BasePanel current;
        if (panelDic.TryGetValue(panelName, out current) && current == panel)
        {
            panelDic.Remove(panelName);
        }

        GameObject.Destroy(panel.gameObject);
    }

    //获得面板
    public T GetPanel<T>() where T : BasePanel
    {
        BasePanel panel;
        if (panelDic.TryGetValue(typeof(T).Name, out panel)) return panel as T;
        return null;
    }

    //面板是不是还在显示。注意：正在淡出的面板这里会返回 false
    public bool IsPanelActive<T>() where T : BasePanel
    {
        BasePanel panel;
        if (panelDic.TryGetValue(typeof(T).Name, out panel) && panel != null) return panel.IsShowing;
        return false;
    }

    public void ShowPanelLater<T>(float waitTime) where T : BasePanel
    {
        CoroutineRunner.Instance.StartCoroutine(wait<T>(waitTime));
        
    }

    public IEnumerator wait<T>(float waitTime) where T: BasePanel
    {
        yield return new WaitForSeconds(waitTime);
        ShowPanel<T>();
    }
    
}
