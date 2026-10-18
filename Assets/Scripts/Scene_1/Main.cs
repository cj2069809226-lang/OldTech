using UnityEngine;

public class Main : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UImanager.Instance.ShowPanel<StartPanel>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
