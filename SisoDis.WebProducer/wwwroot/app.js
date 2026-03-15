const API_BASE = '';

let entities = [];
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

async function loadEntities() {
    entities = await apiCall('/api/entities') || [];
    renderEntities();
}

async function addEntity() {
    const id = parseInt(document.getElementById('entityId').value);
    const pattern = document.getElementById('pattern').value;
    const speed = parseFloat(document.getElementById('speed').value);
    const rate = parseInt(document.getElementById('rate').value);

    if (!id || id < 1) {
        alert('Please enter a valid Entity ID');
        return;
    }

    const result = await apiCall('/api/entities', {
        method: 'POST',
        body: JSON.stringify({ id, pattern, speed })
    });

    if (result.error) {
        alert(result.error);
        return;
    }

    document.getElementById('entityId').value = id + 1;
    await loadEntities();
}

async function deleteEntity(id) {
    await apiCall(`/api/entities/${id}`, { method: 'DELETE' });
    await loadEntities();
}

function renderEntities() {
    const list = document.getElementById('entityList');
    const count = document.getElementById('entityCount');
    
    if (entities.length === 0) {
        list.innerHTML = '<div class="empty-state">No entities added yet</div>';
        count.textContent = '0';
        return;
    }

    list.innerHTML = entities.map(e => `
        <div class="entity-item">
            <div class="entity-info">
                <span class="entity-id">ID: ${e.id}</span>
                <span class="entity-pattern">${e.pattern}</span>
                <span class="entity-position">@ ${e.position?.x?.toFixed(0) || 0}, ${e.position?.y?.toFixed(0) || 0}</span>
            </div>
            <button class="entity-delete" onclick="deleteEntity(${e.id})">✕</button>
        </div>
    `).join('');

    count.textContent = entities.length;
}

async function startSimulation() {
    await apiCall('/api/start', { method: 'POST' });
    isRunning = true;
    updateButtons();
}

async function stopSimulation() {
    await apiCall('/api/stop', { method: 'POST' });
    isRunning = false;
    updateButtons();
}

async function sendStartPdu() {
    await apiCall('/api/sim/start', { method: 'POST' });
    addLog('START/RESUME PDU sent', 'start');
}

async function sendStopPdu() {
    await apiCall('/api/sim/stop', { method: 'POST' });
    addLog('STOP/FREEZE PDU sent', 'stop');
}

async function updateConfig() {
    const multicast = document.getElementById('multicast').value;
    const port = parseInt(document.getElementById('port').value);
    const rate = parseInt(document.getElementById('rate').value);

    await apiCall('/api/config', {
        method: 'POST',
        body: JSON.stringify({ multicastAddress: multicast, port, rate })
    });

    addLog(`Config updated: ${multicast}:${port} @ ${rate}Hz`, '');
}

function updateButtons() {
    const startBtn = document.getElementById('startBtn');
    const stopBtn = document.getElementById('stopBtn');
    const badge = document.getElementById('statusBadge');

    startBtn.disabled = isRunning;
    stopBtn.disabled = !isRunning;
    badge.textContent = isRunning ? 'Running' : 'Stopped';
    badge.className = isRunning ? 'status-badge running' : 'status-badge';
}

async function updateLog() {
    const logs = await apiCall('/api/log') || [];
    const list = document.getElementById('logList');
    
    if (logs.length === 0) {
        list.innerHTML = '<div class="empty-state">No activity yet</div>';
        return;
    }

    list.innerHTML = logs.map(log => {
        let className = '';
        if (log.includes('FIRE')) className = 'fire';
        else if (log.includes('START') && !log.includes('STOP')) className = 'start';
        else if (log.includes('STOP')) className = 'stop';
        return `<div class="log-item ${className}">${log}</div>`;
    }).join('');
}

function addLog(message, type) {
    updateLog();
}

function clearLog() {
    document.getElementById('logList').innerHTML = '<div class="empty-state">No activity yet</div>';
}

async function init() {
    await loadEntities();
    await updateLog();
    
    updateInterval = setInterval(async () => {
        await loadEntities();
        await updateLog();
    }, 1000);
}

document.addEventListener('DOMContentLoaded', init);
