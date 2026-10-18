using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kongyou.Dialogue
{
    /// <summary>
    /// 演示场景的启动器：一进 Play 就铺好背景、自动开始播文案，右上角显示操作提示。
    /// 这个脚本只为"能直接跑起来感受效果"存在，正式接进流程以后删掉就行。
    /// </summary>
    public class DialogueSceneDemo : MonoBehaviour
    {
        [Header("剧本")]
        [Tooltip("Resources/Dialogues 下的文件名，不用写后缀")]
        public string scriptName = "demo";

        [Tooltip("一进 Play 就自动开始播")]
        public bool autoPlay = true;

        [Tooltip("自动播放：一句打完自动接下一句，可以什么都不按就看完整段")]
        public bool autoAdvance = true;

        [Header("画面")]
        [Tooltip("背景图，Resources 下的路径不用写后缀。留空或找不到就用纯色底")]
        public string backdropResource = "BackGrounds/black_room";

        [Tooltip("背景图找不到时用的纯色")]
        public Color fallbackBackdropColor = new Color(0.03f, 0.03f, 0.04f, 1f);

        [Tooltip("右上角显示操作提示")]
        public bool showHint = true;

        TMP_Text _hint;

        void Start()
        {
            BuildBackdrop();
            if (showHint) BuildHint();

            if (autoAdvance) DialogueSystem.AutoMode = true;
            if (autoPlay) DialogueSystem.Play(scriptName);
        }

        void Update()
        {
            // R = 从头再看一遍
            if (KeyInput.WasPressedThisFrame(KeyCode.R)) Replay();

            if (_hint == null) return;

            _hint.enabled = DialogueSystem.IsPlaying;
            /*if (DialogueSystem.IsPlaying)
            {
                _hint.text = "空格 / 点击 = 继续    按住 Ctrl = 快进    Y = 自动播放："
                           + (DialogueSystem.AutoMode ? "开" : "关")
                           + "    R = 重播    T = 收掉\n"
                           + "字体自检：中文测试 一二三 ABC（这行要是方块，就是字体缺中文字形）";
            }*/
        }

        /// <summary>从头再播一遍。</summary>
        public void Replay()
        {
            if (DialogueSystem.IsPlaying) DialogueSystem.Instance.End();
            DialogueSystem.Play(scriptName);
        }

        void BuildBackdrop()
        {
            var canvasGo = MakeCanvas("DemoBackdropCanvas", -100);
            canvasGo.transform.SetParent(transform, false);

            var go = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)go.transform);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;

            var sprite = string.IsNullOrEmpty(backdropResource) ? null : Resources.Load<Sprite>(backdropResource);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.color = fallbackBackdropColor;
            }
        }

        void BuildHint()
        {
            var canvasGo = MakeCanvas("DemoHintCanvas", 501);
            canvasGo.transform.SetParent(transform, false);

            var go = new GameObject("Hint", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(canvasGo.transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-28f, -24f);
            rect.sizeDelta = new Vector2(860f, 130f);

            _hint = go.AddComponent<TextMeshProUGUI>();
            _hint.font = CjkFontProvider.Resolve(null);
            _hint.fontSize = 24f;
            _hint.color = new Color(1f, 1f, 1f, 0.42f);
            _hint.alignment = TextAlignmentOptions.TopRight;
            _hint.raycastTarget = false;
            _hint.text = string.Empty;
        }

        static GameObject MakeCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return go;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
