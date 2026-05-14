using System;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using Fleck;
using Backend.Core;
using Backend.Models;

namespace Backend.Network
{
    public class WSServer
    {
        private readonly AudioController _audioController;
        private readonly AudioRouter _audioRouter;
        private readonly Fleck.WebSocketServer _server;

        public WSServer(string location)
        {
            _audioController = new AudioController();
            _audioRouter = new AudioRouter();
            // Escucha en todas las interfaces de red para que el móvil pueda conectar
            _server = new Fleck.WebSocketServer(location);
        }

        public void Start()
        {
            _server.Start(socket =>
            {
                socket.OnOpen = () => 
                {
                    Console.WriteLine("[WebSocket] Cliente móvil conectado.");
                    
                    // Al conectar, enviamos los dispositivos disponibles (Handshake / Plug & Play)
                    var devicesDict = _audioRouter.GetAvailableRenderDevices();
                    var devicesArray = devicesDict.Select(d => new { id = d.Key, nombre = d.Value }).ToArray();
                    
                    var initPayload = new 
                    { 
                        comando = "init_devices", 
                        dispositivos = devicesArray 
                    };
                    
                    socket.Send(JsonSerializer.Serialize(initPayload));
                    Console.WriteLine("[WebSocket] Handshake 'init_devices' enviado al cliente.");
                };
                
                socket.OnClose = () => Console.WriteLine("[WebSocket] Cliente móvil desconectado.");
                
                socket.OnMessage = message => HandleMessage(message);
            });
            Console.WriteLine($"[WebSocket] Servidor escuchando en {_server.Location}");
        }

        private void HandleMessage(string message)
        {
            try
            {
                // Deserialización Polimórfica
                var command = JsonSerializer.Deserialize<BaseCommand>(message);

                if (command == null || string.IsNullOrWhiteSpace(command.App))
                {
                    Console.WriteLine("[WebSocket] Payload JSON ignorado: app no definida o comando nulo.");
                    return;
                }

                var appName = command.App.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);

                switch (command)
                {
                    case VolumeCommand volCmd:
                        _audioController.SetVolume(appName, volCmd.Vol);
                        break;

                    case RouteCommand routeCmd:
                        var processes = Process.GetProcessesByName(appName);
                        if (processes.Length > 0)
                        {
                            foreach (var proc in processes)
                            {
                                _audioRouter.RouteAppAudio((uint)proc.Id, routeCmd.DispositivoId);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[WebSocket] Ignorado: No hay procesos activos llamados '{appName}' para rutear.");
                        }
                        break;
                        
                    default:
                        Console.WriteLine("[WebSocket] Tipo de comando desconocido.");
                        break;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[WebSocket] Error de parseo JSON: {ex.Message} -> Payload: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebSocket] Excepción interna procesando mensaje: {ex.Message}");
            }
        }
    }
}
