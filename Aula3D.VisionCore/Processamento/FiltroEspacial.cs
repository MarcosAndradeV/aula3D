using OpenCvSharp;

namespace Aula3D.VisionCore.Processamento
{
    /// <summary>
    /// Etapa 1 do pipeline PDI: Responsável por limpar a imagem e isolar a mão.
    /// Sequência: Bilateral -> CLAHE -> HSV -> Canny -> Morfologia.
    /// </summary>
    public class FiltroEspacial : IDisposable
    {
        private readonly Mat _blurred;
        private readonly Mat _hsv;
        private readonly Mat _mask;
        private readonly Mat _kernel;

        public Mat MatFFT { get; private set; } = new Mat();
        public Mat MatCanny { get; private set; } = new Mat();

        // Faixa HSV calibrada para pele sob iluminação de laboratório.
        // Dupla 1: ajuste estes valores se o ambiente tiver luz diferente.
        private readonly Scalar _lowerBound;
        private readonly Scalar _upperBound;

        public FiltroEspacial()
        {
            _blurred = new Mat();
            _hsv     = new Mat();
            _mask    = new Mat();
            _kernel  = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));

            // H: 0-25 (laranja-amarelo da pele), S: 30-180, V: 60-255
            _lowerBound = new Scalar(0, 30, 60);
            _upperBound = new Scalar(25, 180, 255);
        }

        /// <summary>
        /// Aplica sequencialmente os filtros de PDI para isolar a mão do fundo.
        /// Retorna uma máscara binária pronta para a extração de contornos.
        /// </summary>
        public Mat Aplicar(Mat frameRoi)
        {
            // 1. Filtro Bilateral: Suaviza a pele (reduz detalhes) preservando a rigidez das bordas para o Canny.
            Cv2.BilateralFilter(frameRoi, _blurred, 9, 75, 75);

            // 2. Conversão BGR → HSV: Isolamento por matiz da pele independentemente de quão claro o ambiente esteja.
            Cv2.CvtColor(_blurred, _hsv, ColorConversionCodes.BGR2HSV);

            // 3. CLAHE (Equalização de Histograma Local): Aplicado no Canal V (Brilho) para salvar regiões afetadas por sombras de cômodo.
            Mat[] hsvChannels = Cv2.Split(_hsv);
            using var clahe = Cv2.CreateCLAHE(clipLimit: 3.0, tileGridSize: new Size(8, 8));
            clahe.Apply(hsvChannels[2], hsvChannels[2]);
            Cv2.Merge(hsvChannels, _hsv);
            foreach(var c in hsvChannels) c.Dispose();

            // 4. Limiarização (Thresholding): Extrai apenas a pele com base nos limiares HSV.
            Cv2.InRange(_hsv, _lowerBound, _upperBound, _mask);

            // 5. Morfologia: Abertura e Fechamento (remove pequenos ruídos granulares e preenche falhas e buracos no interior da mão).
            Cv2.MorphologyEx(_mask, _mask, MorphTypes.Open,  _kernel);
            Cv2.MorphologyEx(_mask, _mask, MorphTypes.Close, _kernel);
            
            // 6. Dilatação: Exagera levemente a massa do rastreamento final para preencher os dedos até as arestas reais.
            Cv2.Dilate(_mask, _mask, _kernel, iterations: 2);

            // 7. Distance Transform (Corte do Braço): Acha o ponto mais espesso (palma da mão) e remove o corpo da máscara para baixo.
            using Mat dist = new Mat();
            Cv2.DistanceTransform(_mask, dist, DistanceTypes.L2, DistanceTransformMasks.Mask5);
            Cv2.MinMaxLoc(dist, out _, out double maxVal, out _, out Point maxLoc);

            if (maxVal > 15) // Verifica se o centróide representa volume massivo (provável mão)
            {
                int limiteY = (int)Math.Min(_mask.Height, maxLoc.Y + maxVal * 1.0);
                using Mat limiteMask = Mat.Zeros(_mask.Size(), MatType.CV_8UC1);
                Cv2.Rectangle(limiteMask, new Point(0, 0), new Point(_mask.Width, limiteY), Scalar.White, -1);
                Cv2.BitwiseAnd(_mask, limiteMask, _mask);
            }

            // 8. Otimização Final com Frequência e Gradientes
            using Mat gray = new Mat();
            Cv2.CvtColor(frameRoi, gray, ColorConversionCodes.BGR2GRAY);
            
            // Filtro Frequencial (FFT 2D): Remoção drástica de alta frequência (ruídos periódicos da webcam barata).
            var fft = new FiltroFrequencial();
            using Mat grayFft = fft.AplicarPassaBaixa(gray);
            grayFft.CopyTo(MatFFT);

            // Filtro Bilateral sobre a Frequência limpa + Canny Edge para encontrar exatamente os limites externos sólidos.
            Cv2.BilateralFilter(grayFft, gray, 9, 75, 75);
            
            using Mat edges = new Mat();
            Cv2.Canny(gray, edges, 30, 100);
            edges.CopyTo(MatCanny);

            using Mat kernelCanny = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            Cv2.Dilate(edges, edges, kernelCanny, iterations: 2);
            Cv2.MorphologyEx(edges, edges, MorphTypes.Close, kernelCanny, iterations: 2);

            // Pinta internamente o polígono do Canny e colide com nossa casca de Cor via lógica AND.
            using Mat edgesFilled = edges.Clone();
            Cv2.FindContours(edgesFilled, out Point[][] cannyContours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            Cv2.DrawContours(edgesFilled, cannyContours, -1, Scalar.All(255), -1);

            Cv2.BitwiseAnd(_mask, edgesFilled, _mask);

            return _mask;
        }

        /// <summary>Retorna os contornos externos com área mínima de <paramref name="minArea"/>.</summary>
        public Point[][] ExtrairContornos(double minArea = 3000)
        {
            Cv2.FindContours(_mask, out Point[][] contours, out _,
                RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours == null || contours.Length == 0)
                return Array.Empty<Point[]>();

            return contours
                .Where(c => Cv2.ContourArea(c) > minArea)
                .OrderByDescending(c => Cv2.ContourArea(c))
                .Take(2)
                .ToArray();
        }

        /// <summary>Retorna a última máscara calculada (útil para janelas de debug).</summary>
        public Mat GetMask() => _mask;

        public void Dispose()
        {
            _blurred.Dispose();
            _hsv.Dispose();
            _mask.Dispose();
            _kernel.Dispose();
            MatFFT?.Dispose();
            MatCanny?.Dispose();
        }
    }
}
