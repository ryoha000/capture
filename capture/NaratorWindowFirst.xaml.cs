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
            using (var resourceCreator = CanvasDevice.GetSharedDevice())
            using (var canvasBitmap = MainPage._nowFrame)
            using (var canvasRenderTarget = new CanvasRenderTarget(resourceCreator, (float)MainPage._nowFrame.Size.Width, (float)MainPage._nowFrame.Size.Height, canvasBitmap.Dpi))
            using (var drawingSession = canvasRenderTarget.CreateDrawingSession())
            using (var scaleEffect = new ScaleEffect())
            {
                scaleEffect.Source = canvasBitmap;
                scaleEffect.Scale = new System.Numerics.Vector2(1,1);
                drawingSession.DrawImage(scaleEffect);
                drawingSession.Flush();
                SoftwareBitmap softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(canvasRenderTarget.GetPixelBytes().AsBuffer(), BitmapPixelFormat.Bgra8, (int)MainPage._nowFrame.Size.Width, (int)MainPage._nowFrame.Size.Height, BitmapAlphaMode.Premultiplied);

                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
        softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                var source = new SoftwareBitmapSource();
                await source.SetBitmapAsync(softwareBitmap);

                // Set the source of the Image control
                imageControl.Source = source;
            }
        }
    }
}
