﻿using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace capture
{
    class ImageOperate
    {
        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }
        unsafe public static byte[] GetCroppedBitmap(SoftwareBitmap inputBitmap, uint startPointX, uint startPointY, uint width, uint height)
        {
            int bigger;
            if (width >= height)
            {
                bigger = (int)width;
            }
            else
            {
                bigger = (int)height;
            }
            // なぜか0が入れられることがある？けど無視したら動いてるからヨシ！
            if (bigger == 0)
            {
                return new byte[0];
            }
            SoftwareBitmap softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, bigger, bigger, BitmapAlphaMode.Premultiplied);

            using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
            using (BitmapBuffer inputBuffer = inputBitmap.LockBuffer(BitmapBufferAccessMode.Write))
            {
                using (var reference = buffer.CreateReference())
                using (var inputReference = inputBuffer.CreateReference())
                {
                    byte* dataInBytes;
                    uint capacity;
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);


                    byte* inputDataInBytes;
                    uint inputCapacity;
                    ((IMemoryBufferByteAccess)inputReference).GetBuffer(out inputDataInBytes, out inputCapacity);

                    BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
                    BitmapPlaneDescription inputBufferLayout = inputBuffer.GetPlaneDescription(0);
                    int h = bufferLayout.Height;
                    for (int i = 0; i < bufferLayout.Height; i++)
                    {
                        for (int j = 0; j < bufferLayout.Width; j++)
                        {
                            byte valueB;
                            byte valueG;
                            byte valueR;
                            byte valueA;
                            if ((h - height) / 2 < i && (h + height) / 2 > i)
                            {
                                valueB = inputDataInBytes[inputBufferLayout.StartIndex + inputBufferLayout.Stride * (startPointY + i - (h - height) / 2) + 4 * (startPointX + j) + 0];
                                valueG = inputDataInBytes[inputBufferLayout.StartIndex + inputBufferLayout.Stride * (startPointY + i - (h - height) / 2) + 4 * (startPointX + j) + 1];
                                valueR = inputDataInBytes[inputBufferLayout.StartIndex + inputBufferLayout.Stride * (startPointY + i - (h - height) / 2) + 4 * (startPointX + j) + 2];
                                valueA = inputDataInBytes[inputBufferLayout.StartIndex + inputBufferLayout.Stride * (startPointY + i - (h - height) / 2) + 4 * (startPointX + j) + 3];
                                if (((double)valueR * 0.2126 + (double)valueG * 0.7152 + (double)valueB * 0.0772) / 255 > 0.5)
                                {
                                    valueB = (byte)255;
                                    valueG = (byte)255;
                                    valueR = (byte)255;
                                    valueA = 0;
                                }
                                else
                                {
                                    valueB = 0;
                                    valueG = 0;
                                    valueR = 0;
                                    valueA = 0;
                                }
                            }
                            else
                            {
                                valueB = 0;
                                valueG = 0;
                                valueR = 0;
                                valueA = 0;
                            }
                            dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] = valueB;
                            dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = valueG;
                            dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = valueR;
                            dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = valueA;
                        }
                    }
                    byte[] data = new byte[capacity];
                    Marshal.Copy((IntPtr)dataInBytes, data, 0, (int)capacity);
                    return data;
                }
            }
        }

        public static async Task<SoftwareBitmap> CnavasBitmapToSoftwareBitmapAsync(CanvasBitmap canvasBitmap)
        {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                await stream.WriteAsync(canvasBitmap.GetPixelBytes().AsBuffer());
                stream.Seek(0);

                SoftwareBitmap outputBitmap =
                    SoftwareBitmap.CreateCopyFromBuffer(
                        canvasBitmap.GetPixelBytes().AsBuffer(),
                        BitmapPixelFormat.Bgra8,
                        (int)canvasBitmap.Size.Width,
                        (int)canvasBitmap.Size.Height
                    );
                return outputBitmap;
            }
        }
    }
}
