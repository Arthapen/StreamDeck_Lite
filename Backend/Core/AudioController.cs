using System;
using System.Collections.Generic;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace Backend.Core
{
    public class AudioController
    {
        private readonly MMDevice _device;

        public AudioController()
        {
            var enumerator = new MMDeviceEnumerator();
            // Obtiene el dispositivo de reproducción por defecto
            _device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        /// <summary>
        /// Obtiene un diccionario con los nombres de los procesos y su volumen actual (0.0 a 1.0).
        /// </summary>
        public Dictionary<string, float> GetActiveSessions()
        {
            var activeSessions = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            var sessions = _device.AudioSessionManager.Sessions;

            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                var processId = (int)session.GetProcessID;
                
                // Ignoramos el proceso ID 0 (System) o procesos nulos
                if (processId == 0) continue;

                try
                {
                    var process = Process.GetProcessById(processId);
                    var name = process.ProcessName;
                    
                    // Solo incluimos sesiones que no están expiradas (puede ser Active o Inactive pero aún vivas)
                    if (session.State != AudioSessionState.AudioSessionStateExpired)
                    {
                        // Si hay varios hilos de la misma app, mantenemos el último reportado
                        activeSessions[name] = session.SimpleAudioVolume.Volume;
                    }
                }
                catch (Exception)
                {
                    // El proceso pudo haber terminado antes de poder leerlo, lo ignoramos
                }
            }

            return activeSessions;
        }

        /// <summary>
        /// Cambia el volumen de todas las sesiones de un proceso específico.
        /// </summary>
        /// <param name="processName">Nombre del proceso (ej. "spotify")</param>
        /// <param name="volume">Nivel de volumen entre 0.0 y 1.0</param>
        public void SetVolume(string processName, float volume)
        {
            // Validar límites
            volume = Math.Clamp(volume, 0.0f, 1.0f);
            
            var sessions = _device.AudioSessionManager.Sessions;
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                var processId = (int)session.GetProcessID;
                
                if (processId == 0) continue;

                try
                {
                    var process = Process.GetProcessById(processId);
                    if (process.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                    {
                        session.SimpleAudioVolume.Volume = volume;
                        Console.WriteLine($"[AudioController] Volumen de {processName} ajustado a {volume * 100}%");
                    }
                }
                catch (Exception)
                {
                    // Proceso inaccesible o terminado
                }
            }
        }
    }
}
