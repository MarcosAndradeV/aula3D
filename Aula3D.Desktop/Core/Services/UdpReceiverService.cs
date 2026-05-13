using Aula3D.Desktop.Core.Interfaces;
using Aula3D.Desktop.Core.Models;
using Aula3D.VisionCore;

namespace Aula3D.Desktop.Core.Services;

public class UdpReceiverService : IUdpReceiverService, IDisposable
{
    private GestorDeVisaoFacade? _facade;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;

    public event Action<TrackingData>? OnDataReceived;

    public void StartListening(int port = 5005)
    {
        if (_facade != null) return;

        _facade = new GestorDeVisaoFacade();
        _facade.Iniciar(); // Inicia o script Python e o UdpClient nativo do projeto
        
        _cts = new CancellationTokenSource();
        _listenerTask = Task.Run(() => PollingLoop(_cts.Token), _cts.Token);
    }

    private async Task PollingLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_facade != null && _facade.LatestHands.Any())
                {
                    var data = new TrackingData
                    {
                        Hands = _facade.LatestHands.ToList()
                    };
                    OnDataReceived?.Invoke(data);
                }
                
                await Task.Delay(16, token); // ~60fps poll
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void StopListening()
    {
        _cts?.Cancel();
        _facade?.Parar();
        _facade?.Dispose();
        _facade = null;
    }

    public void Dispose()
    {
        StopListening();
        _cts?.Dispose();
    }
}
