using System;

namespace capture
{
    class Voice
    {
        public static async void Main(string koe)
        {
            System.Diagnostics.Process pro = new System.Diagnostics.Process();

            pro.StartInfo.FileName = "softalk"; 
            pro.StartInfo.Arguments = "/close /w:" + koe;               // 引数
            pro.StartInfo.CreateNoWindow = true;            // DOSプロンプトの黒い画面を非表示
            pro.StartInfo.UseShellExecute = true;          // プロセスを新しいウィンドウで起動するか否か
            pro.StartInfo.RedirectStandardOutput = false;    // 標準出力をリダイレクトして取得したい

            pro.Start();
        }
    }
}
