﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;

namespace FlowBoard.Services
{
    class CanvasSizeService
    {
        private static double originalX;
        private static double originalY;
        private static double maxX = 0.0;
        private static double maxY = 0.0;
        private static bool flag = true;

        public static InkCanvas inkCanvas { get; set; }

        public static void Initialize(InkCanvas _inkCanvas)
        {
            inkCanvas = _inkCanvas;
         //   inkCanvas.InkPresenter.StrokeInput.StrokeEnded += adjustInkCanvasSize;
           // inkCanvas.Height = 10000;
           // inkCanvas.Width = 10000;
          //  inkCanvas.InkPresenter.StrokesErased += InkPresenter_StrokesErased;
        }

        private static async void InkPresenter_StrokesErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            await Task.Delay(100);

            //The coordinate of the lower right corner of the erased ink stoke
            var erasedInkX = args.Strokes.ElementAt(0).BoundingRect.Bottom;
            var erasedInkY = args.Strokes.ElementAt(0).BoundingRect.Right;

            var XBound = inkCanvas.InkPresenter.StrokeContainer.BoundingRect.Bottom;
            if (erasedInkX >= maxX && XBound < inkCanvas.ActualHeight + 100)
            {
                if (XBound - 100 > originalX)
                    inkCanvas.Height = XBound - 100;
                else
                    inkCanvas.Height = originalX;  //The size of InkCanvas shrinks to the original size.

                maxX = inkCanvas.Height;
            }

            var YBound = inkCanvas.InkPresenter.StrokeContainer.BoundingRect.Right;
            if (erasedInkY >= maxY && YBound < inkCanvas.ActualWidth + 100)
            {
                if (YBound - 100 > originalY)
                {
                    inkCanvas.Width = YBound - 100;
                }
                else
                    inkCanvas.Width = originalY;

                maxY = inkCanvas.Width;
            }
        }

        private async static void adjustInkCanvasSize(InkStrokeInput sender, PointerEventArgs args)
        {
            await Task.Delay(100);

            if (flag)
            {
                flag = false;
                originalX = inkCanvas.ActualHeight;  //Get the original size 
                originalY = inkCanvas.ActualWidth;
            }
            var XBound = inkCanvas.InkPresenter.StrokeContainer.BoundingRect.Bottom;
            if (XBound > maxX)
                maxX = XBound;  //maxX and maxY always hold the maximum size of StrokeContainer

            if (XBound > inkCanvas.ActualHeight - 1000)
                inkCanvas.Height = XBound + 1000;

            var YBound = inkCanvas.InkPresenter.StrokeContainer.BoundingRect.Right;
            if (YBound > maxY)
                maxY = YBound;

            if (YBound > inkCanvas.ActualWidth - 1000)
                inkCanvas.Width = YBound + 1000;
        }
    }
}
