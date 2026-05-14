using Aula3D.VisionCore;

namespace Aula3D.Desktop.Core.Services;

/// <summary>
/// Processa dados brutos de rastreamento e os converte em transformações 3D aplicáveis.
/// </summary>
public class InteractionProcessor
{
    // Configurações Técnicas
    private const float PAN_SENSITIVITY = 15.0f;
    private const float ROT_SENSITIVITY = 4.0f;
    private const float PAN_LIMIT = 3.0f;
    private const float SAFE_BOUNDARY = 0.85f;

    // Estado Atual
    public float PanX { get; private set; }
    public float PanY { get; private set; }
    public float Theta { get; private set; }
    public float Phi { get; private set; } = (float)Math.PI / 2.0f;
    public float Scale { get; private set; } = 1.0f;

    // Cache de Rastreamento
    private float? _lastX, _lastY, _lastDist;
    private string _lastState = "";

    public (bool updated, string mode) Process(List<HandData> hands)
    {
        if (hands.Count == 0)
        {
            ResetTracking();
            return (false, "Nenhum");
        }

        if (hands.Count == 2) return ProcessBimanual(hands[0], hands[1]);
        return ProcessUnimanual(hands[0]);
    }

    private (bool, string) ProcessUnimanual(HandData hand)
    {
        float normX = (hand.CenterX - 320f) / 320f;
        float normY = (hand.CenterY - 240f) / 240f;

        // Bloqueio de saída: Congela o modelo se a mão estiver na borda
        if (Math.Abs(normX) > SAFE_BOUNDARY || Math.Abs(normY) > SAFE_BOUNDARY)
        {
            ResetTracking();
            return (false, "Zona de Segurança");
        }

        string state = hand.IsOpen ? "Open" : "Closed";
        if (_lastState != state) ResetTracking();

        bool updated = false;
        if (_lastX.HasValue && _lastY.HasValue)
        {
            float dx = normX - _lastX.Value;
            float dy = normY - _lastY.Value;

            if (hand.IsOpen)
            {
                // Rotação: Mantido para acompanhar a direção da mão
                Theta -= dx * ROT_SENSITIVITY;
                Phi = Math.Clamp(Phi - dy * ROT_SENSITIVITY, 0.1f, (float)Math.PI - 0.1f);
                updated = true;
            }
            else
            {
                // Pan: Sinais corrigidos (- dx e + dy) para o modelo rastrear a palma da mão
                PanX = Math.Clamp(PanX - dx * PAN_SENSITIVITY, -PAN_LIMIT, PAN_LIMIT);
                PanY = Math.Clamp(PanY + dy * PAN_SENSITIVITY, -PAN_LIMIT, PAN_LIMIT);
                updated = true;
            }
        }

        _lastX = normX; _lastY = normY; _lastState = state;
        return (updated, hand.IsOpen ? "Rotação" : "Pan");
    }

    private (bool, string) ProcessBimanual(HandData h1, HandData h2)
    {
        if (!h1.IsOpen || !h2.IsOpen) return (false, "Aguardando Gesto");

        float dist = (float)Math.Sqrt(Math.Pow(h1.CenterX - h2.CenterX, 2) + Math.Pow(h1.CenterY - h2.CenterY, 2));
        bool updated = false;

        if (_lastDist.HasValue)
        {
            float delta = (dist - _lastDist.Value) / 200f;
            Scale = Math.Clamp(Scale + delta, 0.1f, 5.0f);
            updated = true;
        }

        _lastDist = dist;
        return (updated, "Zoom");
    }

    private void ResetTracking()
    {
        _lastX = _lastY = _lastDist = null;
        _lastState = "";
    }
}