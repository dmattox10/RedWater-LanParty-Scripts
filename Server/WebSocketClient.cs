using UnityEngine;
using System.Threading.Tasks;
using NativeWebSocket;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket websocket;
    private readonly string serverUrl = "ws://localhost:3000";
    private string playerId;
    
    public string PlayerId => playerId;
    
    public delegate void MessageHandler(string data);
    public event MessageHandler OnGameStateReceived;

    private async void Start()
    {
        await ConnectToServer();
    }

    private async Task ConnectToServer()
    {
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log("Connected to server");
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError($"Error: {e}");
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed");
        };

        websocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            HandleMessage(message);
        };

        await websocket.Connect();
    }

    private void HandleMessage(string jsonMessage)
    {
        Debug.Log($"HandleMessage received: {jsonMessage}");
        try 
        {
            // First parse as JObject to handle the string enum
            var jsonObj = JsonUtility.FromJson<NetworkMessage>(jsonMessage);
            
            // Handle string type comparison instead of enum
            if (jsonObj.type.ToString() == "PlayerJoined")
            {
                playerId = jsonObj.playerId;
                Debug.Log($"Assigned PlayerID: {playerId}"); // More descriptive logging
            }
            else if (jsonObj.type.ToString() == "GameState")
            {
                // Only handle game state if we have a player ID
                if (!string.IsNullOrEmpty(playerId))
                {
                    OnGameStateReceived?.Invoke(jsonObj.data);
                }
            }

            // Log the current state
            Debug.Log($"Current player ID: {playerId ?? "not set"}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing message: {e.Message}\nMessage: {jsonMessage}");
        }
    }

    public async void SendPlayerInput(float horizontal, float vertical)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("No player ID assigned yet!");
            return;
        }

        if (websocket?.State != WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket not connected");
            return;
        }

        var inputData = new PlayerInputData
        {
            horizontal = horizontal,
            vertical = vertical,
            timestamp = Time.time
        };

        var inputJson = JsonUtility.ToJson(inputData);
        var message = new NetworkMessage(MessageType.PlayerInput, playerId, inputJson);

        string jsonMessage = JsonUtility.ToJson(message);
        Debug.Log($"Sending input with playerId {playerId}: {jsonMessage}");
        await websocket.SendText(jsonMessage);
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
            await websocket.Close();
    }

    private void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
        #endif
    }
}