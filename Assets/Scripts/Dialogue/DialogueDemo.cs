using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Kongyou.Dialogue
{
    /// <summary>
    /// 演示用的小开关：按 T 播 Resources/Dialogues/demo.txt，按 Y 切换自动播放。
    /// 正式接入之后把这个脚本删掉就行 —— 文字系统本身不依赖它。
    /// </summary>
    public class DialogueDemo : MonoBehaviour
    {
        [Tooltip("要演示的剧本名（Resources/Dialogues 下的文件名，不用写后缀）")]
        public string scriptName = "demo";

        void Update()
        {
            if (Pressed(GameKey.T))
            {
                if (DialogueSystem.IsPlaying) DialogueSystem.Instance.End();
                else DialogueSystem.Play(scriptName);
            }

            if (Pressed(GameKey.Y))
            {
                DialogueSystem.AutoMode = !DialogueSystem.AutoMode;
                Debug.Log("[Dialogue] 自动播放：" + (DialogueSystem.AutoMode ? "开" : "关"));
            }
        }

        enum GameKey { T, Y }

        static bool Pressed(GameKey key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;

            switch (key)
            {
                case GameKey.T: return kb.tKey.wasPressedThisFrame;
                case GameKey.Y: return kb.yKey.wasPressedThisFrame;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (key)
            {
                case GameKey.T: return Input.GetKeyDown(KeyCode.T);
                case GameKey.Y: return Input.GetKeyDown(KeyCode.Y);
            }
            return false;
#else
            return false;
#endif
        }
    }
}
