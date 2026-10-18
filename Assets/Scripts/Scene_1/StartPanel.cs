using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class StartPanel : BasePanel
{
    public Button startButton;
    public override void Init()
    {
        startButton.onClick.AddListener(()=>
        {
            //UImanager.Instance.PopPanel();
            UImanager.Instance.HidePanel<StartPanel>();
            SceneManager.LoadScene("scene_1");
        });
    }
}
