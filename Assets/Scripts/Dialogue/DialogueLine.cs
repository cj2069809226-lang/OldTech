using System;
using UnityEngine;

namespace Kongyou.Dialogue
{
    /// <summary>
    /// 一句文案。
    /// 既能放进 ScriptableObject 剧本资产里在 Inspector 编辑，也能在代码里直接 new 出来临时播一句。
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        [Tooltip("说话人。留空就不显示名字框。")]
        public string speaker = "";

        [TextArea(2, 6)]
        [Tooltip("正文。支持 TMP 富文本，以及自定义标记 <pause=0.6> 和 <speed=0.5>。")]
        public string text = "";

        [Tooltip("这一行的语速倍率。1 = 默认，0.6 = 慢慢说，2 = 急促。")]
        public float speed = 1f;

        [Tooltip("这一行打字音的音高基准。0.9 = 低沉，1.15 = 尖细。")]
        public float pitch = 1f;

        [Tooltip("这一行专用的打字音，留空就用默认音效。")]
        public AudioClip blip;

        public DialogueLine() { }

        public DialogueLine(string speaker, string text, float speed = 1f, float pitch = 1f)
        {
            this.speaker = speaker;
            this.text = text;
            this.speed = speed;
            this.pitch = pitch;
        }

        public bool HasText
        {
            get { return !string.IsNullOrEmpty(text); }
        }
    }
}
