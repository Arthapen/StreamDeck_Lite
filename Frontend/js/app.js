// =========================================================================
// StreamDeck Lite - Mobile Frontend Logic (Plug & Play)
// =========================================================================

// WS_URL Dinámico: Si hospedas el HTML en la PC, el celu entra a http://IP:PORT 
// y window.location.hostname resuelve mágicamente a la IP local de la PC.
const SERVER_PORT = 8181;
const WS_URL = `ws://${window.location.hostname}:${SERVER_PORT}`;

let socket = null;
let reconnectInterval = null;

// Cache de elementos UI
const statusEl = document.getElementById("connection-status");
const faders = document.querySelectorAll(".fader");

// (No cacheamos los routing-select globalmente porque ahora son dinámicos 
// y necesitamos engancharlos al DOM actual cuando inyectamos opciones)

/**
 * Inicializa y maneja la conexión WebSocket con reconexión automática.
 */
function connect() {
    if (socket && (socket.readyState === WebSocket.OPEN || socket.readyState === WebSocket.CONNECTING)) {
        return;
    }

    statusEl.textContent = "Conectando...";
    statusEl.className = "status disconnected";

    try {
        socket = new WebSocket(WS_URL);
    } catch (e) {
        console.error("Error creando WebSocket:", e);
        scheduleReconnect();
        return;
    }

    socket.onopen = () => {
        console.log("Conectado al servidor StreamDeck Lite.");
        statusEl.textContent = "Conectado";
        statusEl.className = "status connected";
        
        if (reconnectInterval) {
            clearInterval(reconnectInterval);
            reconnectInterval = null;
        }
    };

    socket.onmessage = (event) => {
        try {
            const data = JSON.parse(event.data);
            if (data.comando === "init_devices" && data.dispositivos) {
                console.log("Handshake recibido. Poblando dispositivos...", data.dispositivos);
                populateRoutingSelects(data.dispositivos);
            }
        } catch (err) {
            console.error("Error parseando mensaje entrante:", err);
        }
    };

    socket.onclose = () => {
        console.log("Desconectado del servidor.");
        statusEl.textContent = "Desconectado";
        statusEl.className = "status disconnected";
        scheduleReconnect();
    };

    socket.onerror = (err) => {
        console.error("WebSocket Error:", err);
        socket.close(); 
    };
}

function scheduleReconnect() {
    if (!reconnectInterval) {
        console.log("Intentando reconectar en 3 segundos...");
        reconnectInterval = setInterval(connect, 3000);
    }
}

// Iniciar la primera conexión
connect();

/**
 * Helper para mandar mensajes JSON al backend C#
 */
function sendCommand(payload) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify(payload));
    }
}

/**
 * Puebla dinámicamente los tags <select> de todos los canales 
 * basándose en el arreglo enviado por el Backend.
 */
function populateRoutingSelects(devices) {
    const selects = document.querySelectorAll(".routing-select");
    
    selects.forEach(select => {
        // Guardamos el valor seleccionado actual por si ocurre una reconexión
        const currentValue = select.value;
        
        // Limpiamos todo menos el por defecto (Salida del Sistema)
        select.innerHTML = '<option value="default">Salida del Sistema</option>';
        
        // Inyectamos las opciones de hardware reales
        devices.forEach(device => {
            const option = document.createElement("option");
            option.value = device.id;
            option.textContent = device.nombre;
            select.appendChild(option);
        });

        // Restaurar valor previo si existe en la nueva lista
        if (currentValue && currentValue !== "default") {
            const exists = devices.some(d => d.id === currentValue);
            if (exists) select.value = currentValue;
        }

        // Renovar el event listener para evitar duplicados en reconexiones
        select.removeEventListener("change", handleRouteChange);
        select.addEventListener("change", handleRouteChange);
    });
}

function handleRouteChange(e) {
    const appName = e.target.getAttribute("data-app");
    const deviceId = e.target.value;

    if (deviceId && deviceId !== "default") {
        sendCommand({
            comando: "cambiar_ruta",
            app: appName,
            dispositivo_id: deviceId
        });
    }
}

/**
 * Manejo de Sliders de Volumen (Zero Latency Feel)
 */
faders.forEach(fader => {
    fader.addEventListener("input", (e) => {
        const appName = e.target.getAttribute("data-app");
        const volumeValue = parseFloat(e.target.value);
        
        const readout = e.target.closest('.channel-card').querySelector('.volume-readout');
        readout.textContent = Math.round(volumeValue * 100) + "%";

        sendCommand({
            comando: "volumen",
            app: appName,
            vol: volumeValue
        });
    });
});
