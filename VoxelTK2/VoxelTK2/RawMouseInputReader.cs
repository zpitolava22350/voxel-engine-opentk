using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VoxelTK2 {
    public static class RawMouseInputReader {
        // Constants for raw input
        private const int RIM_TYPEMOUSE = 0;
        private const uint RID_INPUT = 0x10000003;  // from winuser.h
        private const int WM_INPUT = 0x00FF;
        private const int GWL_WNDPROC = -4;
        private const ushort RI_MOUSE_WHEEL = 0x0400; // mouse wheel moved

        // P/Invoke declarations
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE {
            public ushort UsagePage;
            public ushort Usage;
            public RawInputDeviceFlags Flags;
            public IntPtr Target;
        }
        [Flags]
        private enum RawInputDeviceFlags: uint {
            None = 0,
            NoLegacy = 0x30, // RIDEV_NOLEGACY: ignore legacy mouse msgs
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(
            [In] RAWINPUTDEVICE[] pRawInputDevices,
            uint uiNumDevices,
            uint cbSize);

        [DllImport("user32.dll")]
        private static extern int GetRawInputData(
            IntPtr hRawInput,
            uint uiCommand,
            IntPtr pData,
            ref uint pcbSize,
            uint cbSizeHeader);

        // Raw input data structures
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWMOUSE {
            public ushort usFlags;
            public uint ulButtons;          // union: usButtonFlags (low) + usButtonData (high)
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        // Delegates and handles for subclassing WndProc
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private static IntPtr _oldWndProc = IntPtr.Zero;
        private static WndProcDelegate _wndProcDelegate = WndProc; // keep reference alive

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(
            IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // Movement callback (existing)
        public delegate void MouseMovedHandler(int deltaX, int deltaY);
        private static MouseMovedHandler? _moveCallback;
        public static void SetMoveCallback(MouseMovedHandler callback) {
            _moveCallback = callback;
        }

        // **NEW** Wheel callback
        public delegate void MouseWheelHandler(int wheelDelta);
        private static MouseWheelHandler? _wheelCallback;
        public static void SetWheelCallback(MouseWheelHandler callback) {
            _wheelCallback = callback;
        }

        /// <summary>
        /// Call once to start receiving raw mouse input. Pass in the Win32 window handle of the MonoGame window.
        /// </summary>
        public static void Initialize(IntPtr hWnd) {
            // Register the mouse for raw input (Generic desktop mouse, ignore legacy msgs)
            var rid = new RAWINPUTDEVICE[] {
                new RAWINPUTDEVICE {
                    UsagePage = 0x01,   // Generic desktop
                    Usage     = 0x02,   // Mouse
                    Flags     = RawInputDeviceFlags.None, // None allows the window to be interacted with still
                    Target    = hWnd
                }
            };
            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0]))) {
                Debug.WriteLine("Failed to register raw input device. Error: " + Marshal.GetLastWin32Error());
            }

            // Subclass the window to intercept WM_INPUT
            var newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
            _oldWndProc = SetWindowLongPtr(hWnd, GWL_WNDPROC, newWndProcPtr);
        }

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
            if (msg == WM_INPUT) {
                // 1) get buffer size
                uint size = 0;
                GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref size, (uint)Marshal.SizeOf<RAWINPUTHEADER>());

                if (size > 0) {
                    IntPtr buffer = Marshal.AllocHGlobal((int)size);
                    try {
                        // 2) get the raw data
                        if (GetRawInputData(lParam, RID_INPUT, buffer, ref size, (uint)Marshal.SizeOf<RAWINPUTHEADER>()) == (int)size) {
                            var header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);
                            if (header.dwType == RIM_TYPEMOUSE) {
                                // pointer to RAWMOUSE is right after the header
                                IntPtr pMouse = IntPtr.Add(buffer, Marshal.SizeOf<RAWINPUTHEADER>());
                                var mouse = Marshal.PtrToStructure<RAWMOUSE>(pMouse);

                                // Movement callback
                                _moveCallback?.Invoke(mouse.lLastX, mouse.lLastY);

                                // **Wheel callback**: unpack ulButtons
                                ushort usButtonFlags = (ushort)(mouse.ulButtons & 0xFFFF);
                                if ((usButtonFlags & RI_MOUSE_WHEEL) == RI_MOUSE_WHEEL) {
                                    // high‐word of ulButtons is the wheel delta (signed short)
                                    short wheelData = (short)((mouse.ulButtons >> 16) & 0xFFFF);
                                    _wheelCallback?.Invoke(wheelData);
                                }
                            }
                        }
                    } finally {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
            }

            // pass all other messages back to the original proc
            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }
    }
}
