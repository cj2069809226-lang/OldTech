using System;
using System.Collections;
using Kongyou.Dialogue;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ContinueToStartPanel : BasePanel
{
    public Button btnContinue;
    public override void Init()
    {
        AudioManager.Instance.PlayMusic("风声");

        btnContinue.interactable = false;
        btnContinue.image.color = new Color(btnContinue.image.color.r, btnContinue.image.color.g, btnContinue.image.color.b, 0f);
        CoroutineRunner.Instance.StartCoroutine(WaitForDialogue());
        btnContinue.onClick.AddListener(() =>
        {
            UImanager.Instance.HidePanel<ContinueToStartPanel>();
            //UImanager.Instance.ShowPanel<StartPanel>();
            SceneManager.LoadScene("scene_2");
        });
        
    }
    private IEnumerator WaitForDialogue()
    {
       DialogueSystem.Show(
            new DialogueLine("Me", "Seven days after my grandmother <b>passed away</b>,<pause=0.5> I returned to my hometown.", 0.8f, 0.94f)
        );
        yield return WaitDialogueEnded();

        btnContinue.interactable = true;
        btnContinue.image.color = new Color(btnContinue.image.color.r, btnContinue.image.color.g, btnContinue.image.color.b, 1f); 
    }
    



    private static IEnumerator WaitDialogueEnded()
    {
        DialogueSystem system = DialogueSystem.Instance;
        if (system == null) yield break;

        bool ended = false;
        Action onEnded = () => ended = true;

        // Show 是同步的，执行到这里说明文案已经在播了，不会漏掉 Ended
        system.Ended += onEnded;
        while (!ended) yield return null;

        if (system != null) system.Ended -= onEnded;
    }
}
