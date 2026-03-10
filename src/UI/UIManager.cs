using Godot;
using System;

public partial class UIManager : CanvasLayer
{
    private HSlider _sliderX;
    private HSlider _sliderY;
    private HSlider _sliderZ;
    private CheckBox _checkX;
    private CheckBox _checkY;
    private CheckBox _checkZ;
    private Button _btnLoadLocal;

    // Distância máxima do clipping. Para objetos muito grandes, esse valor pode precisar ser ajustado via código dependendo da AABB do Modelo
    private float _maxClipDistance = 50.0f;

    [Signal]
    public delegate void OnLoadLocalRequestedEventHandler();

    [Signal]
    public delegate void OnAxisValuesChangedEventHandler(float x, float y, float z);

    public override void _Ready()
    {
        // Setup via código dos nós de UI, visando facilitar se montados via Editor.
        // Assumimos a existência destes nós na cena
        _sliderX = GetNodeOrNull<HSlider>("Panel/VBoxContainer/HBoxX/SliderX");
        _checkX = GetNodeOrNull<CheckBox>("Panel/VBoxContainer/HBoxX/CheckX");

        _sliderY = GetNodeOrNull<HSlider>("Panel/VBoxContainer/HBoxY/SliderY");
        _checkY = GetNodeOrNull<CheckBox>("Panel/VBoxContainer/HBoxY/CheckY");

        _sliderZ = GetNodeOrNull<HSlider>("Panel/VBoxContainer/HBoxZ/SliderZ");
        _checkZ = GetNodeOrNull<CheckBox>("Panel/VBoxContainer/HBoxZ/CheckZ");

        _btnLoadLocal = GetNodeOrNull<Button>("Panel/BtnLoadLocal");

        if (_sliderX != null) 
        {
            _sliderX.ValueChanged += (val) => UpdateClippingPlane("x", (float)val, _checkX.ButtonPressed);
            _sliderX.ValueChanged += (val) => EmitAxisValues();
        }
        if (_checkX != null) _checkX.Toggled += (pressed) => UpdateClippingPlane("x", (float)_sliderX.Value, pressed);

        if (_sliderY != null) 
        {
            _sliderY.ValueChanged += (val) => UpdateClippingPlane("y", (float)val, _checkY.ButtonPressed);
            _sliderY.ValueChanged += (val) => EmitAxisValues();
        }
        if (_checkY != null) _checkY.Toggled += (pressed) => UpdateClippingPlane("y", (float)_sliderY.Value, pressed);

        if (_sliderZ != null) 
        {
            _sliderZ.ValueChanged += (val) => UpdateClippingPlane("z", (float)val, _checkZ.ButtonPressed);
            _sliderZ.ValueChanged += (val) => EmitAxisValues();
        }
        if (_checkZ != null) _checkZ.Toggled += (pressed) => UpdateClippingPlane("z", (float)_sliderZ.Value, pressed);

        if (_btnLoadLocal != null)
        {
            _btnLoadLocal.Pressed += () => EmitSignal(SignalName.OnLoadLocalRequested);
        }

        // Setup Inicial dos Global Uniforms para desativado
        DisableAllClipping();
    }

    private static void DisableAllClipping()
    {
        // Valores muito altos para nao cortar nada no shader
        RenderingServer.GlobalShaderParameterSet("plane_distance_x", 10000.0f);
        RenderingServer.GlobalShaderParameterSet("plane_distance_y", 10000.0f);
        RenderingServer.GlobalShaderParameterSet("plane_distance_z", 10000.0f);
    }

    private void EmitAxisValues()
    {
        float x = _sliderX != null ? (float)_sliderX.Value : 0f;
        float y = _sliderY != null ? (float)_sliderY.Value : 0f;
        float z = _sliderZ != null ? (float)_sliderZ.Value : 0f;
        
        EmitSignal(SignalName.OnAxisValuesChanged, x, y, z);
    }

    private void UpdateClippingPlane(string axis, float sliderValue, bool isEnabled)
    {
        string paramName = $"plane_distance_{axis}";

        if (!isEnabled)
        {
            RenderingServer.GlobalShaderParameterSet(paramName, 10000.0f); // Desliga Corte
            return;
        }

        // Mapeia o Slider (0 a 100) para a Distância (-MaxDistance a +MaxDistance)
        // Isso permite o plano varrer do canto negativo ao positivo do modelo
        float normalized = sliderValue / 100.0f; // 0 a 1
        float actualDistance = Mathf.Lerp(-_maxClipDistance, _maxClipDistance, normalized);

        RenderingServer.GlobalShaderParameterSet(paramName, actualDistance);
    }
}
