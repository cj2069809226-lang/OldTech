using UnityEngine;

public class Main : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.Instance.PlayMusic("增加恐怖氛围可用");
        UImanager.Instance.ShowPanel<StartPanel>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
