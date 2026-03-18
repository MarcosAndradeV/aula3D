using OpenCvSharp;

namespace Aula3D.VisionCore.Processamento
{
    /// <summary>
    /// Etapa 2 do pipeline PDI: Extrai o momento geométrico da forma isolada.
    ///
    /// Necessita que o Array de contornos seja filtrado previamente (ex: pegando apenas
    /// o contorno de maior área na Etapa 1) para não calcular massa sobre ruídos de fundo.
    /// </summary>
    public static class ExtratorHu
    {
        /// <summary>
        /// Calcula o centro de massa e o bounding rect a partir de <paramref name="contour"/>.
        /// Preenche <see cref="HandTrackingResult.CenterOfMass"/> e <see cref="HandTrackingResult.BoundingRect"/>.
        /// </summary>
        public static void ExtrairGeometria(Point[] contour, HandTrackingResult resultado)
        {
            resultado.BoundingRect = Cv2.BoundingRect(contour);

            Moments m = Cv2.Moments(contour);
            if (m.M00 > 0)
            {
                resultado.CenterOfMass = new Point(
                    (int)(m.M10 / m.M00),
                    (int)(m.M01 / m.M00)
                );
            }
        }

        /// <summary>
        /// Calcula e retorna o vetor de 7 Momentos de Hu
        /// com escala logarítmica para o <paramref name="contour"/> fornecido.
        /// A transformação log normaliza a grande variação de escala dos momentos.
        /// </summary>
        public static double[] CalcularMomentosHu(Point[] contour)
        {
            var mom = Cv2.Moments(contour);
            double[] hu = mom.HuMoments();

            for (int i = 0; i < 7; i++)
            {
                hu[i] = -Math.Sign(hu[i]) * Math.Log10(Math.Abs(hu[i]) + 1e-10);
            }

            return hu;
        }
    }
}
