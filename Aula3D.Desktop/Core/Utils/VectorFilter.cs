namespace Aula3D.Desktop.Core.Utils;

public class VectorFilter
{
    private float _currentX;
    private float _currentY;
    private float _currentZ;

    private readonly float _lerpFactor;

    public VectorFilter(float lerpFactor = 0.1f)
    {
        _lerpFactor = lerpFactor;
    }

    public (float X, float Y, float Z) ApplyLerp(float targetX, float targetY, float targetZ)
    {
        _currentX = Lerp(_currentX, targetX, _lerpFactor);
        _currentY = Lerp(_currentY, targetY, _lerpFactor);
        _currentZ = Lerp(_currentZ, targetZ, _lerpFactor);

        return (_currentX, _currentY, _currentZ);
    }

    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    public void Reset()
    {
        _currentX = 0f;
        _currentY = 0f;
        _currentZ = 0f;
    }
}
