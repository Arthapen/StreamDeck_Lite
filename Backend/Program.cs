using System;
using Backend.Network;
using Backend.Core;

Console.WriteLine("=== StreamDeck Lite Backend (Fase 2) ===");
Console.WriteLine("Iniciando servicios...");

try
{
    // Escucha en todas las interfaces, importante para que el móvil
    // en la misma red Wi-Fi pueda conectarse a la IP de esta PC.
    var server = new WSServer("ws://0.0.0.0:8181");
    server.Start();

    Console.WriteLine("\nEl servidor está listo y esperando conexiones locales (Puerto 8181).");
    Console.WriteLine("\nDispositivos de Salida de Audio detectados (Endpoints):");
    
    // Mostramos los IDs de los dispositivos para que el usuario pueda armar 
    // su JSON de ruteo fácilmente o verifiquemos que la API funciona.
    var router = new AudioRouter();
    var devices = router.GetAvailableRenderDevices();
    
    if (devices.Count == 0)
    {
        Console.WriteLine(" - No se detectaron dispositivos de audio activos.");
    }
    else
    {
        foreach(var device in devices)
        {
            Console.WriteLine($" - [{device.Key}] : {device.Value}");
        }
    }

    Console.WriteLine("\nPresiona 'Q' para detener el servidor y salir...");
    while (Console.ReadKey(true).Key != ConsoleKey.Q)
    {
        // Bloqueo simple hasta que el usuario decida salir
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error crítico en el loop principal: {ex.Message}");
}
