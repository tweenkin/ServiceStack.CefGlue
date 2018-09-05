using System;
using System.Runtime.InteropServices;
using Xilium.CefGlue;
using WinApi.Windows;
using ServiceStack.CefGlue.Win64;

namespace ServiceStack.CefGlue
{
    public sealed class CefPlatformWindows : CefPlatform
    {
        private static CefPlatformWindows provider;
        public static CefPlatformWindows Provider => provider ?? (provider = new CefPlatformWindows());
        private CefPlatformWindows() { }

        public static int Start(CefConfig config)
        {
            Instance = Provider;
            return Provider.Run(config);
        }

        public int Run(CefConfig config)
        {
            if (config.Width == 0 || config.Height == 0)
            {
                var res = Instance.GetScreenResolution();
                config.Width = (int)(res.Width * .75);
                config.Height = (int)(res.Height * .75);
            }
            
            if (config.HideConsoleWindow)
                Instance.HideConsoleWindow();

            var factory = WinapiHostFactory.Init(config.Icon);
            using (var window = factory.CreateWindow(
                () => new CefGlueHost(config),
                config.WindowTitle,
                constructionParams: new FrameWindowConstructionParams()))
            {

                window.SetSize(config.Width, config.Height);
                if (config.CenterToScreen)
                {
                    window.CenterToScreen();
                }
                else if (config.X != null || config.Y != null)
                {
                    window.SetPosition(config.X.GetValueOrDefault(), config.Y.GetValueOrDefault());
                }
                window.Show();

                //startTask.Wait();
                return new EventLoop().Run(window);
            }
        }

        public override CefSize GetScreenResolution()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            var scalingFactor = GetScalingFactor(hdc);
            return new CefSize(
                (int)(GetSystemMetrics(SystemMetric.SM_CXSCREEN) * scalingFactor),
                (int)(GetSystemMetrics(SystemMetric.SM_CYSCREEN) * scalingFactor)
            );
        }

