using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

namespace Kongyou.Dialogue
{
    /// <summary>
    /// 打字机效果。挂在带 TMP_Text 的对象上（DialogueSystem 会自己挂）。
    ///
    /// 做法是一次性把整句交给 TMP，然后只推进 maxVisibleCharacters。
    /// 好处：不用每帧重建字符串（没有 GC 垃圾）、不触发文字重排、天然兼容 TMP 富文本和中文。
    ///
    /// 正文里可以写两个自定义标记（标记本身不会被显示出来）：
    ///     &lt;pause=0.6&gt;   在这里停 0.6 秒
    ///     &lt;speed=0.5&gt;   从这里开始变成 0.5 倍速，写 &lt;speed=1&gt; 恢复原速
    /// </summary>
    public class TypewriterText : MonoBehaviour
    {
        [Tooltip("要打字的 TMP 文本，留空就自动取同一个物体上的。")]
        public TMP_Text target;

        [Tooltip("每秒显示多少个字")]
        public float charsPerSecond = 20f;

        [Tooltip("句末标点（。！？…）后面的停顿")]
        public float sentencePause = 0.3f;

        [Tooltip("逗号类标点（，、；：）后面的停顿")]
        public float commaPause = 0.2f;

        [Tooltip("按住快进键时的速度倍率")]
        public float fastForwardMultiplier = 12f;

        [Tooltip("单帧最多推进多少秒。进场景卡顿、第一次搭对话框、动态字体生成图集都可能让某一帧膨胀到零点几秒，不封顶就会一口气把整句吐完")]
        public float maxStepDeltaTime = 0.1f;

        [Tooltip("用不受 timeScale 影响的时间，游戏暂停时也能正常打字")]
        public bool useUnscaledTime = true;

        /// <summary>正在逐字显示。</summary>
        public bool IsTyping { get; private set; }

        /// <summary>正在快进。</summary>
        public bool IsFastForwarding { get; private set; }

        /// <summary>每显示一个字回调一次。空格和标点也会回调，要不要出声交给调用方决定。</summary>
        public event Action<char> CharRevealed;

        /// <summary>整句打完时回调一次。</summary>
        public event Action Completed;

        /// <summary>可见字数（不包含富文本标签）。</summary>
        public int CharacterCount { get { return _total; } }

        int _total;
        int _visibleCount;
        int _revealed;
        float _acc;
        float _wait;
        float _leadingPause;
        float _lineSpeed = 1f;
        float[] _pauseAfter = new float[128];
        float[] _speedAt = new float[128];

        void Awake()
        {
            if (target == null) target = GetComponent<TMP_Text>();
        }

        void OnDisable()
        {
            IsTyping = false;
            IsFastForwarding = false;
        }

        /// <summary>开始打一段字。会自动把上一句停掉。</summary>
        public void Play(string text, float lineSpeed = 1f)
        {
            if (target == null) target = GetComponent<TMP_Text>();
            if (target == null)
            {
                Debug.LogError("[Typewriter] 身上没有 TMP_Text，打字机没法工作");
                return;
            }

            _lineSpeed = Mathf.Approximately(lineSpeed, 0f) ? 1f : lineSpeed;

            var clean = BuildTimeline(text);

            target.text = clean;
            target.maxVisibleCharacters = 0;
            target.ForceMeshUpdate();     // 必须先刷新一次，characterCount 才是准的

            _revealed = 0;
            _acc = 0f;
            _wait = _leadingPause;
            _leadingPause = 0f;
            _total = Mathf.Min(target.textInfo.characterCount, _visibleCount);
            IsFastForwarding = false;

            if (_total <= 0)
            {
                // 空行。不算在打字，交给调用方决定下一步
                IsTyping = false;
                return;
            }

            IsTyping = true;
        }

        void Update()
        {
            if (!IsTyping) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            // 进场景、第一次搭 UI、动态字体生成图集，这些都可能让某一帧花掉零点几秒。
            // 那一帧的 deltaTime 就是这么大的值，下面的 while 会把它当成"打字时间"一次消耗掉，
            // 于是整句在同一帧里全冒出来，看起来就像打字机没生效。这里给单帧设个上限。
            if (maxStepDeltaTime > 0f) dt = Mathf.Min(dt, maxStepDeltaTime);

            // 先消化标点和 <pause> 带来的停顿
            if (_wait > 0f)
            {
                _wait -= dt;
                if (_wait > 0f) return;
                dt = -_wait;      // 停顿结束后剩下的这点时间，继续用来推进
                _wait = 0f;
            }

            // 一个字可能只要几毫秒，一帧要吐好几个字，所以用 while 精确消耗时间
            while (dt > 0f && _revealed < _total)
            {
                float need = 1f / CurrentSpeed();

                if (_acc + dt < need)
                {
                    _acc += dt;
                    dt = 0f;
                }
                else
                {
                    dt -= (need - _acc);
                    _acc = 0f;
                    RevealOne();
                    if (_wait > 0f) return;    // 这个字后面要停顿，这一帧先停手
                }
            }

            if (_revealed >= _total) Complete();
        }

