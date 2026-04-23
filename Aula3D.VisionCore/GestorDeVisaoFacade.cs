using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aula3D.VisionCore.Processamento;

namespace Aula3D.VisionCore
{
    public class GestorDeVisaoFacade : IDisposable
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }
        public bool HandDetected { get; private set; }
        public bool GestoDetectado { get; private set; } // true = Aberta, false = Fechada
        public bool IsRunning { get; private set; }
        public int CurrentFPS { get; private set; }

        private Task? _visionTask;
        private CancellationTokenSource? _cts;
        
        // Componentes de Rede e Processo
        private Process? _pythonProcess;
        private UdpClient? _udpClient;
        private const int PortaUDP = 5005;

        private SuavizadorKalman _kalman;

        public GestorDeVisaoFacade()
        {
            _kalman = new SuavizadorKalman();
        }

        public void Iniciar()
        {
            if (IsRunning) return;

            // 1. Inicia o Microserviço Python em background (Oculto)
            var startInfo = new ProcessStartInfo
            {
                FileName = "uv",
                Arguments = "run main.py",
                WorkingDirectory = System.IO.Path.GetFullPath("../Aula3D.TrackerService"),
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            try 
            {
                _pythonProcess = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao iniciar o microserviço Python: {ex.Message}");
            }

            // 2. Prepara o receptor UDP
            _udpClient = new UdpClient(PortaUDP);
            _cts = new CancellationTokenSource();
            
            IsRunning = true;
            _visionTask = Task.Run(() => LoopDeVisaoUDP(_cts.Token), _cts.Token);
        }

        public void Parar()
        {
            if (!IsRunning) return;

            _cts?.Cancel();
            
            // Encerra a porta de rede
            _udpClient?.Close();
            
            // Derruba o processo Python atrelado para não deixar processos zumbis
            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                _pythonProcess.Kill();
                _pythonProcess.Dispose();
            }

            _visionTask?.Wait();
            IsRunning = false;
        }

        private void LoopDeVisaoUDP(CancellationToken token)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, PortaUDP);
            var stopwatch = Stopwatch.StartNew();
            int frameCount = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Verifica se há pacotes na rede sem travar a thread
                    if (_udpClient!.Available > 0)
                    {
                        byte[] bytesRecebidos = _udpClient.Receive(ref endPoint);
                        string json = System.Text.Encoding.UTF8.GetString(bytesRecebidos);
                        
                        var pontosRede = JsonSerializer.Deserialize<List<PontoRede>>(json);

                        if (pontosRede != null && pontosRede.Count == 21)
                        {
                            ProcessarPontosDaRede(pontosRede);
                            frameCount++;
                            
                            // Calcula FPS de rede
                            if (stopwatch.ElapsedMilliseconds >= 1000)
                            {
                                CurrentFPS = frameCount;
                                frameCount = 0;
                                stopwatch.Restart();
                            }
                        }
                    }
                    else
                    {
                        // Evita uso de 100% da CPU enquanto aguarda o pacote
                        Thread.Sleep(2); 
                    }
                }
                catch (Exception)
                {
                    // Ignora erros de socket durante o encerramento
                    if (token.IsCancellationRequested) break;
                }
            }
        }

        private void ProcessarPontosDaRede(List<PontoRede> pontosRede)
        {
            // 1. Converter DTO de rede para o tipo geométrico do OpenCV
            Point[] pontosCv = pontosRede.Select(p => new Point(p.X, p.Y)).ToArray();

            // 2. A MÁSCARA SINTÉTICA (O elo entre MediaPipe e seu PDI)
            // Assumimos resolução padrão de webcam (640x480). O Godot ignora essa escala, usa apenas proporção.
            using Mat mascaraSintetica = Mat.Zeros(new Size(640, 480), MatType.CV_8UC1);

            // Cria um invólucro convexo (a silhueta externa da mão) usando os pontos
            Point[] convexHull = Cv2.ConvexHull(pontosCv);
            
            // Desenha a silhueta preenchida de branco sólido na imagem preta
            Cv2.FillConvexPoly(mascaraSintetica, convexHull, Scalar.White);

            // 3. Reconexão com o seu PDI clássico já existente
            var resultado = new HandTrackingResult { HandDetected = true, Contour = convexHull };
            
            ExtratorHu.ExtrairGeometria(convexHull, resultado);
            resultado.HuMoments = ExtratorHu.CalcularMomentosHu(convexHull);
            
            ClassificadorDeGestos.Classificar(convexHull, resultado);

            HandDetected = resultado.HandDetected;
            GestoDetectado = resultado.IsHandOpen;

            // 4. Suavização final para o Godot usando o seu Kalman
            if (resultado.CenterOfMass.X > 0)
            {
                var pontoSuavizado = _kalman.Corrigir(resultado.CenterOfMass.X, resultado.CenterOfMass.Y);
                X = pontoSuavizado.X;
                Y = pontoSuavizado.Y;

                // Calculo de profundidade nativo (Z) baseado na área do ConvexHull
                double area = Cv2.ContourArea(convexHull);
                Z = (float)(Math.Sqrt(area) / 100.0);
            }
            else
            {
                var predicaoCega = _kalman.ObterPredicao();
                X = predicaoCega.X;
                Y = predicaoCega.Y;
            }
        }

        public void Dispose()
        {
            Parar();
        }
    }
}