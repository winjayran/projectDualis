using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Debug = UnityEngine.Debug;

namespace ProjectDualis.Core
{
    /// <summary>
    /// Platform-specific window controller for transparent and always-on-top windows.
    /// Currently supports Windows DWM API.
    /// </summary>
    public class WindowController : MonoBehaviour
    {
        private DualisConfig config;
        private bool isInitialized = false;
        private IntPtr hwnd = IntPtr.Zero;

#if UNITY_STANDALONE_WIN
        #region Windows DWM API

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int LWA_COLORKEY = 0x00000001;
        private const int LWA_ALPHA = 0x00000002;
        private const int ULW_COLORKEY = 0x00000001;
        private const int ULW_ALPHA = 0x00000002;
        private const int ULW_OPAQUE = 0x00000004;

        // DwmExtendFrameIntoClientArea
        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_EXCLUDED_FROM_PEEK = 12;
        private const int DWMWA_DISALLOW_PEEK = 11;
        private const int DWMWA_HAS_ICONIC_BITMAP = 10;
        private const int DWMWA_FORCE_ICONIC_REPRESENTATION = 7;

        // SetWindowPos flags
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOREDRAW = 0x0008;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_HIDEWINDOW = 0x0080;
        private const uint SWP_NOOWNERZORDER = 0x0200;

        // HWND insertion points
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private static readonly IntPtr HWND_TOP = new IntPtr(0);

        #endregion
#endif

        public void Initialize(DualisConfig config)
        {
            this.config = config;

#if UNITY_STANDALONE_WIN
            // Get window handle
            StartCoroutine(GetWindowHandleDelayed());
#elif UNITY_STANDALONE_OSX
            Debug.LogWarning("[WindowController] macOS transparent window support requires native plugin.");
#elif UNITY_STANDALONE_LINUX
            Debug.LogWarning("[WindowController] Linux transparent window support requires X11 compositing.");
#endif
        }

        private System.Collections.IEnumerator GetWindowHandleDelayed()
        {
            // Wait a frame for the window to be created
            yield return null;

#if UNITY_STANDALONE_WIN
            hwnd = GetActiveWindow();
            isInitialized = true;

            if (hwnd != IntPtr.Zero)
            {
                ApplyWindowSettings();
                Debug.Log("[WindowController] Window handle obtained: " + hwnd);
            }
            else
            {
                Debug.LogWarning("[WindowController] Failed to get window handle.");
            }
#endif
        }

        public void SetTransparent(bool transparent)
        {
            config.transparentBackground = transparent;

            if (isInitialized)
            {
                ApplyWindowSettings();
            }
        }

        public void SetAlwaysOnTop(bool onTop)
        {
            config.alwaysOnTop = onTop;

            if (isInitialized)
            {
                ApplyWindowSettings();
            }
        }

        private void ApplyWindowSettings()
        {
#if UNITY_STANDALONE_WIN
            if (hwnd == IntPtr.Zero) return;

            try
            {
                // Always on top
                if (config.alwaysOnTop)
                {
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
                else
                {
                    SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }

                // Transparent background
                if (config.transparentBackground)
                {
                    // Extend glass frame into client area
                    MARGINS margins = new MARGINS
                    {
                        cxLeftWidth = -1,
                        cxRightWidth = -1,
                        cyTopHeight = -1,
                        cyBottomHeight = -1
                    };
                    DwmExtendFrameIntoClientArea(hwnd, ref margins);

                    // Set window style for layered window
                    int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    exStyle |= WS_EX_LAYERED;
                    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

                    // Make black color transparent (for background clearing)
                    SetLayeredWindowAttributes(hwnd, 0xFF000000, 255, LWA_COLORKEY);
                }
                else
                {
                    // Remove transparency
                    int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    exStyle &= ~WS_EX_LAYERED;
                    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
                }

                Debug.Log($"[WindowController] Window settings applied. Transparent: {config.transparentBackground}, OnTop: {config.alwaysOnTop}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WindowController] Failed to apply window settings: {e.Message}");
            }
#endif
        }

        public void SetWindowPosition(int x, int y, int width, int height)
        {
#if UNITY_STANDALONE_WIN
            if (hwnd == IntPtr.Zero) return;

            SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height,
                SWP_NOZORDER | SWP_NOACTIVATE);
#endif
        }

        private void Update()
        {
            // Re-apply settings if needed (some settings may be reset by Unity)
#if UNITY_STANDALONE_WIN
            if (isInitialized && hwnd != IntPtr.Zero)
            {
                // Keep window on top if enabled
                if (config.alwaysOnTop)
                {
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
                }
            }
#endif
        }

        /// <summary>
        /// Check if transparent window is supported on current platform.
        /// </summary>
        public static bool IsTransparentSupported
        {
            get
            {
#if UNITY_STANDALONE_WIN
                return true;
#elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
                return false;
#else
                return false;
#endif
            }
        }
    }
}
