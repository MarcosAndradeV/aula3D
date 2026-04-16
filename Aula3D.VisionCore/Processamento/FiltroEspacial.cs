using OpenCvSharp;
using System;
using System.Linq;

namespace Aula3D.VisionCore.Processamento
{
    public class FiltroEspacial : IDisposable
    {
        private Mat _hsv = new Mat();
        private Mat _mask = new Mat();
        private Mat _kernelOpen = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5));
        private Mat _kernelClose = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(15, 15));

        private Scalar _lowerBounds = new Scalar(0, 30, 60);
        private Scalar _upperBounds = new Scalar(20, 150, 255);

        public void Aplicar(Mat frame)
        {
            Cv2.CvtColor(frame, _hsv, ColorConversionCodes.BGR2HSV);
            Cv2.InRange(_hsv, _lowerBounds, _upperBounds, _mask);
            
            Cv2.MorphologyEx(_mask, _mask, MorphTypes.Open, _kernelOpen);
            Cv2.MorphologyEx(_mask, _mask, MorphTypes.Close, _kernelClose);
        }

        public Mat GetMask() => _mask;

        public Point[][] ExtrairContornos(double minArea)
        {
            Cv2.FindContours(_mask, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            return contours.Where(c => Cv2.ContourArea(c) >= minArea).ToArray();
        }

        public void Dispose()
        {
            _hsv?.Dispose();
            _mask?.Dispose();
            _kernelOpen?.Dispose();
            _kernelClose?.Dispose();
        }
    }
}
