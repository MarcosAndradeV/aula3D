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

        public GestorDeVisaoFacade()
        {
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
                                hand.Classify();
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

        public void Dispose()
        {
            Parar();
        }
    }
}
