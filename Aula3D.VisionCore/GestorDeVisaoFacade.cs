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
using Aula3D.VisionCore.Utils;

using Aula3D.VisionCore.Interfaces;

namespace Aula3D.VisionCore
{
    public class GestorDeVisaoFacade : IGestureProvider, IDisposable
    {
        public List<HandData> LatestHands { get; private set; } = new();
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
                WorkingDirectory = PathResolver.ObterCaminhoTrackerService(),
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

                        var hands = JsonSerializer.Deserialize<List<HandData>>(json);

                        if (hands != null)
                        {
                            foreach (var hand in hands)
                            {
                                ClassificarGesto(hand);
                            }
                            
                            LatestHands = hands;
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

        private void ClassificarGesto(HandData hand)
        {
            if (hand.Landmarks == null || hand.Landmarks.Count < 21) return;

            // Verifica se os dedos estão esticados (a ponta está mais longe do pulso que a junta do meio)
            bool[] extended = new bool[4]; // Index, Middle, Ring, Pinky
            
            int[][] fingerIndices = new int[][] { 
                new int[] {8, 6},   // Index Tip vs Index PIP
                new int[] {12, 10}, // Middle Tip vs Middle PIP
                new int[] {16, 14}, // Ring Tip vs Ring PIP
                new int[] {20, 18}  // Pinky Tip vs Pinky PIP
            };

            var wrist = hand.Landmarks[0];
            int extendedFingers = 0;
            for (int i = 0; i < fingerIndices.Length; i++)
            {
                var tip = hand.Landmarks[fingerIndices[i][0]];
                var pip = hand.Landmarks[fingerIndices[i][1]];
                
                double distTip = Math.Sqrt(Math.Pow(tip.X - wrist.X, 2) + Math.Pow(tip.Y - wrist.Y, 2));
                double distPip = Math.Sqrt(Math.Pow(pip.X - wrist.X, 2) + Math.Pow(pip.Y - wrist.Y, 2));
                
                if (distTip > distPip)
                {
                    extended[i] = true;
                    extendedFingers++;
                }
            }

            // Se pelo menos 2 dedos estiverem esticados, consideramos "Aberto"
            hand.IsOpen = extendedFingers >= 2;

            // Gesto de "Apontar": Apenas o indicador esticado
            hand.IsPointing = extended[0] && !extended[1] && !extended[2] && !extended[3];
        }

        public void Dispose()
        {
            Parar();
        }
    }
}
