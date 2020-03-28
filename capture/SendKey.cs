using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace capture
{
    class SendKey
    {
        public const int WM_LBUTTONDOWN = 0x201;
        public const int WM_LBUTTONUP = 0x202;
        public const int MK_LBUTTON = 0x0001;
        public static int GWL_STYLE = -16;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hWnd, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);


        //static void Main(string[] args)
        public static void sendKeyStart()
        {
            // 電卓のトップウィンドウのウィンドウハンドル（※見つかることを前提としている）
            IntPtr mainWindowHandle = FindWindow(null, MainPage.targetcap.DisplayName);

            // 対象のボタンを探す
            //var hWnd = FindTargetButton(GetWindow(mainWindowHandle));
            FindTargetButton(GetWindow(mainWindowHandle));

            // マウスを押して放す
            //SendMessage(hWnd, WM_LBUTTONDOWN, MK_LBUTTON, 0x000A000A);
            //SendMessage(hWnd, WM_LBUTTONUP, 0x00000000, 0x000A000A);
        }

        // 全てのボタンを列挙し、その10番目のボタンのウィンドウハンドルを返す
        private static void FindTargetButton(Window top)
        {
            var all = GetAllChildWindows(top, new List<Window>());
            foreach (Window window in all)
            {
                var magos = GetAllChildWindows(window, new List<Window>());
                foreach (Window mago in magos)
                {
                    System.Diagnostics.Debug.WriteLine(mago.ClassName);
                }
                System.Diagnostics.Debug.WriteLine(window.ClassName);
            }
            // return all.Where(x => x.ClassName == "Button").First().hWnd;
        }

        // 
        // 指定したウィンドウの全ての子孫ウィンドウを取得し、リストに追加する
        private static List<Window> GetAllChildWindows(Window parent, List<Window> dest)
        {
            dest.Add(parent);
            EnumChildWindows(parent.hWnd).ToList().ForEach(x => GetAllChildWindows(x, dest));
            return dest;
        }

        // 与えた親ウィンドウの直下にある子ウィンドウを列挙する（孫ウィンドウは見つけてくれない）
        private static IEnumerable<Window> EnumChildWindows(IntPtr hParentWindow)
        {
            IntPtr hWnd = IntPtr.Zero;
            while ((hWnd = FindWindowEx(hParentWindow, hWnd, null, null)) != IntPtr.Zero) { yield return GetWindow(hWnd); }
        }

        // ウィンドウハンドルを渡すと、ウィンドウテキスト（ラベルなど）、クラス、スタイルを取得してWindowsクラスに格納して返す
        private static Window GetWindow(IntPtr hWnd)
        {
            int textLen = GetWindowTextLength(hWnd);
            string windowText = null;
            if (0 < textLen)
            {
                //ウィンドウのタイトルを取得する
                StringBuilder windowTextBuffer = new StringBuilder(textLen + 1);
                GetWindowText(hWnd, windowTextBuffer, windowTextBuffer.Capacity);
                windowText = windowTextBuffer.ToString();
            }

            //ウィンドウのクラス名を取得する
            StringBuilder classNameBuffer = new StringBuilder(256);
            GetClassName(hWnd, classNameBuffer, classNameBuffer.Capacity);

            // スタイルを取得する
            int style = GetWindowLong(hWnd, GWL_STYLE);
            return new Window() { hWnd = hWnd, Title = windowText, ClassName = classNameBuffer.ToString(), Style = style };
        }

        private class Window
        {
            public string ClassName;
            public string Title;
            public IntPtr hWnd;
            public int Style;
        }
    }
}
