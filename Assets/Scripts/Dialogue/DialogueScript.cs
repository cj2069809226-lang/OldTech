using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Kongyou.Dialogue
{
    /// <summary>
    /// 一份剧本。
    ///
    /// 两种用法：
    ///   1. 在 Assets 里右键 Create > 恐游 > 文案剧本，得到可以在 Inspector 编辑的资产；
    ///   2. 用纯文本剧本 Resources/Dialogues/xxx.txt，策划直接改文本，不用打开 Unity。
    ///
    /// 纯文本格式（一行一句，用竖线分隔）：
    ///     说话人|正文
    ///     说话人|正文|speed=1.4|pitch=0.9|blip=声音名
    ///     |没写说话人就是旁白
    ///     // 以 // 或 # 开头的是注释，空行会被忽略
    /// </summary>
    [CreateAssetMenu(menuName = "恐游/文案剧本", fileName = "NewDialogueScript")]
    public class DialogueScript : ScriptableObject
    {
        [Tooltip("剧本内容。用纯文本剧本时，这里是运行时解析出来的结果。")]
        public DialogueLine[] lines = new DialogueLine[0];

        /// <summary>纯文本剧本放在 Resources 下的哪个文件夹。</summary>
        public const string ResourceFolder = "Dialogues";

        /// <summary>打字音素材放在 Resources 下的哪个文件夹（行尾写 blip=名字 时会来这里找）。</summary>
        public const string BlipFolder = "DialogueBlips";

        /// <summary>读 Resources/Dialogues/&lt;名字&gt;.txt。名字不用带后缀。</summary>
        public static DialogueScript LoadFromResources(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var asset = Resources.Load<TextAsset>(ResourceFolder + "/" + name);
            if (asset == null)
            {
                Debug.LogError("[Dialogue] 找不到剧本：Resources/" + ResourceFolder + "/" + name + ".txt");
                return null;
            }

            // 用 asset.bytes 自己解码，不走 asset.text。
            // Unity 对"没有 BOM"的文本文件会按系统 ANSI 码页去解，UTF-8 的中文直接变乱码。
            return FromText(DecodeText(asset.bytes), name);
        }

        /// <summary>
        /// 自己解码剧本：先按 UTF-8 严格解，解不出来说明文件是 GBK/ANSI 存的，再按 936 解一遍。
        /// 这样剧本无论是"UTF-8 带 BOM"、"UTF-8 不带 BOM"还是"记事本另存的 ANSI"，中文都不会乱。
        /// </summary>
        public static string DecodeText(byte[] raw)
        {
            if (raw == null || raw.Length == 0) return string.Empty;

            try
            {
                var strict = new UTF8Encoding(false, true);
                return strict.GetString(raw).TrimStart('\uFEFF');
            }
            catch (Exception)
            {
                // 不是合法的 UTF-8，往下走
            }

            try
            {
                return Encoding.GetEncoding(936).GetString(raw).TrimStart('\uFEFF');
            }
            catch (Exception)
            {
                // 这个平台不支持 936，最后兜底
            }

            return Encoding.UTF8.GetString(raw).TrimStart('\uFEFF');
        }

        public static DialogueScript FromText(string raw, string name = "runtime")
        {
            var script = CreateInstance<DialogueScript>();
            script.name = name;
            script.lines = Parse(raw);
            return script;
        }

        /// <summary>把纯文本拆成一句一句的文案。</summary>
        public static DialogueLine[] Parse(string raw)
        {
            var result = new List<DialogueLine>();
            if (string.IsNullOrEmpty(raw)) return result.ToArray();

            // 有些编辑器存文件会带上 BOM，这里先摘掉，免得第一行被当成奇怪的台词
            if (raw[0] == '\uFEFF') raw = raw.Substring(1);

            var rows = raw.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int r = 0; r < rows.Length; r++)
            {
                var row = rows[r].Trim().Trim('\uFEFF').Trim();
                if (row.Length == 0) continue;
                if (row.StartsWith("//") || row[0] == '#') continue;

                var parts = row.Split('|');
                var line = new DialogueLine();

                line.text = parts[0].Trim();
                if (parts.Length >= 2)
                {
                    line.speaker = parts[0].Trim();
                    line.text = parts[1].Trim();
                    for (int i = 2; i < parts.Length; i++) ApplyOption(line, parts[i]);
                }

                if (line.HasText) result.Add(line);
            }

            return result.ToArray();
        }

        static void ApplyOption(DialogueLine line, string pair)
        {
            if (string.IsNullOrEmpty(pair)) return;

            int eq = pair.IndexOf('=');
            if (eq <= 0) return;

            var key = pair.Substring(0, eq).Trim().ToLowerInvariant();
            var value = pair.Substring(eq + 1).Trim();

            switch (key)
            {
                case "speed":
                    line.speed = ParseFloat(value, line.speed);
                    break;

                case "pitch":
                    line.pitch = ParseFloat(value, line.pitch);
                    break;

                case "blip":
                    line.blip = Resources.Load<AudioClip>(BlipFolder + "/" + value);
                    if (line.blip == null)
                    {
                        Debug.LogWarning("[Dialogue] 找不到打字音：Resources/" + BlipFolder + "/" + value);
                    }
                    break;
            }
        }

        static float ParseFloat(string value, float fallback)
        {
            float parsed;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : fallback;
        }
    }
}
