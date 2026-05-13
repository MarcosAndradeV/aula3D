using System.Collections.Generic;
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

    public class HandData
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Handedness { get; set; } = string.Empty;
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public List<Landmark> Landmarks { get; set; } = new();

        // Propriedades calculadas/auxiliares
        public bool IsOpen { get; set; }
        public bool IsPointing { get; set; }
    }

    public class Landmark
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class PontoRede
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
