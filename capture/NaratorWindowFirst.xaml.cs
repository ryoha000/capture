using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Graphics.Canvas.Effects;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace capture
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class NaratorWindowFirst : Page
    {
        private CanvasBitmap Image { get; set; }

        public NaratorWindowFirst()
        {
            this.InitializeComponent();
            setImage();
        }

        private async void setImage()
        {
            WriteableBitmap wb = new WriteableBitmap((int)MainPage._nowFrame.Size.Width, (int)MainPage._nowFrame.Size.Height);
            await ByteToWriteableBitmap(wb, MainPage._nowFrame.GetPixelBytes());
            SoftwareBitmap outputBitmap = SoftwareBitmap.CreateCopyFromBuffer(
                wb.PixelBuffer,
                BitmapPixelFormat.Bgra8,
                wb.PixelWidth,
                wb.PixelHeight
            );
            SoftwareBitmap displayableImage = SoftwareBitmap.Convert(outputBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(displayableImage);
            imageControl.Source = source;
        }

        private async Task ByteToWriteableBitmap(WriteableBitmap wb, byte[] bgra)
        {
            using (Stream stream = wb.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(bgra, 0, bgra.Length);
            }
        }
    }
}
