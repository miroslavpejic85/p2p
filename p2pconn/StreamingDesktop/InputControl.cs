using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace p2pconn
{
    public class InputControl : System.MarshalByRefObject
    {
        enum MouseEventFlags : uint
        {
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP = 0x0040,
            MOUSEEVENTF_XDOWN = 0x0080,
            MOUSEEVENTF_XUP = 0x0100,
            MOUSEEVENTF_WHEEL = 0x0800,
            MOUSEEVENTF_VIRTUALDESK = 0x4000,
            MOUSEEVENTF_ABSOLUTE = 0x8000
        }
        enum SendInputEventType : int
        {
            InputMouse,
            InputKeyboard,
            InputHardware
        }

        enum keyboardEventFlags : uint
        {
              KEYEVENTF_EXTENDEDKEY = 0x1,
              KEYEVENTF_KEYUP = 0x2,
              INPUT_MOUSE = 0,
              INPUT_KEYBOARD = 1
        }

        private const uint KEYEVENTF_EXTENDEDKEY = 0x1;
        private const uint KEYEVENTF_KEYUP = 0x2;
        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;
 
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSE_INPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public MouseEventFlags dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBD_INPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public uint dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWARE_INPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public SendInputEventType type;
            public MouseKeybdhardwareInputUnion mkhi;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct MouseKeybdhardwareInputUnion
        {
            [FieldOffset(0)]
            public MOUSE_INPUT mi;

            [FieldOffset(0)]
            public KEYBD_INPUT ki;

            [FieldOffset(0)]
            public HARDWARE_INPUT hi;
        }

        // count of input events
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, ref INPUT input, int cbSize);
        public void PressOrReleaseMouseButton(bool Press, bool Left, int X, int Y)
        {
            INPUT mouseDownUpInput = new INPUT();
            mouseDownUpInput.type = SendInputEventType.InputMouse;
            if (Left)
            {
                mouseDownUpInput.mkhi.mi.dwFlags = Press ? MouseEventFlags.MOUSEEVENTF_LEFTDOWN : MouseEventFlags.MOUSEEVENTF_LEFTUP;
            }
            else
            {
                mouseDownUpInput.mkhi.mi.dwFlags = Press ? MouseEventFlags.MOUSEEVENTF_RIGHTDOWN : MouseEventFlags.MOUSEEVENTF_RIGHTUP;
            }
            SendInput(1, ref mouseDownUpInput, Marshal.SizeOf(new INPUT()));
        }

        #region "Mouse Whell"
        public void MouseWheel1(int Delta)
        {
            try
            {
                INPUT mouseWheelInput = new INPUT();
                mouseWheelInput.type = SendInputEventType.InputMouse;
                mouseWheelInput.mkhi.mi.dx = 0;
                mouseWheelInput.mkhi.mi.dy = 0;
                mouseWheelInput.mkhi.mi.mouseData = Convert.ToUInt32(Delta);
                mouseWheelInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_WHEEL;
                mouseWheelInput.mkhi.mi.time = 0;
                SendInput(1, ref mouseWheelInput, Marshal.SizeOf(new INPUT()));
            }
            catch
            {
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private const int MOUSEEVENTF_WHEEL = 0x0800;
        public void MouseWheel(int Delta)
        {
            try
            {
                mouse_event(MOUSEEVENTF_WHEEL, 0, 0, Delta, 0);
            }
            catch
            {
            }
        }
        #endregion

        [DllImport("user32.dll")]
        private static extern void SetCursorPos(int x, int y);

        public void MoveMouse(int x, int y)
        {
            Point B = new Point(x, y);
            Cursor.Position = B;
        }
        public void SendKeystroke(byte VirtualKeyCode, byte ScanCode, bool KeyDown, bool ExtendedKey)
        {
            INPUT KeyboardInput = new INPUT();
            KeyboardInput.type = SendInputEventType.InputKeyboard;
            KeyboardInput.mkhi.ki.wVk = VirtualKeyCode;
            KeyboardInput.mkhi.ki.wScan = ScanCode;
            KeyboardInput.mkhi.ki.dwExtraInfo = 0;
            KeyboardInput.mkhi.ki.time = 0;
            if (!KeyDown)
            {
                KeyboardInput.mkhi.ki.dwFlags = KeyboardInput.mkhi.ki.dwFlags | KEYEVENTF_KEYUP;
            }
            if (ExtendedKey)
            {
                KeyboardInput.mkhi.ki.dwFlags = KeyboardInput.mkhi.ki.dwFlags | KEYEVENTF_EXTENDEDKEY;
            }
            SendInput(1, ref KeyboardInput, Marshal.SizeOf(new INPUT()));
        }
    }
}
