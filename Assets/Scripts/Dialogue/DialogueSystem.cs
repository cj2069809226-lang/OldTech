using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kongyou.Dialogue
{
    /// <summary>
    /// 文字系统总控。
    ///
    /// 它不依赖任何场景物体，也不需要 prefab —— 运行游戏时自动创建一个常驻的 [DialogueSystem]，
    /// 第一次播文案时现场搭出对话框 UI。所以不管切到哪个场景都能直接用。
    ///
    /// 用法（贴在任意按钮回调 / 流程里）：
    ///     DialogueSystem.Play("demo");                                    // 播 Resources/Dialogues/demo.txt
    ///     DialogueSystem.Play(DialogueScript.LoadFromResources("demo"));  // 同上，先拿到剧本自己留着也行
    ///     DialogueSystem.Show(new DialogueLine("旁白", "潮气顺着门缝爬进来。"));   // 即兴来一句
    ///     DialogueSystem.Instance.End();                                  // 强行收掉对话框
    ///
    /// 默认操作：空格 / 回车 / 点屏幕 = 继续（打字中按一下先把整句显示完）；按住 Ctrl = 快进；
    ///          把 PlayerCanAdvance 设成 false 就进入"过场模式"：玩家什么都按不动，台词自己往下走。
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueSystem : MonoBehaviour
    {
        // ---------------- 单例 ----------------

        static DialogueSystem _instance;

        public static DialogueSystem Instance { get { return _instance; } }

        /// <summary>文案正在播（或正在等玩家点）。</summary>
        public static bool IsPlaying
        {
            get { return _instance != null && _instance._state != State.Idle; }
        }

        /// <summary>自动播放模式：一句打完停一会儿自动下一句。</summary>
        public static bool AutoMode
        {
            get { return _instance != null && _instance._autoMode; }
            set { GetOrCreate()._autoMode = value; }
        }

        /// <summary>
        /// 玩家能不能推进 / 跳过文案。设成 false = 过场模式：
        ///   · 点击屏幕、空格、回车都不再推进（对话框右下角的"继续"箭头也会隐藏）；
        ///   · 打字阶段"点一下立刻出整句"和按住 Ctrl 快进，一并失效；
        ///   · 台词改成按 autoDelay 自动往下走，所以剧情不会被卡住。
        /// 现在全局默认 false（不许跳过）。想让玩家能点着看就写一句：
        ///     DialogueSystem.PlayerCanAdvance = true;
        /// 也可以只在某段演出里临时放开。
        /// </summary>
        public static bool PlayerCanAdvance = false;

        static DialogueSystem GetOrCreate()
        {
            if (_instance == null) CreateInstance();
            return _instance;
        }

        /// <summary>进游戏时自动把系统挂上，不用手动摆到场景里。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            GetOrCreate();
        }

        static DialogueSystem CreateInstance()
        {
            var go = new GameObject("[DialogueSystem]");
            DontDestroyOnLoad(go);

            _instance = go.AddComponent<DialogueSystem>();
            // 这里原来会挂一个 DialogueDemo（T 键播示例剧本、Y 键切自动播放）。
            // 那些是调试用的热键，会在任何场景里生效，正式游戏里不该有，所以去掉了。
            // 想调试就自己在场景里挂一个 DialogueDemo。

            return _instance;
        }

        // ---------------- 对外入口 ----------------

        public static void Play(string resourceName)
        {
            var script = DialogueScript.LoadFromResources(resourceName);
            if (script != null) Play(script);
        }

        public static void Play(DialogueScript script)
        {
            if (script == null) return;
            GetOrCreate().PlayLines(script.lines);
        }

        public static void Play(DialogueLine[] lines)
        {
            GetOrCreate().PlayLines(lines);
        }

        public static void Show(params DialogueLine[] lines)
        {
            GetOrCreate().PlayLines(lines);
        }

        // ---------------- Inspector ----------------

        [Header("文案")]
        [Tooltip("每秒显示多少个字")]
        public float charsPerSecond = 28f;

        [Tooltip("自动播放时，一句打完停多久再下一句（过场模式也用它，中文建议 2~3 秒）")]
        public float autoDelay = 2.2f;

        [Tooltip("开局打一条自检日志（第一句文字 + 字体能不能显示中文），排查乱码时用")]
        public bool logSelfCheck = true;

        [Header("声音")]
        [Range(0f, 1f)]
        public float blipVolume = 0.35f;

        [Tooltip("打字音素材。留空就用程序化生成的合成音（不用先找素材也能听见声音）")]
        public AudioClip[] blipClips;

        [Tooltip("两个字之间最短的发声间隔。防止一帧显示多个字时音效叠成噪音")]
        public float blipMinInterval = 0.028f;

        [Header("外观")]
        [Tooltip("中文字体。留空就在运行时自动找系统里的中文字体")]
        public TMP_FontAsset fontAsset;

        public Color boxColor = new Color(0f, 0f, 0f, 0.78f);
        public Color speakerColor = new Color(1f, 0.96f, 0.86f, 1f);
        public Color bodyColor = new Color(0.94f, 0.94f, 0.94f, 1f);

        public float bodyFontSize = 36f;
        public float speakerFontSize = 30f;
        public float boxHeight = 340f;
        public float boxSideMargin = 120f;
        public float boxBottomMargin = 60f;

        // ---------------- 运行时状态 ----------------

        enum State { Idle, Typing, WaitingForAdvance }

        readonly List<DialogueLine> _lines = new List<DialogueLine>();

        State _state = State.Idle;
        int _index;
        DialogueLine _current;
        bool _autoMode;
        float _autoTimer;
        float _lastBlip;
        int _fadeToken;

        GameObject _root;
        CanvasGroup _rootGroup;
        GameObject _nameBox;
        TMP_Text _nameText;
        TMP_Text _bodyText;
        TypewriterText _typer;
        GameObject _indicator;
        RectTransform _indicatorRect;
        float _indicatorBaseY;
        AudioSource _audio;
        AudioClip[] _synthBlips;

        /// <summary>整段文案播完时触发。</summary>
        public event Action Ended;

        /// <summary>每一句开始播时触发，参数是这句在本次文案里的下标（从 0 开始）。
        /// 想让画面跟着台词走（换立绘、换手机屏、换背景）就挂这个。</summary>
        public event Action<int> LineStarted;

        void Awake()
        {
            _instance = this;

            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.loop = false;
            _audio.spatialBlend = 0f;
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ---------------- 播放流程 ----------------

        public void PlayLines(IList<DialogueLine> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                Debug.LogWarning("[Dialogue] 剧本是空的，没东西可播");
                return;
            }

            EnsureUI();

            _lines.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line != null && line.HasText) _lines.Add(line);
            }

            if (_lines.Count == 0)
            {
                Debug.LogWarning("[Dialogue] 剧本里没有有效台词");
                return;
            }

            _index = 0;
            _autoTimer = 0f;

            ShowRoot();
            StartLine(_index);
        }

        void StartLine(int index)
        {
            _current = _lines[index];

            if (index == 0 && logSelfCheck)
            {
                Debug.Log("[Dialogue] 文案自检：第一句 = \"" + _current.text + "\"　前几个字的码点 = "
                          + FirstCodePoints(_current.text, 4));
            }

            SetSpeaker(_current.speaker);
            SetIndicator(false);

            if (LineStarted != null) LineStarted(index);

            _typer.charsPerSecond = charsPerSecond;
            _state = State.Typing;
            _typer.Play(_current.text, _current.speed);

            // 空行之类的情况：没进打字状态，直接算这句打完了
            if (!_typer.IsTyping) OnLineTypedOut();
        }

        void OnLineTypedOut()
        {
            _state = State.WaitingForAdvance;
            SetIndicator(PlayerCanAdvance);    // 过场模式下不显示"点我继续"的箭头
            _autoTimer = autoDelay;
        }

        /// <summary>继续：正在打字就先把整句显示完，已经打完就下一句。</summary>
        public void Advance()
        {
            if (!PlayerCanAdvance) return;     // 过场模式：玩家推不动（要强行收掉请直接调 End()）
            if (_state == State.Idle) return;

            if (_typer != null && _typer.IsTyping)
            {
                _typer.FinishNow();    // 会触发 Completed，走 OnLineTypedOut
                return;
            }

            NextLine();
        }

        void NextLine()
        {
            _index++;
            if (_index >= _lines.Count)
            {
                End();
                return;
            }

            StartLine(_index);
        }

        /// <summary>直接收掉对话框。</summary>
        public void End()
        {
            if (_state == State.Idle) return;

            _state = State.Idle;
            _lines.Clear();
            _current = null;

            if (_typer != null) _typer.Clear();

            StartCoroutine(FadeRoot(0f, 0.18f, true, () =>
            {
                if (Ended != null) Ended();
            }));
        }

        // ---------------- 每帧 ----------------

        void Update()
        {
            if (_state == State.Idle) return;

            if (_state == State.Typing)
            {
                if (_typer != null)
                    _typer.SetFastForward(PlayerCanAdvance
                        && (KeyInput.IsHeld(KeyCode.LeftControl) || KeyInput.IsHeld(KeyCode.RightControl)));
            }
            else if (_state == State.WaitingForAdvance && (_autoMode || !PlayerCanAdvance))
            {
                _autoTimer -= Time.unscaledDeltaTime;
                if (_autoTimer <= 0f)
                {
                    NextLine();
                    return;
                }
            }

            BobIndicator();

            if (!PlayerCanAdvance) return;     // 过场模式：键盘一律不理会

            if (KeyInput.WasPressedThisFrame(KeyCode.Space)
                || KeyInput.WasPressedThisFrame(KeyCode.Return)
                || KeyInput.WasPressedThisFrame(KeyCode.KeypadEnter))
            {
                Advance();
            }
        }

        void BobIndicator()
        {
            if (_indicator == null || _indicatorRect == null || !_indicator.activeSelf) return;

            var pos = _indicatorRect.anchoredPosition;
            pos.y = _indicatorBaseY - Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.4f)) * 8f;
            _indicatorRect.anchoredPosition = pos;
        }

        // ---------------- 打字音 ----------------

        void OnCharRevealed(char c)
        {
            if (_typer != null && _typer.IsFastForwarding) return;   // 快进时不出声
            if (char.IsWhiteSpace(c)) return;                        // 空格不出声
            if (char.IsPunctuation(c) || char.IsSymbol(c)) return;    // 标点交给停顿去表现
            if (_audio == null) return;

            float now = Time.unscaledTime;
            if (now - _lastBlip < blipMinInterval) return;            // 限流，避免叠成噪音
            _lastBlip = now;

            var clip = PickBlip();
            if (clip == null) return;

            float pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            if (_current != null) pitch *= Mathf.Clamp(_current.pitch, 0.3f, 3f);

            _audio.pitch = pitch;
            _audio.PlayOneShot(clip, blipVolume);
        }

        AudioClip PickBlip()
        {
            if (_current != null && _current.blip != null) return _current.blip;

            if (blipClips != null && blipClips.Length > 0)
            {
                return blipClips[UnityEngine.Random.Range(0, blipClips.Length)];
            }

            if (_synthBlips == null) _synthBlips = BlipSynth.Bank(4);
            if (_synthBlips.Length > 0)
            {
                return _synthBlips[UnityEngine.Random.Range(0, _synthBlips.Length)];
            }

            return null;
        }

        // ---------------- UI ----------------

        void SetSpeaker(string speaker)
        {
            bool show = !string.IsNullOrEmpty(speaker) && speaker.Trim().Length > 0;

            if (_nameBox != null) _nameBox.SetActive(show);
            if (show && _nameText != null) _nameText.text = speaker.Trim();
        }

        void SetIndicator(bool on)
        {
            if (_indicator != null) _indicator.SetActive(on);
        }

        void ShowRoot()
        {
            if (_root == null) return;

            if (_root.activeSelf && _rootGroup.alpha >= 1f)
            {
                _rootGroup.interactable = true;
                _rootGroup.blocksRaycasts = true;
                return;
            }

            StartCoroutine(FadeRoot(1f, 0.15f, false, null));
        }

        IEnumerator FadeRoot(float target, float duration, bool deactivateWhenDone, Action onDone)
        {
            int token = ++_fadeToken;

            if (target > 0f && _root != null && !_root.activeSelf) _root.SetActive(true);

            _rootGroup.interactable = target > 0f;
            _rootGroup.blocksRaycasts = target > 0f;

            float start = _rootGroup.alpha;
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                if (token != _fadeToken) yield break;      // 有新的淡入淡出接管了

                _rootGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                yield return null;
            }

            if (token != _fadeToken) yield break;

            _rootGroup.alpha = target;
            if (deactivateWhenDone && _root != null) _root.SetActive(false);

            if (onDone != null) onDone();
        }

        void EnsureUI()
        {
            if (_root != null) return;

            var font = CjkFontProvider.Resolve(fontAsset);

            // 独立 Canvas，叠在现有 UI 之上，不动你原来的 Canvas
            var canvasGo = new GameObject("DialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 整块对话区域，统一淡入淡出
            _root = new GameObject("DialogueRoot", typeof(RectTransform), typeof(CanvasGroup));
            _root.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)_root.transform);

            _rootGroup = _root.GetComponent<CanvasGroup>();
            _rootGroup.alpha = 0f;
            _rootGroup.interactable = false;
            _rootGroup.blocksRaycasts = false;

            // 全屏热区：点屏幕任何地方都能继续
            var hotzone = CreateImage("AdvanceArea", _root.transform, new Color(0f, 0f, 0f, 0f));
            Stretch(hotzone.rectTransform);
            hotzone.raycastTarget = true;

            var button = hotzone.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(Advance);

            // 对话框
            var box = CreateImage("Box", _root.transform, boxColor);
            var boxRect = box.rectTransform;
            boxRect.anchorMin = new Vector2(0f, 0f);
            boxRect.anchorMax = new Vector2(1f, 0f);
            boxRect.pivot = new Vector2(0.5f, 0f);
            boxRect.offsetMin = new Vector2(boxSideMargin, boxBottomMargin);
            boxRect.offsetMax = new Vector2(-boxSideMargin, boxBottomMargin + boxHeight);

            // 正文
            _bodyText = CreateText("Body", box.transform, font, bodyColor, bodyFontSize, TextAlignmentOptions.TopLeft);
            var bodyRect = _bodyText.rectTransform;
            Stretch(bodyRect);
            bodyRect.offsetMin = new Vector2(48f, 40f);
            bodyRect.offsetMax = new Vector2(-48f, -36f);
            _bodyText.lineSpacing = 14f;

            _typer = _bodyText.gameObject.AddComponent<TypewriterText>();
            _typer.target = _bodyText;
            _typer.useUnscaledTime = true;
            _typer.CharRevealed += OnCharRevealed;
            _typer.Completed += OnLineTypedOut;

            // 名字框：贴在对话框左上角外侧
            var nameBox = CreateImage("NameBox", box.transform, Opaque(boxColor));
            _nameBox = nameBox.gameObject;
            var nameRect = nameBox.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(0f, 1f);
            nameRect.pivot = new Vector2(0f, 0f);
            nameRect.anchoredPosition = new Vector2(24f, 6f);
            nameRect.sizeDelta = new Vector2(280f, 58f);

            _nameText = CreateText("NameText", _nameBox.transform, font, speakerColor, speakerFontSize, TextAlignmentOptions.MidlineLeft);
            var nameTextRect = _nameText.rectTransform;
            Stretch(nameTextRect);
            nameTextRect.offsetMin = new Vector2(20f, 0f);
            nameTextRect.offsetMax = new Vector2(-20f, 0f);

            _nameBox.SetActive(false);

            // 继续指示箭头
            var indicatorText = CreateText("Indicator", box.transform, font, speakerColor, 26f, TextAlignmentOptions.BottomRight);
            indicatorText.text = "\u25BC";
            _indicator = indicatorText.gameObject;
            _indicatorRect = indicatorText.rectTransform;
            _indicatorRect.anchorMin = new Vector2(1f, 0f);
            _indicatorRect.anchorMax = new Vector2(1f, 0f);
            _indicatorRect.pivot = new Vector2(1f, 0f);
            _indicatorRect.sizeDelta = new Vector2(56f, 56f);
            _indicatorBaseY = 20f;
            _indicatorRect.anchoredPosition = new Vector2(-28f, _indicatorBaseY);

            _indicator.SetActive(false);
            _root.SetActive(false);

            if (logSelfCheck) Debug.Log("[Dialogue] 字体自检：" + CjkFontProvider.Describe(font));
        }

        static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, Color color, float size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.color = color;
            text.fontSize = size;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;

            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Color Opaque(Color c)
        {
            return new Color(c.r, c.g, c.b, Mathf.Clamp01(c.a + 0.15f));
        }

        static string FirstCodePoints(string s, int count)
        {
            if (string.IsNullOrEmpty(s)) return "（空）";

            var sb = new StringBuilder();
            int n = Mathf.Min(count, s.Length);
            for (int i = 0; i < n; i++)
            {
                sb.Append("U+").Append(((int)s[i]).ToString("X4")).Append(' ');
            }

            return sb.ToString().TrimEnd();
        }
    }
}
