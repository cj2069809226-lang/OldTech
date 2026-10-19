using UnityEngine;

public class wait : MonoBehaviour
{
    public StorySequenceDialogue story;
    public Shake shake;

    void Start()
    {
        if (story == null)
        {
            Debug.LogWarning("[wait] 没有指定 StorySequenceDialogue。", this);
            return;
        }

        if (shake == null)
        {
            Debug.LogWarning("[wait] 没有指定 Shake。", this);
            return;
        }

        story.Finished += OnStoryFinished;

        // 如果剧情在绑定前已经结束，补一次
        if (story.IsFinished)
            OnStoryFinished();
    }

    void OnDestroy()
    {
        if (story != null)
            story.Finished -= OnStoryFinished;
    }

    void OnStoryFinished()
    {
        if (shake != null)
            shake.Play();
    }
}
