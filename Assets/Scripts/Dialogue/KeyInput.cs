using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Kongyou.Dialogue
{
    /// <summary>
    /// 统一的按键查询。
    ///
    /// 这个工程用的是新版 Input System（activeInputHandler = Input System Package），
    /// 所以不能直接写 Input.GetKeyDown —— 那会直接抛异常。
    /// 这里把新旧两套 API 包一层，其余代码只管看 KeyCode。
    /// </summary>
    public static class KeyInput
    {
        public static bool WasPressedThisFrame(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;

            switch (key)
            {
                case KeyCode.Space: return kb.spaceKey.wasPressedThisFrame;
                case KeyCode.Return: return kb.enterKey.wasPressedThisFrame;
                case KeyCode.KeypadEnter: return kb.numpadEnterKey.wasPressedThisFrame;
                case KeyCode.LeftControl: return kb.leftCtrlKey.wasPressedThisFrame;
                case KeyCode.RightControl: return kb.rightCtrlKey.wasPressedThisFrame;
                case KeyCode.Escape: return kb.escapeKey.wasPressedThisFrame;
                case KeyCode.Tab: return kb.tabKey.wasPressedThisFrame;
                case KeyCode.R: return kb.rKey.wasPressedThisFrame;
                case KeyCode.T: return kb.tKey.wasPressedThisFrame;
                case KeyCode.Y: return kb.yKey.wasPressedThisFrame;
                case KeyCode.A: return kb.aKey.wasPressedThisFrame;
                case KeyCode.D: return kb.dKey.wasPressedThisFrame;
                case KeyCode.LeftArrow: return kb.leftArrowKey.wasPressedThisFrame;
                case KeyCode.RightArrow: return kb.rightArrowKey.wasPressedThisFrame;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

        public static bool IsHeld(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;

            switch (key)
            {
                case KeyCode.Space: return kb.spaceKey.isPressed;
                case KeyCode.Return: return kb.enterKey.isPressed;
                case KeyCode.LeftControl: return kb.leftCtrlKey.isPressed;
                case KeyCode.RightControl: return kb.rightCtrlKey.isPressed;
                case KeyCode.A: return kb.aKey.isPressed;
                case KeyCode.D: return kb.dKey.isPressed;
                case KeyCode.LeftArrow: return kb.leftArrowKey.isPressed;
                case KeyCode.RightArrow: return kb.rightArrowKey.isPressed;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#else
            return false;
#endif
        }

        /// <summary>鼠标左键这一帧有没有按下（点击用）。</summary>
        public static bool MousePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        /// <summary>鼠标在屏幕上的位置（像素）。</summary>
        public static Vector2 MouseScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }
    }
}
