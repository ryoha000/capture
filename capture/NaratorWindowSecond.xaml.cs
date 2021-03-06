﻿using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace capture
{
    public sealed partial class NaratorWindowSecond : Page
    {

        Dictionary<uint, Windows.UI.Xaml.Input.Pointer> pointers;
        public static Windows.Storage.StorageFile _file;

        public NaratorWindowSecond()
        {
            this.InitializeComponent();
            setImage();
            setSize();

            // Initialize the dictionary.
            pointers = new Dictionary<uint, Windows.UI.Xaml.Input.Pointer>();

            // Declare the pointer event handlers.
            canvas1.PointerPressed +=
                new PointerEventHandler(canvas1_MouseLeftButtonDown);
            canvas1.PointerMoved +=
                new PointerEventHandler(canvas1_MouseMove);
            canvas1.PointerReleased +=
                new PointerEventHandler(canvas1_MouseUp);
        }

        private async void finish(object sender, RoutedEventArgs e)
        {
            MainPage.ReadyNarator();

            // Create sample file; replace if exists.
            StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            System.Diagnostics.Debug.WriteLine(storageFolder.Path);
            _file = await storageFolder.CreateFileAsync(MainPage.targetcap.DisplayName + ".csv", Windows.Storage.CreationCollisionOption.OpenIfExists);
            System.Diagnostics.Debug.WriteLine(_file.Path);

            //InMemoryRandomAccessStream stream = await Voice.Main("あいうえおかきくけこ");
            //MediaElement playbackMediaElement = new MediaElement();
            //playbackMediaElement.SetSource(stream, "wav");
            //playbackMediaElement.Play();
        }

        private async void setImage()
        {
            SoftwareBitmap outputBitmap = await ImageOperate.CnavasBitmapToSoftwareBitmapAsync(MainPage._currentFrame);
            SoftwareBitmap displayableImage = SoftwareBitmap.Convert(outputBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(displayableImage);
            image.ImageSource = source;
        }

        private void setSize()
        {
            var scale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var width = MainPage._currentFrame.Size.Width / scale;
            var height = MainPage._currentFrame.Size.Height / scale;
            bool result = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryResizeView(new Size { Width = width, Height = height + 30 });
            canvas1.Width = width;
            canvas1.Height = height;
            if (!result)
            {
                bool result_full = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            }
        }

        private PointerPoint init; //マウスクリック時の初期位置
        private bool isDrag; //ドラッグ判定
        private List<UIElement> canvasStock = new List<UIElement>(); //再描画する際に既存のパスを消す用の格納リスト

        /// <summary>
        /// パスを描画する
        /// </summary>
        /// <param name="p">マウスの現在地のポジション</param>
        private void DrawRectangle(PointerPoint p)
        {
            var canvas = canvas1 as Canvas;

            //既存のパスを削除
            foreach (UIElement ui in canvasStock)
            {
                canvas1.Children.Remove(ui);
            }

            //描画用パス
            var rect = new Windows.UI.Xaml.Shapes.Rectangle();
            double width;
            double height;

            rect.Stroke = new SolidColorBrush(Colors.Red);
            rect.StrokeThickness = 1;
            width = Math.Abs(init.Position.X - p.Position.X);
            height = Math.Abs(init.Position.Y - p.Position.Y);
            rect.Width = width;
            rect.Height = height;

            //マウスの位置により配置を変える
            if (init.Position.X < p.Position.X)
            {
                Canvas.SetLeft(rect, init.Position.X);
            }
            else
            {
                Canvas.SetLeft(rect, p.Position.X);
            }

            if (init.Position.Y < p.Position.Y)
            {
                Canvas.SetTop(rect, init.Position.Y);
            }
            else
            {
                Canvas.SetTop(rect, p.Position.Y);
            }

            //キャンバスの子と削除用に格納するのを忘れずに
            canvas1.Children.Add(rect);
            canvasStock.Add(rect);
        }

        /// <summary>
        /// canvas1上で左クリック時
        /// </summary>
        private void canvas1_MouseLeftButtonDown(object sender, PointerRoutedEventArgs e)
        {
            //既存のパスを削除
            foreach (UIElement ui in canvasStock)
            {
                canvas1.Children.Remove(ui);
            }
            Canvas c = sender as Canvas;
            init = e.GetCurrentPoint(c);
            //c.CaptureMouse();
            isDrag = true;
        }

        /// <summary>
        /// canvas1上で移動時
        /// </summary>
        private void canvas1_MouseMove(object sender, PointerRoutedEventArgs e)
        {
            if (isDrag)
            {
                var imap = e.GetCurrentPoint(canvas1); //キャンバス上のマウスの現在地
                DrawRectangle(imap); //再描画
            }
        }

        /// <summary>
        /// canvas1上で左クリック離した時
        /// </summary>
        private void canvas1_MouseUp(object sender, PointerRoutedEventArgs e)
        {
            if (isDrag)
            {
                Canvas c = sender as Canvas;
                isDrag = false;
                var imap = e.GetCurrentPoint(canvas1);
                MainPage.SetCoordinate(init.Position.X, imap.Position.X, init.Position.Y, imap.Position.Y, "charactor");
                //c.ReleaseMouseCapture();
            }
        }
    }
}
