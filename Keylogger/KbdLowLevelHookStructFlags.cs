﻿using System;

namespace Keylogger
{
    [Flags]
    public enum KbdLowLevelHookStructFlags : uint
    {
        LLKHF_EXTENDED = 0x01,
        LLKHF_INJECTED = 0x10,
        LLKHF_ALTDOWN = 0x20,
        LLKHF_UP = 0x80
    }
}
