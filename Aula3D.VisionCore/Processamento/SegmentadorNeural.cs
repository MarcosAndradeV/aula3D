using System;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace Aula3D.VisionCore.Processamento
{
    public class SegmentadorNeural : IDisposable
    {
        private InferenceSession _session;

        public SegmentadorNeural(string modelPath)
        {
            var options = new SessionOptions();
            _session = new InferenceSession(modelPath, options);
        }

        public Mat ObterMascara(Mat frameRoi)
        {
            int targetWidth = 224;
            int targetHeight = 224;

            using Mat resized = new Mat();
            Cv2.Resize(frameRoi, resized, new Size(targetWidth, targetHeight));

            using Mat rgb = new Mat();
            Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

            var tensor = new DenseTensor<float>(new[] { 1, 3, targetHeight, targetWidth });

            unsafe
            {
                byte* ptr = rgb.DataPointer;
                int channels = rgb.Channels();
                long step = rgb.Step();

                for (int y = 0; y < targetHeight; y++)
                {
                    for (int x = 0; x < targetWidth; x++)
                    {
                        int offset = (int)(y * step + x * channels);
                        tensor[0, 0, y, x] = ptr[offset + 0] / 255.0f; // R
                        tensor[0, 1, y, x] = ptr[offset + 1] / 255.0f; // G
                        tensor[0, 2, y, x] = ptr[offset + 2] / 255.0f; // B
                    }
                }
            }

            var inputs = new[] { NamedOnnxValue.CreateFromTensor(_session.InputMetadata.Keys.First(), tensor) };
            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();
            var span = output.Span;

            Mat mask = new Mat(targetHeight, targetWidth, MatType.CV_8UC1);

            unsafe
            {
                byte* outPtr = mask.DataPointer;
                int count = targetWidth * targetHeight;
                for (int i = 0; i < count; i++)
                {
                    outPtr[i] = span[i] > 0.5f ? (byte)255 : (byte)0;
                }
            }

            Mat finalMask = new Mat();
            Cv2.Resize(mask, finalMask, new Size(frameRoi.Width, frameRoi.Height), 0, 0, InterpolationFlags.Nearest);
            
            mask.Dispose();

            return finalMask;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
