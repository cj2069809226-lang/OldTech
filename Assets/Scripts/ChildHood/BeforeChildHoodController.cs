using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BeforeChildHoodController : MonoBehaviour
{
    [Header("场景对象")]
    public GameObject blackScreen;
    public GameObject charactersRoot;
    public TMP_Text memoryText;

    [Header("流程")]
    public float titleHold = 2.2f;
    public string childHoodScene = "ChildHood";

    bool canEnter;
    bool started;
    bool loading;

    void Start()
    {
        if (started) return;
        started = true;
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        canEnter = false;

        if (blackScreen != null) blackScreen.SetActive(true);
        if (charactersRoot != null) charactersRoot.SetActive(true);
        if (memoryText != null) memoryText.text = "CHILDHOOD MEMORY";

        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, titleHold));

        if (blackScreen != null) blackScreen.SetActive(false);
        canEnter = true;
    }

    public void EnterChildHood()
    {
        if (loading || !canEnter) return;
        loading = true;

        if (!Application.CanStreamedLevelBeLoaded(childHoodScene))
        {
            Debug.LogError("[BeforeChildHood] 场景没有加入 Build Settings：" + childHoodScene, this);
            loading = false;
            return;
        }

        SceneManager.LoadScene(childHoodScene);
    }
}
