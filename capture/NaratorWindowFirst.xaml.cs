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
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Input;
// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace capture
{
    /// <summary>
    /// </summary>
    public sealed partial class NaratorWindowFirst : Page
    {
        private CanvasBitmap Image { get; set; }
        Dictionary<uint, Windows.UI.Xaml.Input.Pointer> pointers;

        public NaratorWindowFirst()
        {
            this.InitializeComponent();
            setImage();

            // Initialize the dictionary.
            pointers = new Dictionary<uint, Windows.UI.Xaml.Input.Pointer>();

            // Declare the pointer event handlers.
            Target.PointerPressed +=
                new PointerEventHandler(Target_PointerPressed);
            //Target.PointerEntered +=
            //    new PointerEventHandler(Target_PointerEntered);
            Target.PointerReleased +=
                new PointerEventHandler(Target_PointerReleased);
            //Target.PointerExited +=
            //    new PointerEventHandler(Target_PointerExited);
            //Target.PointerCanceled +=
            //    new PointerEventHandler(Target_PointerCanceled);
            //Target.PointerCaptureLost +=
            //    new PointerEventHandler(Target_PointerCaptureLost);
            //Target.PointerMoved +=
            //    new PointerEventHandler(Target_PointerMoved);
            //Target.PointerWheelChanged +=
            //    new PointerEventHandler(Target_PointerWheelChanged);

            //buttonClear.Click +=
            //    new RoutedEventHandler(ButtonClear_Click);
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
            Target.Source = source;
        }

        private async Task ByteToWriteableBitmap(WriteableBitmap wb, byte[] bgra)
        {
            using (Stream stream = wb.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(bgra, 0, bgra.Length);
            }
        }

        /// <summary>
        /// The pointer pressed event handler.
        /// PointerPressed and PointerReleased don't always occur in pairs. 
        /// Your app should listen for and handle any event that can conclude 
        /// a pointer down (PointerExited, PointerCanceled, PointerCaptureLost).
        /// </summary>
        /// <param name="sender">Source of the pointer event.</param>
        /// <param name="e">Event args for the pointer routed event.</param>
        void Target_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Prevent most handlers along the event route from handling the same event again.
            e.Handled = true;

            PointerPoint ptrPt = e.GetCurrentPoint(Target);

            // Update event log.
            System.Diagnostics.Debug.WriteLine("Down: ");
            System.Diagnostics.Debug.WriteLine(ptrPt.Position.X);
            System.Diagnostics.Debug.WriteLine(ptrPt.Position.Y);

            // Lock the pointer to the target.
            Target.CapturePointer(e.Pointer);

            // Update event log.
            // System.Diagnostics.Debug.WriteLine("Pointer captured: " + ptrPt.Position.X, ptrPt.Position.Y);

            // Check if pointer exists in dictionary (ie, enter occurred prior to press).
            if (!pointers.ContainsKey(ptrPt.PointerId))
            {
                // Add contact to dictionary.
                pointers[ptrPt.PointerId] = e.Pointer;
            }
        }

        /// <summary>
        /// The pointer released event handler.
        /// PointerPressed and PointerReleased don't always occur in pairs. 
        /// Your app should listen for and handle any event that can conclude 
        /// a pointer down (PointerExited, PointerCanceled, PointerCaptureLost).
        /// </summary>
        /// <param name="sender">Source of the pointer event.</param>
        /// <param name="e">Event args for the pointer routed event.</param>
        void Target_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // Prevent most handlers along the event route from handling the same event again.
            e.Handled = true;

            PointerPoint ptrPt = e.GetCurrentPoint(Target);

            // Update event log.
            System.Diagnostics.Debug.WriteLine("Up: ");
            System.Diagnostics.Debug.WriteLine(ptrPt.Position.X);
            System.Diagnostics.Debug.WriteLine(ptrPt.Position.Y);

            // If event source is mouse or touchpad and the pointer is still 
            // over the target, retain pointer and pointer details.
            // Return without removing pointer from pointers dictionary.
            // For this example, we assume a maximum of one mouse pointer.
            if (ptrPt.PointerDevice.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                // Remove contact from dictionary.
                if (pointers.ContainsKey(ptrPt.PointerId))
                {
                    pointers[ptrPt.PointerId] = null;
                    pointers.Remove(ptrPt.PointerId);
                }

                // Release the pointer from the target.
                Target.ReleasePointerCapture(e.Pointer);

                // Update event log.
                // System.Diagnostics.Debug.WriteLine("Pointer released: " + ptrPt.Position.X, ptrPt.Position.Y);
            }
        }
    }
}
