using OpenCvSharp;

namespace Aula3D.VisionCore
{
    public class HandTrackingResult
    {
        public bool HandDetected { get; set; }
        public bool IsHandOpen { get; set; }
        public string? State { get; set; }
        public Point CenterOfMass { get; set; }
        public Rect BoundingRect { get; set; }
        public Point[]? Contour { get; set; }
        public Point[]? DefectPoints { get; set; }
        public double[]? HuMoments { get; set; }
    }

    public class PontoRede
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