        float CurrentSpeed()
        {
            int index = Mathf.Clamp(_revealed, 0, Mathf.Max(0, _total - 1));
            float speed = charsPerSecond * _lineSpeed * _speedAt[index];
            if (IsFastForwarding) speed *= fastForwardMultiplier;
            return Mathf.Max(0.0001f, speed);
        }

        void RevealOne()
        {
            int index = _revealed;
            _revealed++;
            target.maxVisibleCharacters = _revealed;

            if (CharRevealed != null) CharRevealed(GetCharacter(index));

            if (index < _pauseAfter.Length && _pauseAfter[index] > 0f) _wait = _pauseAfter[index];
        }

        char GetCharacter(int index)
        {
            var info = target.textInfo;
            if (info == null || index < 0 || index >= info.characterCount) return '\0';
            return info.characterInfo[index].character;
        }

        void Complete()
        {
            if (!IsTyping) return;

            IsTyping = false;
            IsFastForwarding = false;
            _acc = 0f;
            _wait = 0f;
            target.maxVisibleCharacters = _total;

            if (Completed != null) Completed();
        }

        /// <summary>立刻把整句显示完（玩家点了跳过）。</summary>
        public void FinishNow()
        {
            if (!IsTyping) return;

            _acc = 0f;
            _wait = 0f;
            _revealed = _total;
            IsTyping = false;
            IsFastForwarding = false;
            target.maxVisibleCharacters = _total;

            if (Completed != null) Completed();
        }

        /// <summary>直接清空，不触发 Completed。</summary>
        public void Clear()
        {
            IsTyping = false;
            IsFastForwarding = false;
            _revealed = 0;
            _acc = 0f;
            _wait = 0f;
            _leadingPause = 0f;
            _total = 0;

            if (target != null)
            {
                target.text = string.Empty;
                target.maxVisibleCharacters = 0;
            }
        }

        public void SetFastForward(bool on)
        {
            if (IsTyping) IsFastForwarding = on;
        }

        // ---------------- 解析 ----------------

        /// <summary>
        /// 把正文里 &lt;pause&gt; &lt;speed&gt; 摘出来，换成一张"每个字之后停多久"的表，
        /// 返回真正交给 TMP 显示的文本。TMP 自己的富文本标签原样保留。
        /// </summary>
        string BuildTimeline(string raw)
        {
            raw = raw ?? string.Empty;
            EnsureCapacity(raw.Length + 1);

            for (int i = 0; i < _pauseAfter.Length; i++)
            {
                _pauseAfter[i] = 0f;
                _speedAt[i] = 1f;
            }

            _visibleCount = 0;
            _leadingPause = 0f;
            float speed = 1f;

            var sb = new StringBuilder(raw.Length);

            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];

                if (c == '<')
                {
                    int close = raw.IndexOf('>', i + 1);
                    if (close > i)
                    {
                        var tag = raw.Substring(i + 1, close - i - 1);
                        float pause;
                        float speedValue;

                        if (TryReadCommand(tag, "pause", out pause))
                        {
                            // 停在这个标记所在的位置，也就是上一个字之后
                            if (_visibleCount > 0)
                            {
                                _pauseAfter[_visibleCount - 1] = Mathf.Max(_pauseAfter[_visibleCount - 1], pause);
                            }
                            else
                            {
                                _leadingPause = Mathf.Max(_leadingPause, pause);
                            }

                            i = close;
                            continue;
                        }

                        if (TryReadCommand(tag, "speed", out speedValue))
                        {
                            speed = Mathf.Max(0.01f, speedValue);
                            i = close;
                            continue;
                        }

                        // 不是我们的标记，那应该是 TMP 的富文本：原样交给它
                        sb.Append(raw, i, close - i + 1);
                        i = close;
                        continue;
                    }
                }

                _speedAt[_visibleCount] = speed;
                AddPunctuationPause(c, _visibleCount);
                sb.Append(c);
                _visibleCount++;
            }

            return sb.ToString();
        }

        void AddPunctuationPause(char c, int index)
        {
            if (c == '。' || c == '！' || c == '？' || c == '…' || c == '.' || c == '!' || c == '?')
            {
                _pauseAfter[index] = Mathf.Max(_pauseAfter[index], sentencePause);
            }
            else if (c == '，' || c == '、' || c == '；' || c == '：' || c == ',' || c == ';' || c == ':')
            {
                _pauseAfter[index] = Mathf.Max(_pauseAfter[index], commaPause);
            }
        }

        static bool TryReadCommand(string tag, string key, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(tag)) return false;
            if (!tag.StartsWith(key, StringComparison.OrdinalIgnoreCase)) return false;

            int eq = tag.IndexOf('=');
            if (eq != key.Length) return false;

            return float.TryParse(
                tag.Substring(eq + 1).Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        void EnsureCapacity(int size)
        {
            if (_pauseAfter.Length >= size) return;

            int n = _pauseAfter.Length;
            while (n < size) n *= 2;

            _pauseAfter = new float[n];
            _speedAt = new float[n];
        }
    }
}
