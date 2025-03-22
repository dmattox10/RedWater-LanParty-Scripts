const WebSocket = require('ws');
const server = new WebSocket.Server({ port: 3000 });

const gameState = {
    ships: new Map(),
    lastUpdateTime: Date.now()
};

const generatePlayerId = () => {
    const characters = 'ABCDEF0123456789';
    let id = '';
    for (let i = 0; i < 6; i++) {
        id += characters[Math.floor(Math.random() * characters.length)];
    }
    return id; 
}

const TICK_RATE = 60; // Updates per second
const TICK_INTERVAL = 1000 / TICK_RATE;

// Ship configuration constants matching Unity ShipConfigurationSO
const SHIP_CONFIGS = {
    Small: {
        stats: {
            maxSpeed: 5,
            acceleration: 0.1,
            deceleration: 0.05,
            reverseMaxSpeed: 2,
            turnAcceleration: 0.8,
            mass: 1.0
        }
    },
    Medium: {
        stats: {
            maxSpeed: 4,
            acceleration: 0.08,
            deceleration: 0.03,
            reverseMaxSpeed: 1.5,
            turnAcceleration: 0.6,
            mass: 1.5
        }
    },
    Large: {
        stats: {
            maxSpeed: 3,
            acceleration: 0.05,
            deceleration: 0.02,
            reverseMaxSpeed: 1,
            turnAcceleration: 0.4,
            mass: 2.0
        }
    }
};

server.on('connection', async (ws) => {
    const playerId = generatePlayerId();
    ws.playerId = playerId;  // Store playerId with the connection
    console.log(`Player ${playerId} connected`);

    // Send player ID immediately and wait for confirmation
    await new Promise((resolve) => {
        ws.send(JSON.stringify({
            type: "PlayerJoined",  // Must match MessageType enum in C#
            playerId: playerId,
            data: null
        }), resolve);
    });

    // Then add to game state
    gameState.ships.set(playerId, {
        playerId,
        position: { x: 0, y: 0 },
        rotation: 0,
        shipClass: 'Small',
        activeWeapons: [],
        abilitiesUnlocked: [false, false, false]
    });

    // Rest of connection handling
    ws.on('message', (message) => {
        try {
            const data = JSON.parse(message);
            console.log('[SERVER] Received message:', data);
            
            if (!data.playerId) {
                console.error('[SERVER] Message received without player ID:', data);
                return;
            }

            switch (data.type) {
                case 'PlayerInput':
                    const inputData = JSON.parse(data.data);
                    console.log(`[SERVER] Processing input for player ${data.playerId}:`, inputData);
                    handlePlayerInput(data.playerId, inputData);
                    break;
            }
        } catch (error) {
            console.error('[SERVER] Error processing message:', error);
        }
    });

    ws.on('close', () => {
        gameState.ships.delete(playerId);
        broadcastGameState();
    });
});

function handlePlayerInput(playerId, inputData) {
    const ship = gameState.ships.get(playerId);
    if (!ship) return;

    const config = SHIP_CONFIGS[ship.shipClass];
    if (!config) return;

    const stats = config.stats;
    const deltaTime = 1/60;

    // Initialize velocity if not exists
    if (!ship.velocity) {
        ship.velocity = { x: 0, y: 0 };
        ship.acceleration = { x: 0, y: 0 };
    }

    // Update rotation - remove abs and invert direction
    if (inputData.horizontal !== 0) {
        const effectiveTurn = stats.turnAcceleration / stats.mass;
        ship.rotation -= inputData.horizontal * effectiveTurn * deltaTime; // Negative for correct turn direction
        ship.rotation = ship.rotation % 360;
    }

    // Convert rotation to radians
    const rotationRad = (ship.rotation * Math.PI) / 180;

    // Calculate acceleration based on input and mass - remove abs
    if (inputData.vertical !== 0) {
        // Use raw input value for direction
        const baseAccel = stats.acceleration / stats.mass;
        // Forward is negative Y in Unity
        ship.acceleration.x = Math.sin(rotationRad) * baseAccel * -inputData.vertical;
        ship.acceleration.y = Math.cos(rotationRad) * baseAccel * -inputData.vertical;
    } else {
        // Apply deceleration to acceleration
        ship.acceleration.x *= (1 - stats.deceleration);
        ship.acceleration.y *= (1 - stats.deceleration);
    }

    // Update velocity with acceleration
    ship.velocity.x += ship.acceleration.x * deltaTime;
    ship.velocity.y += ship.acceleration.y * deltaTime;

    // Apply mass-based drag when no input
    if (inputData.vertical === 0) {
        const drag = stats.deceleration / stats.mass;
        ship.velocity.x *= (1 - drag * deltaTime);
        ship.velocity.y *= (1 - drag * deltaTime);
    }

    // Limit speed based on direction and ship stats
    const currentSpeed = Math.sqrt(ship.velocity.x * ship.velocity.x + ship.velocity.y * ship.velocity.y);
    const maxSpeed = inputData.vertical >= 0 ? stats.maxSpeed : stats.reverseMaxSpeed;

    if (currentSpeed > maxSpeed) {
        const scale = maxSpeed / currentSpeed;
        ship.velocity.x *= scale;
        ship.velocity.y *= scale;
    }

    // Update position
    ship.position.x += ship.velocity.x * deltaTime;
    ship.position.y += ship.velocity.y * deltaTime;
}

function broadcastGameState() {
    const state = {
        type: 'GameState',
        data: JSON.stringify({
            ships: Array.from(gameState.ships.values()).map(ship => ({
                ...ship,
                isEnemy: true  // All ships will be marked as enemy by default
            })),
            serverTime: Date.now()
        })
    };

    server.clients.forEach(client => {
        if (client.readyState === WebSocket.OPEN) {
            // Create a custom state for each client where their ship is not marked as enemy
            const clientState = {
                ...state,
                data: JSON.stringify({
                    ships: JSON.parse(state.data).ships.map(ship => ({
                        ...ship,
                        isEnemy: ship.playerId !== client.playerId
                    })),
                    serverTime: Date.now()
                })
            };
            client.send(JSON.stringify(clientState));
        }
    });
}

// Update loop
setInterval(() => {
    gameState.lastUpdateTime = Date.now();
    broadcastGameState();
}, TICK_INTERVAL);