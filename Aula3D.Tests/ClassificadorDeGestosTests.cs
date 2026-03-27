using Xunit;
using Aula3D.VisionCore.Processamento;
using System.IO;

namespace Aula3D.Tests
{
    public class ClassificadorDeGestosTests
    {
        private const string JsonPath = "gestures.json";

        public ClassificadorDeGestosTests()
        {
            // Limpa o estado antes de cada teste
            if (File.Exists(JsonPath)) File.Delete(JsonPath);
        }

        [Fact]
        public void DeveSalvarEReconhecerAssinaturaComSucesso()
        {
            // Arrange: Simulando os momentos de Hu reais gerados da mão aberta
            double[] momentosIniciais = { 0.519, 3.156, 3.073, 2.556, -5.909, 4.405, -5.391 };
            // Pequena variação simulando outro frame da câmera (mesma iluminação)
            double[] momentosSimilares = { 0.520, 3.150, 3.070, 2.558, -5.900, 4.400, -5.399 }; 
            string nomeGesto = "ABERTA";

            // Act: Salva primeiro, criando localmente na build do teste o gestures.json
            ClassificadorDeGestos.SalvarAssinatura(nomeGesto, momentosIniciais);
            
            // Tenta reconhecer a mão com os novos valores baseados no json
            string? reconhecido = ClassificadorDeGestos.ReconhecerPorAssinatura(momentosSimilares);

            // Assert
            Assert.True(File.Exists(JsonPath));
            Assert.Equal(nomeGesto, reconhecido);
        }
    }
}
