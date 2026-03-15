const API_BASE = '';

let isRunning = false;
let updateInterval = null;

async function apiCall(endpoint, options = {}) {
    try {
        const response = await fetch(`${API_BASE}${endpoint}`, {
            headers: { 'Content-Type': 'application/json' },
            ...options
        });
        return await response.json();
    } catch (error) {
        console.error('API Error:', error);
    }
}

async function loadStatus() {
    const status = await apiCall('/api/status') || {};
    
    document.getElementById('totalReceived').textContent = status.totalReceived || 0;
    document.getElementById('totalBytes').textContent = formatBytes(status.totalBytes || 0);
    
    const startBtn = document.getElementById('startBtn');
    const stopBtn = document.getElementById('stopBtn');
    const badge = document.getElementById('statusBadge');
    
    isRunning = status.isRunning || false;
    startBtn.disabled = isRunning;
    stopBtn.disabled = !isRunning;
    badge.textContent = isRunning ? 'Listening' : 'Stopped';
    badge.className = isRunning ? 'status-badge running' : 'status-badge';
}

async function loadLog() {
    const logs = await apiCall('/api/log') || [];
    const list = document.getElementById('logList');
    
    if (logs.length === 0) {
        list.innerHTML = '<div class="empty-state">No PDUs received yet</div>';
        return;
    }

    list.innerHTML = logs.map(log => {
        const typeClass = getTypeClass(log.type);
        return `
            <div class="log-item">
                <span class="log-time">${formatTime(log.time)}</span>
                <span class="log-type ${typeClass}">${log.type}</span>
                <span class="log-entity">ID:${log.entityId || '-'}</span>
                <span class="log-message">${log.message || ''}</span>
                <span class="log-size">${log.size || 0}B</span>
            </div>
        `;
    }).join('');
}

function getTypeClass(type) {
    if (type === 'SYSTEM') return 'system';
    if (type.includes('State')) return 'entity';
    if (type === 'Fire' || type === 'Detonation') return 'fire';
    if (type === 'Collision') return 'collision';
    if (type.includes('Start') || type.includes('Stop') || type.includes('Resume') || type.includes('Freeze')) return 'sim';
    return '';
}

async function loadPduTypes() {
    const types = await apiCall('/api/pdu-types') || [];
    const grid = document.getElementById('typesGrid');
    
    grid.innerHTML = types.map(t => `
        <div class="type-item">
            <span class="type-code">${t.type}</span>
            <span class="type-name">${t.name}</span>
        </div>
    `).join('');
}

async function startReceiver() {
    await apiCall('/api/start', { method: 'POST' });
    await loadStatus();
    await loadLog();
}

async function stopReceiver() {
    await apiCall('/api/stop', { method: 'POST' });
    await loadStatus();
}

async function updateConfig() {
    const multicast = document.getElementById('multicast').value;
    const port = parseInt(document.getElementById('port').value);

    await apiCall('/api/config', {
        method: 'POST',
        body: JSON.stringify({ multicastAddress: multicast, port })
    });
}

async function clearLog() {
    await apiCall('/api/log', { method: 'DELETE' });
    await loadLog();
}

function formatTime(dateStr) {
    if (!dateStr) return '--:--:--';
    const date = new Date(dateStr);
    return date.toLocaleTimeString('en-US', { hour12: false });
}

function formatBytes(bytes) {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}

async function init() {
    await loadPduTypes();
    await loadStatus();
    await loadLog();
    
    updateInterval = setInterval(async () => {
        await loadStatus();
        await loadLog();
    }, 1000);
}

document.addEventListener('DOMContentLoaded', init);