        float GetScalingFactor(IntPtr hdc)
        {
            int LogicalScreenHeight = GetDeviceCaps(hdc, VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(hdc, DESKTOPVERTRES);
            return (float)PhysicalScreenHeight / (float)LogicalScreenHeight;
        }

        public override void HideConsoleWindow()
        {
            Console.Title = typeof(CefPlatformWindows).Namespace;
            var hWnd = FindWindow(null, Console.Title);
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, 0);
            }
        }

        public override void ResizeWindow(IntPtr handle, int width, int height)
        {
            if (handle != IntPtr.Zero)
            {
                SetWindowPos(handle, IntPtr.Zero,
                    0, 0, width, height,
                    SetWindowPosFlags.NoZOrder
                );
            }
        }

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(SystemMetric smIndex);

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        const int VERTRES = 10;
        const int DESKTOPVERTRES = 117;
        const int LOGPIXELSX = 88;
        const int LOGPIXELSY = 90;

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [Flags]
        internal enum SetWindowPosFlags : uint
        {
            AsyncWindowPosition = 0x4000,
            DeferErase = 0x2000,
            DrawFrame = 0x0020,
            FrameChanged = 0x0020,
            HideWindow = 0x0080,
            NoActivate = 0x0010,
            NoCopyBits = 0x0100,
            NoMove = 0x0002,
            NoOwnerZOrder = 0x0200,
            NoRedraw = 0x0008,
            NoReposition = 0x0200,
            NoSendChanging = 0x0400,
            NoSize = 0x0001,
            NoZOrder = 0x0004,
            ShowWindow = 0x0040,
        }

        internal enum SystemMetric
        {
            SM_CXSCREEN = 0,  // 0x00
            SM_CYSCREEN = 1,  // 0x01
            SM_CXVSCROLL = 2,  // 0x02
            SM_CYHSCROLL = 3,  // 0x03
            SM_CYCAPTION = 4,  // 0x04
            SM_CXBORDER = 5,  // 0x05
            SM_CYBORDER = 6,  // 0x06
            SM_CXDLGFRAME = 7,  // 0x07
            SM_CXFIXEDFRAME = 7,  // 0x07
            SM_CYDLGFRAME = 8,  // 0x08
            SM_CYFIXEDFRAME = 8,  // 0x08
            SM_CYVTHUMB = 9,  // 0x09
            SM_CXHTHUMB = 10, // 0x0A
            SM_CXICON = 11, // 0x0B
            SM_CYICON = 12, // 0x0C
            SM_CXCURSOR = 13, // 0x0D
            SM_CYCURSOR = 14, // 0x0E
            SM_CYMENU = 15, // 0x0F
            SM_CXFULLSCREEN = 16, // 0x10
            SM_CYFULLSCREEN = 17, // 0x11
            SM_CYKANJIWINDOW = 18, // 0x12
            SM_MOUSEPRESENT = 19, // 0x13
            SM_CYVSCROLL = 20, // 0x14
            SM_CXHSCROLL = 21, // 0x15
            SM_DEBUG = 22, // 0x16
            SM_SWAPBUTTON = 23, // 0x17
            SM_CXMIN = 28, // 0x1C
            SM_CYMIN = 29, // 0x1D
            SM_CXSIZE = 30, // 0x1E
            SM_CYSIZE = 31, // 0x1F
            SM_CXSIZEFRAME = 32, // 0x20
            SM_CXFRAME = 32, // 0x20
            SM_CYSIZEFRAME = 33, // 0x21
            SM_CYFRAME = 33, // 0x21
            SM_CXMINTRACK = 34, // 0x22
            SM_CYMINTRACK = 35, // 0x23
            SM_CXDOUBLECLK = 36, // 0x24
            SM_CYDOUBLECLK = 37, // 0x25
            SM_CXICONSPACING = 38, // 0x26
            SM_CYICONSPACING = 39, // 0x27
            SM_MENUDROPALIGNMENT = 40, // 0x28
            SM_PENWINDOWS = 41, // 0x29
            SM_DBCSENABLED = 42, // 0x2A
            SM_CMOUSEBUTTONS = 43, // 0x2B
            SM_SECURE = 44, // 0x2C
            SM_CXEDGE = 45, // 0x2D
            SM_CYEDGE = 46, // 0x2E
            SM_CXMINSPACING = 47, // 0x2F
            SM_CYMINSPACING = 48, // 0x30
            SM_CXSMICON = 49, // 0x31
            SM_CYSMICON = 50, // 0x32
            SM_CYSMCAPTION = 51, // 0x33
            SM_CXSMSIZE = 52, // 0x34
            SM_CYSMSIZE = 53, // 0x35
            SM_CXMENUSIZE = 54, // 0x36
            SM_CYMENUSIZE = 55, // 0x37
            SM_ARRANGE = 56, // 0x38
            SM_CXMINIMIZED = 57, // 0x39
            SM_CYMINIMIZED = 58, // 0x3A
            SM_CXMAXTRACK = 59, // 0x3B
            SM_CYMAXTRACK = 60, // 0x3C
            SM_CXMAXIMIZED = 61, // 0x3D
            SM_CYMAXIMIZED = 62, // 0x3E
            SM_NETWORK = 63, // 0x3F
            SM_CLEANBOOT = 67, // 0x43
            SM_CXDRAG = 68, // 0x44
            SM_CYDRAG = 69, // 0x45
            SM_SHOWSOUNDS = 70, // 0x46
            SM_CXMENUCHECK = 71, // 0x47
            SM_CYMENUCHECK = 72, // 0x48
            SM_SLOWMACHINE = 73, // 0x49
            SM_MIDEASTENABLED = 74, // 0x4A
            SM_MOUSEWHEELPRESENT = 75, // 0x4B
            SM_XVIRTUALSCREEN = 76, // 0x4C
            SM_YVIRTUALSCREEN = 77, // 0x4D
            SM_CXVIRTUALSCREEN = 78, // 0x4E
            SM_CYVIRTUALSCREEN = 79, // 0x4F
            SM_CMONITORS = 80, // 0x50
            SM_SAMEDISPLAYFORMAT = 81, // 0x51
            SM_IMMENABLED = 82, // 0x52
            SM_CXFOCUSBORDER = 83, // 0x53
            SM_CYFOCUSBORDER = 84, // 0x54
            SM_TABLETPC = 86, // 0x56
            SM_MEDIACENTER = 87, // 0x57
            SM_STARTER = 88, // 0x58
            SM_SERVERR2 = 89, // 0x59
            SM_MOUSEHORIZONTALWHEELPRESENT = 91, // 0x5B
            SM_CXPADDEDBORDER = 92, // 0x5C
            SM_DIGITIZER = 94, // 0x5E
            SM_MAXIMUMTOUCHES = 95, // 0x5F

            SM_REMOTESESSION = 0x1000, // 0x1000
            SM_SHUTTINGDOWN = 0x2000, // 0x2000
            SM_REMOTECONTROL = 0x2001, // 0x2001


            SM_CONVERTIBLESLATEMODE = 0x2003,
            SM_SYSTEMDOCKED = 0x2004,
        }
    }
}