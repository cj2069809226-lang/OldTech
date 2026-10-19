using System;
using System.Collections;
using Kongyou.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 遗像特写 + 发现祖母信件。
///
/// 流程：
///   1. 播放主角两句独白；
///   2. 独白结束后允许点击照片；
///   3. 点击照片显示泛黄信纸面板和信封文字；
///   4. 点击 Open 进入 ACT II。
/// </summary>
public class Photo : MonoBehaviour
{
    [Header("遗像按钮")]
    [Tooltip("玩家点击照片用的 Button；留空会自动取同一个物体上的 Button")]
    public Button photoButton;

    [Header("信件 UI")]
    [Tooltip("泛黄信纸面板，默认隐藏")]
    public GameObject letterPanel;

    [Tooltip("信封文字所在的 TMP 文本")]
    public TMP_Text letterText;

    [Tooltip("信纸上的 Open 按钮")]
    public Button openButton;

    [Header("ACT II")]
    [Tooltip("ACT II 场景名，必须加入 Build Settings")]
    public string act2SceneName = "ACT2";

    [Header("开场独白")]
    public bool playIntroOnStart = true;

    readonly DialogueLine[] introLines =
    {
        new DialogueLine("Me", "Mom once told me about her <b>older sister</b>.", 0.85f, 0.95f),
        new DialogueLine("Me", "<pause=0.3>Could this be her?", 0.8f, 1.02f),
    };

    bool introPlaying;
    bool letterVisible;
    bool envelopePlaying;

    void Awake()
    {
        if (photoButton == null)
            photoButton = GetComponent<Button>();

        if (photoButton != null)
            photoButton.onClick.AddListener(OnPhotoClicked);

        if (openButton != null)
            openButton.onClick.AddListener(OpenLetter);
    }

    void Start()
    {
        AudioManager.Instance.PlayMusic("水滴声");

        if (letterPanel != null)
            letterPanel.SetActive(false);

        if (openButton != null)
            openButton.gameObject.SetActive(false);

        if (photoButton != null)
            photoButton.interactable = false;

        if (letterPanel == null)
            Debug.LogWarning("[Photo] 没有指定 Letter Panel，点击照片后不会显示信纸。", this);

        if (playIntroOnStart)
            StartCoroutine(PlayIntro());
        else if (photoButton != null)
            photoButton.interactable = true;
    }

    IEnumerator PlayIntro()
    {
        introPlaying = true;

        if (photoButton != null)
            photoButton.interactable = false;

        DialogueSystem.Show(introLines);
        yield return WaitDialogueEnded();

        introPlaying = false;

        if (photoButton != null)
            photoButton.interactable = true;
    }

    IEnumerator WaitDialogueEnded()
    {
        DialogueSystem system = DialogueSystem.Instance;
        if (system == null) yield break;

        bool ended = false;
        Action onEnded = () => ended = true;
        system.Ended += onEnded;

        while (!ended) yield return null;

        if (system != null)
            system.Ended -= onEnded;
    }

    void OnPhotoClicked()
    {
        if (introPlaying || letterVisible) return;
        if (DialogueSystem.IsPlaying) return;

        ShowLetter();
    }

    void ShowLetter()
    {
        letterVisible = true;

        if (photoButton != null)
            photoButton.interactable = false;

        if (letterPanel != null)
            letterPanel.SetActive(true);

        // 信纸本身只作为背景，信封文字改由对话框显示，避免两处重复。
        if (letterText != null)
            letterText.text = string.Empty;

        if (openButton != null)
        {
            openButton.gameObject.SetActive(false);
            openButton.interactable = false;
        }

        StartCoroutine(PlayEnvelopeDialogue());
    }

    IEnumerator PlayEnvelopeDialogue()
    {
        envelopePlaying = true;

        DialogueSystem.Show(
            new DialogueLine("Envelope Contents", "To my dear <b>grandchild</b>.", 0.8f, 0.94f),
            new DialogueLine("Envelope Contents", "<pause=0.35>Read this only <b>after I'm gone</b>.", 0.75f, 0.9f)
        );

        yield return WaitDialogueEnded();

        envelopePlaying = false;

        if (openButton != null)
        {
            openButton.gameObject.SetActive(true);
            openButton.interactable = true;
        }
    }

    public void OpenLetter()
    {
        if (!letterVisible || envelopePlaying) return;

        if (string.IsNullOrEmpty(act2SceneName))
        {
            Debug.LogError("[Photo] ACT II 场景名为空。", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(act2SceneName))
        {
            Debug.LogError("[Photo] 场景没有加入 Build Settings：" + act2SceneName, this);
            return;
        }

        SceneManager.LoadScene(act2SceneName);
    }

    void OnDestroy()
    {
        if (photoButton != null)
            photoButton.onClick.RemoveListener(OnPhotoClicked);

        if (openButton != null)
            openButton.onClick.RemoveListener(OpenLetter);
    }
}
