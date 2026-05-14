using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace Backend.Core
{
    /// <summary>
    /// Maneja el enrutamiento de audio por aplicación utilizando llamadas
    /// a código no administrado (COM / IPolicyConfig) de Windows.
    /// </summary>
    public class AudioRouter
    {
        // GUIDs correspondientes a las interfaces no documentadas de Windows 10+
        [ComImport]
        [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
        private class AudioPolicyConfigClient
        {
        }

        [ComImport]
        [Guid("2A59116D-6C4F-4CB0-8D1C-4FA12B64EE53")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioPolicyConfigFactory
        {
            // Métodos del VTable que preceden a SetPersistedDefaultAudioEndpoint
            // Es vital mantener el orden exacto para evitar AccessViolation en COM
            int __VtblOffset00();
            int __VtblOffset01();
            int __VtblOffset02();
            int __VtblOffset03();
            int __VtblOffset04();
            int __VtblOffset05();
            int __VtblOffset06();
            int __VtblOffset07();
            
            [PreserveSig]
            int SetPersistedDefaultAudioEndpoint(uint processId, uint flow, uint role, [MarshalAs(UnmanagedType.LPWStr)] string deviceId);
            
            [PreserveSig]
            int GetPersistedDefaultAudioEndpoint(uint processId, uint flow, uint role, [Out, MarshalAs(UnmanagedType.LPWStr)] out string deviceId);
            
            [PreserveSig]
            int ClearAllPersistedApplicationDefaultEndpoints();
        }

        /// <summary>
        /// Cambia la salida de audio de un proceso específico hacia el ID de dispositivo dado.
        /// </summary>
        /// <param name="processId">PID del proceso (ej. PID de discord.exe)</param>
        /// <param name="deviceId">ID del Endpoint de Audio de Windows (ej. el que provee NAudio)</param>
        public void RouteAppAudio(uint processId, string deviceId)
        {
            IAudioPolicyConfigFactory? policyConfig = null;
            try
            {
                policyConfig = (IAudioPolicyConfigFactory)new AudioPolicyConfigClient();
                
                // Flow 0 = Render (Salida de audio)
                // Role 1 = Multimedia, Role 2 = Communications, Role 0 = Console
                
                // Forzamos el enrutamiento para los 3 roles principales por si la app discrimina
                int resultMultimedia = policyConfig.SetPersistedDefaultAudioEndpoint(processId, 0, 1, deviceId);
                int resultCommunications = policyConfig.SetPersistedDefaultAudioEndpoint(processId, 0, 2, deviceId);
                int resultConsole = policyConfig.SetPersistedDefaultAudioEndpoint(processId, 0, 0, deviceId);

                if (resultMultimedia != 0)
                {
                    Console.WriteLine($"[AudioRouter] Error COM al rutear PID {processId}. HRESULT: {resultMultimedia}");
                }
                else
                {
                    Console.WriteLine($"[AudioRouter] PID {processId} enrutado exitosamente al dispositivo {deviceId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AudioRouter] Excepción fatal P/Invoke: {ex.Message}");
            }
            finally
            {
                if (policyConfig != null)
                {
                    Marshal.ReleaseComObject(policyConfig);
                }
            }
        }

        /// <summary>
        /// Helper para obtener los dispositivos de salida disponibles (endpoints).
        /// </summary>
        public Dictionary<string, string> GetAvailableRenderDevices()
        {
            var devices = new Dictionary<string, string>();
            using var enumerator = new MMDeviceEnumerator();
            
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                devices[device.ID] = device.FriendlyName;
            }
            
            return devices;
        }
    }
}
