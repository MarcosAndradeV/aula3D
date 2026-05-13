using Aula3D.VisionCore;
using System;

var facade = new GestorDeVisaoFacade();
facade.Iniciar();

Console.WriteLine("Cérebro PDI iniciado. Aguardando o olho do MediaPipe... Pressione ESC para sair.\n");

while (true)
{
    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
        break;

    if (facade.LatestHands.Count > 0)
    {
        var h = facade.LatestHands[0];
        Console.Write($"\rMãos: {facade.LatestHands.Count} | X: {h.CenterX:F2} | Y: {h.CenterY:F2} | Aberta? {h.IsOpen} | FPS UDP: {facade.CurrentFPS}    ");
    }
    else
    {
        Console.Write($"\rNenhuma mão detectada. FPS UDP: {facade.CurrentFPS}                                ");
    }
    System.Threading.Thread.Sleep(50);
}

facade.Parar();