using Kongyou.Dialogue;
using UnityEngine;

public class BeforeChildHoodCharacter : MonoBehaviour
{
    public BeforeChildHoodController controller;

    void OnMouseDown()
    {
        if (!DialogueSystem.IsPlaying)
            Click();
    }

    public void Click()
    {
        if (controller != null)
            controller.EnterChildHood();
    }
}
