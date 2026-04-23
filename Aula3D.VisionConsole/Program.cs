using Aula3D.VisionCore;
using System.Text.Json;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;

Console.WriteLine("Iniciando injeção falsa de rede...");

_ = Task.Run(async () => {
    using var udpSender = new UdpClient();
    var endpoint = new IPEndPoint(IPAddress.Loopback, 5005);

    string mockJson = @"[
        {""X"": 200, ""Y"": 200}, {""X"": 300, ""Y"": 200}, 
        {""X"": 300, ""Y"": 300}, {""X"": 200, ""Y"": 300},
        {""X"": 250, ""Y"": 250}, {""X"": 250, ""Y"": 250}, {""X"": 250, ""Y"": 250}, 
        {""X"": 250, ""Y"": 250}, {""X"": 250, ""Y"": 250}, {""X"": 250, ""Y"": 250},
        {""X"": 250, ""Y"": 250}, {""X"": 250, ""Y"": 250}, {""X"": 250, ""Y"": 250},
        {""X"": 250, ""Y"": 250}, {""X"": 250, ""Y"": 250}, {""X"": 250, ""Y"": 250},
        {""X"": 250, ""Y"": 250}, {""X"": 250, ""Y"": 250}, {""X"": 250, ""Y"": 250},
        {""X"": 250, ""Y"": 250}, {""X"": 250, ""Y"": 250}
    ]";
    byte[] bytes = Encoding.UTF8.GetBytes(mockJson);

    while (true)
    {
        udpSender.Send(bytes, bytes.Length, endpoint);
        await Task.Delay(33); 
    }
});

var facade = new GestorDeVisaoFacade();
facade.Iniciar();

Console.WriteLine("Escutando... Pressione CTRL+C para sair.\n");

while (true)
{
    Console.Write($"\rX: {facade.X:F2} | Y: {facade.Y:F2} | Z: {facade.Z:F2} | Aberta? {facade.GestoDetectado} | FPS UDP: {facade.CurrentFPS}    ");
    System.Threading.Thread.Sleep(100);
}