using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Kongyou.Dialogue
{
    /// <summary>
    /// 找一套能显示中文的 TMP 字体。
    ///
    /// 优先用你指定的字体资产；没指定就现场从系统字体里造一个"动态字体资产"，
    /// 这样不用提前做字体图集也能显示中文（动态模式下缺哪个字补哪个字）。
    /// 如果你已经自己做了一套中文字体资产，把它拖到 DialogueSystem 的 fontAsset 上即可。
    /// </summary>
    public static class CjkFontProvider
    {
        static readonly string[] Candidates =
        {
            "Microsoft YaHei UI", "Microsoft YaHei", "微软雅黑",
            "SimHei", "黑体", "SimSun", "宋体",
            "Noto Sans CJK SC", "Source Han Sans SC",
            "PingFang SC", "Hiragino Sans GB", "Heiti SC"
        };

        const string SampleText = "测试中文";

        /// <summary>字体资产放在 Resources 下的约定位置：丢一个能显示中文的进去就会被自动用上。</summary>
        public const string FontResourceFolder = "Fonts";

        static TMP_FontAsset _cached;
        static bool _attempted;
        static bool _warned;

        public static TMP_FontAsset Resolve(TMP_FontAsset preferred)
        {
            if (preferred != null) return preferred;
            if (_attempted) return _cached;

            _attempted = true;

            // 优先级：Resources/Fonts 里你放的中文字体资产 > 系统字体现场生成 > TMP 默认字体
            _cached = FromResourcesFolder();
            if (_cached == null) _cached = CreateFromOsFont();

            if (_cached == null)
            {
                _cached = DefaultFont();
                WarnOnce();
            }

            return _cached;
        }

        /// <summary>给自检日志用的一句话描述。</summary>
        public static string Describe(TMP_FontAsset asset)
        {
            if (asset == null) return "（空，会退回 TMP 默认字体）";

            return asset.name + (CanRenderChinese(asset) ? "  [中文可用]" : "  [缺少中文字形!]");
        }

        static TMP_FontAsset FromResourcesFolder()
        {
            try
            {
                var all = Resources.LoadAll<TMP_FontAsset>(FontResourceFolder);
                for (int i = 0; i < all.Length; i++)
                {
                    if (CanRenderChinese(all[i])) return all[i];
                }
            }
            catch (Exception)
            {
                // 没有这个文件夹就当没有
            }

            return null;
        }

        static TMP_FontAsset CreateFromOsFont()
        {
            for (int i = 0; i < Candidates.Length; i++)
            {
                try
                {
                    var osFont = Font.CreateDynamicFontFromOSFont(Candidates[i], 32);
                    if (osFont == null) continue;

                    var asset = TMP_FontAsset.CreateFontAsset(
                        osFont,
                        90,                        // 采样字号
                        9,                         // 图集留白
                        GlyphRenderMode.SDFAA,
                        1024,
                        1024,
                        AtlasPopulationMode.Dynamic,
                        true);

                    if (asset == null) continue;

                    asset.name = "OSFont_" + Candidates[i];

                    // 顺手验一下这个字体到底认不认中文，不认就换下一个候选
                    if (CanRenderChinese(asset)) return asset;
                }
                catch (Exception)
                {
                    // 这台机器上这个字体不能用，试下一个
                }
            }

            return null;
        }

        /// <summary>这个字体资产到底能不能显示中文。</summary>
        public static bool CanRenderChinese(TMP_FontAsset asset)
        {
            if (asset == null) return false;

            try
            {
                string missing;
                asset.TryAddCharacters(SampleText, out missing);
                return asset.HasCharacter('测');
            }
            catch (Exception)
            {
                return false;
            }
        }

        static TMP_FontAsset DefaultFont()
        {
            try
            {
                return TMP_Settings.defaultFontAsset;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static void WarnOnce()
        {
            if (_warned) return;
            _warned = true;

            Debug.LogWarning(
                "[Dialogue] 没能自动找到系统中文字体，中文可能显示成方块。\n" +
                "解决办法：找一个中文 ttf/otf 放进项目，右键 Create > TextMeshPro > Font Asset，" +
                "然后把做好的字体资产丢进 Assets/Resources/" + FontResourceFolder + "/ 就会被自动用上" +
                "（或者拖到场景里 [DialogueSystem] 的 Font Asset 上）。");
        }
    }
}
