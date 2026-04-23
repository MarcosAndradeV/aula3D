using Aula3D.VisionCore;
using System;

var facade = new GestorDeVisaoFacade();
facade.Iniciar();

Console.WriteLine("Cérebro PDI iniciado. Aguardando o olho do MediaPipe... Pressione ESC para sair.\n");

while (true)
{
    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
        break;

    Console.Write($"\rX: {facade.X:F2} | Y: {facade.Y:F2} | Z: {facade.Z:F2} | Aberta? {facade.GestoDetectado} | FPS UDP: {facade.CurrentFPS}    ");
    System.Threading.Thread.Sleep(50);
}

facade.Parar();