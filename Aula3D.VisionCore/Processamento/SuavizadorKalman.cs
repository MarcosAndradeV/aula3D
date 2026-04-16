using OpenCvSharp;
using System;

namespace Aula3D.VisionCore.Processamento
{
    public class SuavizadorKalman : IDisposable
    {
        private KalmanFilter _kalman;
        private Mat _measurement;
        private bool _isInitialized;

        public SuavizadorKalman()
        {
            _kalman = new KalmanFilter(4, 2, 0); 
            _measurement = new Mat(2, 1, MatType.CV_32F);

            _kalman.TransitionMatrix = new Mat(4, 4, MatType.CV_32F);
            _kalman.TransitionMatrix.SetTo(0);
            _kalman.TransitionMatrix.Set<float>(0, 0, 1.0f);
            _kalman.TransitionMatrix.Set<float>(0, 2, 1.0f);
            _kalman.TransitionMatrix.Set<float>(1, 1, 1.0f);
            _kalman.TransitionMatrix.Set<float>(1, 3, 1.0f);
            _kalman.TransitionMatrix.Set<float>(2, 2, 1.0f);
            _kalman.TransitionMatrix.Set<float>(3, 3, 1.0f);

            _kalman.MeasurementMatrix = new Mat(2, 4, MatType.CV_32F);
            _kalman.MeasurementMatrix.SetTo(0);
            _kalman.MeasurementMatrix.Set<float>(0, 0, 1.0f);
            _kalman.MeasurementMatrix.Set<float>(1, 1, 1.0f);

            _kalman.ProcessNoiseCov = new Mat(4, 4, MatType.CV_32F);
            Cv2.SetIdentity(_kalman.ProcessNoiseCov, Scalar.All(1e-4));

            _kalman.MeasurementNoiseCov = new Mat(2, 2, MatType.CV_32F);
            Cv2.SetIdentity(_kalman.MeasurementNoiseCov, Scalar.All(1e-1));

            _kalman.ErrorCovPost = new Mat(4, 4, MatType.CV_32F);
            Cv2.SetIdentity(_kalman.ErrorCovPost, Scalar.All(1));
            
            _isInitialized = false;
        }

        public Point2f ObterPredicao()
        {
            if (!_isInitialized) return new Point2f(0, 0);
            var prediction = _kalman.Predict();
            return new Point2f(prediction.At<float>(0), prediction.At<float>(1));
        }

        public Point2f Corrigir(float x, float y)
        {
            if (!_isInitialized)
            {
                _kalman.StatePre.Set<float>(0, x);
                _kalman.StatePre.Set<float>(1, y);
                _kalman.StatePre.Set<float>(2, 0);
                _kalman.StatePre.Set<float>(3, 0);
                
                _kalman.StatePost.Set<float>(0, x);
                _kalman.StatePost.Set<float>(1, y);
                _kalman.StatePost.Set<float>(2, 0);
                _kalman.StatePost.Set<float>(3, 0);
                
                _isInitialized = true;
            }

            _measurement.Set<float>(0, x);
            _measurement.Set<float>(1, y);

            var estimated = _kalman.Correct(_measurement);
            return new Point2f(estimated.At<float>(0), estimated.At<float>(1));
        }

        public void Dispose()
        {
            _kalman?.Dispose();
            _measurement?.Dispose();
        }
    }
}
