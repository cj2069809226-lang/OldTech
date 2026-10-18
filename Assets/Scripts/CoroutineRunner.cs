using System.Collections;
using UnityEngine;

public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;

    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance == null) // Unity 的假 null 判断，对象被销毁也会进这里
            {
                var go = new GameObject("[CoroutineRunner]");
                if(go != null)
                {
                    Debug.Log("CoroutineRunner实例化成功");
                }
                _instance = go.AddComponent<CoroutineRunner>();
                DontDestroyOnLoad(go);// 切场景也不销毁
            }
            return _instance;
        }
    }

}
