using UnityEngine;

namespace Kongyou.Dialogue
{
    /// <summary>
    /// 程序化生成的打字音。
    /// 项目里还没放音频素材的时候靠它保证一运行就能听见声音；
    /// 以后在 DialogueSystem 上挂了真实 AudioClip，会自动优先用真实素材。
    /// </summary>
    public static class BlipSynth
    {
        static AudioClip[] _cached;

        /// <summary>生成（或取出）一组音高略有差别的打字音。</summary>
        public static AudioClip[] Bank(int count = 4)
        {
            if (_cached != null && _cached.Length == count) return _cached;

            var clips = new AudioClip[count];
            for (int i = 0; i < count; i++) clips[i] = Create(i);

            _cached = clips;
            return _cached;
        }

        static AudioClip Create(int index)
        {
            const int sampleRate = 44100;

            float duration = 0.032f + 0.007f * index;
            int samples = Mathf.Max(64, Mathf.RoundToInt(sampleRate * duration));
            var data = new float[samples];

            float baseFreq = 520f + index * 85f;    // 每个变体的基频错开一点
            float decay = 1f / (duration * 0.3f);   // 指数衰减的快慢
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;

                // 极短的起音段，避免开头"啪"的一声爆音
                float env = Mathf.Exp(-t * decay) * Mathf.Clamp01(t / 0.0015f);

                // 频率稍微往下滑一点，听起来像"嗒"而不像电子音
                phase += 2f * Mathf.PI * baseFreq * (1f - 0.35f * (t / duration)) / sampleRate;

                float tone = Mathf.Sin(phase);
                float noise = Random.value * 2f - 1f;

                data[i] = (tone * 0.75f + noise * 0.25f) * env * 0.5f;
            }

            var clip = AudioClip.Create("BlipSynth_" + index, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
