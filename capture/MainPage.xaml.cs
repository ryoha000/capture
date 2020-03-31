using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Timers;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;

namespace capture
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Capture API objects.
        private SizeInt32 _lastSize;
        private GraphicsCaptureItem _item;
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _session;

        // Non-API related members.
        private CanvasDevice _canvasDevice;
        private CompositionGraphicsDevice _compositionGraphicsDevice;
        private Compositor _compositor;
        private CompositionDrawingSurface _surface;
        public static CanvasBitmap _currentFrame;
        private string _screenshotFilename = "test.png";
        public static GraphicsCaptureItem targetcap;
        private static double scale;

        public static uint lineStartX;
        public static uint lineStartY;
        public static uint lineHeight;
        public static uint lineWidth;
        private static SoftwareBitmap line;
        private static uint charactorStartX;
        private static uint charactorStartY;
        private static uint charactorHeight;
        private static uint charactorWidth;
        private static SoftwareBitmap charactor;
        private static bool isReadyNarator = false;
        private static bool isStartNarator = false;
        private static SoftwareBitmap outputBitmap;
        private static OcrEngine engine;
        private static OcrResult lineResult;
        private static OcrResult charaResult;
        private static byte[] bits;
        private static WriteableBitmap wb;

        private static List<(string, string)> texts = new List<(string, string)>();

        public MainPage()
        {
            this.InitializeComponent();
            scale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            Setup();
        }

        private void Setup()
        {
            _canvasDevice = new CanvasDevice();

            _compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(
                Windows.UI.Xaml.Window.Current.Compositor,
                _canvasDevice);

            _compositor = Windows.UI.Xaml.Window.Current.Compositor;

            _surface = _compositionGraphicsDevice.CreateDrawingSurface(
                new Windows.Foundation.Size(400, 400),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);    // This is the only value that currently works with
                                                    // the composition APIs.

            var visual = _compositor.CreateSpriteVisual();
            visual.RelativeSizeAdjustment = Vector2.One;
            var brush = _compositor.CreateSurfaceBrush(_surface);
            brush.HorizontalAlignmentRatio = 0.5f;
            brush.VerticalAlignmentRatio = 0.5f;
            brush.Stretch = CompositionStretch.Uniform;
            visual.Brush = brush;
            ElementCompositionPreview.SetElementChildVisual(this, visual);
        }

        public async Task StartCaptureAsync()
        {
            // The GraphicsCapturePicker follows the same pattern the
            // file pickers do.
            var picker = new GraphicsCapturePicker();
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();
            targetcap = item;

            // The item may be null if the user dismissed the
            // control without making a selection or hit Cancel.
            if (item != null)
            {
                StartCaptureInternal(item);
            }
        }

        private void StartCaptureInternal(GraphicsCaptureItem item)
        {
            // Stop the previous capture if we had one.
            StopCapture();
            isStartNarator = false;
            isReadyNarator = false;

            _item = item;
            _lastSize = _item.Size;

            _framePool = Direct3D11CaptureFramePool.Create(
               _canvasDevice, // D3D device
               DirectXPixelFormat.B8G8R8A8UIntNormalized, // Pixel format
               2, // Number of frames
               _item.Size); // Size of the buffers

            _framePool.FrameArrived += (s, a) =>
            {
                // The FrameArrived event is raised for every frame on the thread
                // that created the Direct3D11CaptureFramePool. This means we
                // don't have to do a null-check here, as we know we're the only
                // one dequeueing frames in our application.  

                // NOTE: Disposing the frame retires it and returns  
                // the buffer to the pool.

                using (var frame = _framePool.TryGetNextFrame())
                {
                    ProcessFrame(frame);
                }
            };

            _item.Closed += (s, a) =>
            {
                StopCapture();
            };

            _session = _framePool.CreateCaptureSession(_item);
            _session.StartCapture();
        }

        public void StopCapture()
        {
            _session?.Dispose();
            _framePool?.Dispose();
            _item = null;
            _session = null;
            _framePool = null;
        }

        private void ProcessFrame(Direct3D11CaptureFrame frame)
        {
            // Resize and device-lost leverage the same function on the
            // Direct3D11CaptureFramePool. Refactoring it this way avoids
            // throwing in the catch block below (device creation could always
            // fail) along with ensuring that resize completes successfully and
            // isn’t vulnerable to device-lost.
            bool needsReset = false;
            bool recreateDevice = false;

            if ((frame.ContentSize.Width != _lastSize.Width) ||
                (frame.ContentSize.Height != _lastSize.Height))
            {
                needsReset = true;
                _lastSize = frame.ContentSize;
            }

            try
            {
                // Take the D3D11 surface and draw it into a  
                // Composition surface.

                // Convert our D3D11 surface into a Win2D object.
                CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(
                    _canvasDevice,
                    frame.Surface);

                _currentFrame = canvasBitmap;
                if (isReadyNarator && !isStartNarator)
                {
                    isStartNarator = true;
                    StartNarator();
                }

                // Helper that handles the drawing for us.
                //FillSurfaceWithBitmap(canvasBitmap);
            }

            // This is the device-lost convention for Win2D.
            catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
            {
                // We lost our graphics device. Recreate it and reset
                // our Direct3D11CaptureFramePool.  
                needsReset = true;
                recreateDevice = true;
            }

            if (needsReset)
            {
                ResetFramePool(frame.ContentSize, recreateDevice);
            }
        }

        private void ResetFramePool(SizeInt32 size, bool recreateDevice)
        {
            do
            {
                try
                {
                    if (recreateDevice)
                    {
                        _canvasDevice = new CanvasDevice();
                    }

                    _framePool.Recreate(
                        _canvasDevice,
                        DirectXPixelFormat.B8G8R8A8UIntNormalized,
                        2,
                        size);
                }
                // This is the device-lost convention for Win2D.
                catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
                {
                    _canvasDevice = null;
                    recreateDevice = true;
                }
            } while (_canvasDevice == null);
        }

        private async void Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            await StartCaptureAsync();
        }

        private async void ScreenshotButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            await SaveImageAsync(_screenshotFilename, _currentFrame);
        }

        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveRecordingAsyncM("a.mp4", 5000);
        }

        private async Task SaveImageAsync(string filename, CanvasBitmap frame)
        {
            StorageFolder pictureFolder = KnownFolders.SavedPictures;
            StorageFile file = await pictureFolder.CreateFileAsync(
                filename,
                CreationCollisionOption.ReplaceExisting);

            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                await frame.SaveAsync(fileStream, CanvasBitmapFileFormat.Png, 1f);
            }
        }

        private async Task SaveRecordingAsyncM(string filename, int span)
        {
            Windows.Media.AppRecording.AppRecordingManager am = Windows.Media.AppRecording.AppRecordingManager.GetDefault();
            if (am.GetStatus().CanRecord)
            {
                StorageFolder pictureFolder = KnownFolders.SavedPictures;
                StorageFile file = await pictureFolder.CreateFileAsync(
                    filename,
                    CreationCollisionOption.ReplaceExisting);
                var rec = am.StartRecordingToFileAsync(file);
                await Task.Delay(span);
                rec.Cancel();
            }
        }

        private async void OpenNaratorWindowAsync(object sender, RoutedEventArgs e)
        {
            var currentViewId = ApplicationView.GetForCurrentView().Id;
            await CoreApplication.CreateNewView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                Window.Current.Content = new Frame();
                ((Frame)Window.Current.Content).Navigate(typeof(NaratorWindowFirst));
                Window.Current.Activate();
                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(
                    ApplicationView.GetApplicationViewIdForWindow(Window.Current.CoreWindow),
                    ViewSizePreference.Default,
                    currentViewId,
                    ViewSizePreference.Default);
            });
        }

        public static async Task ByteToWriteableBitmap(WriteableBitmap wb, byte[] bgra)
        {
            using (Stream stream = wb.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(bgra, 0, bgra.Length);
            }
        }

        public static async Task<SoftwareBitmap> GetCroppedBitmapAsync(SoftwareBitmap softwareBitmap, uint startPointX, uint startPointY, uint width, uint height)
        {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);

                encoder.SetSoftwareBitmap(softwareBitmap);

                encoder.BitmapTransform.Bounds = new BitmapBounds()
                {
                    X = startPointX * (uint)scale,
                    Y = startPointY * (uint)scale,
                    Height = height * (uint)scale,
                    Width = width * (uint)scale
                };


                await encoder.FlushAsync();

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                return await decoder.GetSoftwareBitmapAsync(softwareBitmap.BitmapPixelFormat, BitmapAlphaMode.Premultiplied);
            }
        }

        public static void SetCoordinate(double x1, double x2, double y1, double y2, string type)
        {
            uint startX = 0;
            uint width = 0;
            uint startY = 0;
            uint height = 0;
            if (x1 <= x2)
            {
                startX = (uint)x1;
                width = (uint)(x2 - x1);
            }
            else
            {
                startX = (uint)x2;
                width = (uint)(x1 - x2);
            }
            if (y1 <= y2)
            {
                startY = (uint)y1;
                height = (uint)(y2 - y1);
            }
            else
            {
                startY = (uint)y2;
                height = (uint)(y1 - y2);
            }
            if (type == "line")
            {
                lineStartX = startX;
                lineStartY = startY;
                lineWidth = width;
                lineHeight = height;
            }
            else if (type == "charactor")
            {
                charactorStartX = startX;
                charactorStartY = startY;
                charactorWidth = width;
                charactorHeight = height;
            }
        }

        public static void ReadyNarator()
        {
            isReadyNarator = true;

            // OCRの準備。言語設定を日本語にする
            Windows.Globalization.Language language = new Windows.Globalization.Language("ja");
            engine = OcrEngine.TryCreateFromLanguage(language);
        }

        private void StartNarator()
        {
            TimeSpan period = TimeSpan.FromMilliseconds(200);

            ThreadPoolTimer PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
            {
                //
                // TODO: Work
                //

                //
                // Update the UI thread by using the UI core dispatcher.
                //
                await Dispatcher.RunAsync(CoreDispatcherPriority.High,
                    async () =>
                    {
                        //
                        // UI components can be accessed within this scope.
                        //
                        wb = new WriteableBitmap(_lastSize.Width, _lastSize.Height);
                        bits = _currentFrame.GetPixelBytes();
                        await ByteToWriteableBitmap(wb, bits);
                        outputBitmap = SoftwareBitmap.CreateCopyFromBuffer(
                            wb.PixelBuffer,
                            BitmapPixelFormat.Bgra8,
                            wb.PixelWidth,
                            wb.PixelHeight
                        );
                        if (lineWidth != 0 && charactorWidth != 0)
                        {
                            line = await GetCroppedBitmapAsync(outputBitmap, lineStartX, lineStartY, lineWidth, lineHeight);
                            lineResult = await RunWin10Ocr(line);
                            // System.Diagnostics.Debug.WriteLine("line");
                            // System.Diagnostics.Debug.WriteLine(ocrResultL.Text);
                            
                            charactor = await GetCroppedBitmapAsync(outputBitmap, charactorStartX, charactorStartY, charactorWidth, charactorHeight);
                            charaResult = await RunWin10Ocr(charactor);
                            // System.Diagnostics.Debug.WriteLine("chara");
                            // System.Diagnostics.Debug.WriteLine(ocrResultC.Text);
                            if (texts.Count > 0 && texts[texts.Count - 1] != (charaResult.Text, lineResult.Text))
                            {
                                texts.Add((charaResult.Text, lineResult.Text));
                            }
                            if (texts.Count > 100)
                            {
                                foreach (var text in texts)
                                {
                                    await Windows.Storage.FileIO.WriteTextAsync(NaratorWindowSecond._file, text.Item1 + "," + text.Item2);
                                }
                                texts = texts.GetRange(100, texts.Count - 100);
                            }
                        }
                        outputBitmap.Dispose();
                        line.Dispose();
                        charactor.Dispose();
                    });

            }, period);
        }

        async Task<OcrResult> RunWin10Ocr(SoftwareBitmap snap)
        {

            // OCRをはしらせる
            var ocrResult = await engine.RecognizeAsync(snap);
            return ocrResult;
        }
    }
}
