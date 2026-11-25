window.telemetryMap = (() => {
    let map = null;
    let connection = null;
    let fittedInitialBounds = false;
    const markers = new Map();
    const trails = new Map();

    function getStatusColor(telemetry) {
        if (!telemetry.ignitionOn) return "#64748b";
        return telemetry.speedKmh > 0 ? "#22c55e" : "#f59e0b";
    }

    function buildPopupHtml(telemetry) {
        const ignitionText = telemetry.ignitionOn ? "Ligada" : "Desligada";
        return `
            <div class="map-popup">
                <strong>${telemetry.deviceId}</strong><br/>
                Velocidade: ${telemetry.speedKmh.toFixed(1)} km/h<br/>
                Ignição: ${ignitionText}<br/>
                Bateria: ${telemetry.batteryPercent}%<br/>
                Odômetro: ${telemetry.odometerKm.toFixed(2)} km<br/>
                Seq: #${telemetry.sequenceNumber}
            </div>
        `;
    }

    function upsertDevice(state) {
        if (!map || !state || !state.latest) return;

        const t = state.latest;
        const latLng = [t.latitude, t.longitude];
        const color = getStatusColor(t);

        let marker = markers.get(t.deviceId);
        if (!marker) {
            marker = L.circleMarker(latLng, {
                radius: 8,
                color: "#ffffff",
                weight: 2,
                fillColor: color,
                fillOpacity: 0.95
            }).addTo(map);
            marker.bindPopup(buildPopupHtml(t));
            markers.set(t.deviceId, marker);
        } else {
            marker.setLatLng(latLng);
            marker.setStyle({ fillColor: color });
            marker.setPopupContent(buildPopupHtml(t));
        }

        if (Array.isArray(state.trail) && state.trail.length > 1) {
            const path = state.trail.map(p => [p.latitude, p.longitude]);
            let polyline = trails.get(t.deviceId);
            if (!polyline) {
                polyline = L.polyline(path, {
                    color: color,
                    weight: 3,
                    opacity: 0.65
                }).addTo(map);
                trails.set(t.deviceId, polyline);
            } else {
                polyline.setLatLngs(path);
                polyline.setStyle({ color: color });
            }
        }
    }

    async function loadInitialSnapshot() {
        try {
            const response = await fetch("/api/telemetry");
            if (!response.ok) return;
            const snapshot = await response.json();
            snapshot.forEach(upsertDevice);

            if (!fittedInitialBounds && snapshot.length > 0) {
                const group = L.featureGroup(Array.from(markers.values()));
                map.fitBounds(group.getBounds().pad(0.2), { maxZoom: 15 });
                fittedInitialBounds = true;
            }
        } catch (err) {
            console.warn("Falha ao carregar snapshot inicial:", err);
        }
    }

    async function startSignalR() {
        if (connection) return;

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/telemetry")
            .withAutomaticReconnect()
            .build();

        connection.on("TelemetryUpdated", (state) => {
            upsertDevice(state);
            if (!fittedInitialBounds && markers.size > 0) {
                const first = markers.values().next().value;
                map.setView(first.getLatLng(), 14);
                fittedInitialBounds = true;
            }
        });

        try {
            await connection.start();
        } catch (err) {
            console.error("Erro ao conectar ao SignalR:", err);
        }
    }

    async function init(elementId) {
        if (map) return;

        map = L.map(elementId).setView([-23.55052, -46.633308], 13);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: "&copy; OpenStreetMap contributors",
            maxZoom: 19
        }).addTo(map);

        await loadInitialSnapshot();
        await startSignalR();
    }

    function focusDevice(deviceId) {
        if (!map) return;
        const marker = markers.get(deviceId);
        if (!marker) return;

        map.setView(marker.getLatLng(), Math.max(map.getZoom(), 15), { animate: true });
        marker.openPopup();
    }

    return {
        init,
        focusDevice
    };
})();
