// Leaflet + OpenStreetMap JS interop for Deal Map Dashboard
window.leafletMapInterop = {
    _map: null,
    _markers: [],

    initMap: function (elementId, pins, dotNetRef) {
        const mapElement = document.getElementById(elementId);
        if (!mapElement) return;

        // Clean up previous instance
        if (this._map) {
            this._map.remove();
            this._map = null;
            this._markers = [];
        }

        this._map = L.map(elementId).setView([39.8283, -98.5795], 4);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        }).addTo(this._map);

        if (!pins || pins.length === 0) return;

        const bounds = L.latLngBounds();

        pins.forEach(pin => {
            const marker = L.marker([pin.latitude, pin.longitude])
                .addTo(this._map);

            const capRate = pin.capRate != null ? (pin.capRate * 100).toFixed(2) + '%' : 'N/A';
            const irr = pin.irr != null ? (pin.irr * 100).toFixed(2) + '%' : 'N/A';
            const price = pin.purchasePrice
                ? '$' + pin.purchasePrice.toLocaleString('en-US', { maximumFractionDigits: 0 })
                : 'N/A';

            const content = `
                <div style="font-family: 'Plus Jakarta Sans', sans-serif; min-width: 200px;">
                    <h3 style="margin: 0 0 8px; font-size: 16px; color: #1a1a2e;">${pin.propertyName}</h3>
                    <p style="margin: 0 0 4px; font-size: 13px; color: #666;">${pin.address}</p>
                    <div style="margin-top: 8px; font-size: 13px;">
                        <span style="display: inline-block; padding: 2px 8px; border-radius: 4px; background: ${this._statusColor(pin.status)}; color: white; font-size: 11px;">${pin.status}</span>
                    </div>
                    <table style="margin-top: 8px; font-size: 13px; border-collapse: collapse;">
                        <tr><td style="padding: 2px 12px 2px 0; color: #888;">Units</td><td>${pin.unitCount}</td></tr>
                        <tr><td style="padding: 2px 12px 2px 0; color: #888;">Price</td><td>${price}</td></tr>
                        <tr><td style="padding: 2px 12px 2px 0; color: #888;">Cap Rate</td><td>${capRate}</td></tr>
                        <tr><td style="padding: 2px 12px 2px 0; color: #888;">IRR</td><td>${irr}</td></tr>
                    </table>
                </div>`;

            marker.bindPopup(content);

            marker.on('click', () => {
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnMarkerClicked', pin.id);
                }
            });

            this._markers.push(marker);
            bounds.extend([pin.latitude, pin.longitude]);
        });

        if (pins.length === 1) {
            this._map.setView([pins[0].latitude, pins[0].longitude], 14);
        } else {
            this._map.fitBounds(bounds, { padding: [50, 50] });
        }
    },

    _statusColor: function (status) {
        switch (status) {
            case 'Draft': return '#888';
            case 'InProgress': return '#f59e0b';
            case 'Complete': return '#10b981';
            case 'Archived': return '#6b7280';
            default: return '#3b82f6';
        }
    },

    dispose: function () {
        if (this._map) {
            this._map.remove();
            this._map = null;
            this._markers = [];
        }
    }
};
