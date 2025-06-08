using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using System.Net.WebSockets;
using Newtonsoft.Json;          // ← 记得在 Package Manager 安装 *com.unity.nuget.newtonsoft-json*
using Newtonsoft.Json.Linq;

public class PositionPanelController : MonoBehaviour
{
    [SerializeField] UIDocument uiDoc;
    [SerializeField] string wsUrl =
        "ws://login.threekingdom.realmoflegend.com:8000/ws/position/5";

    /*──────── UI ────────*/
    Label statusLbl;
    Button connectBtn, disconnectBtn, sendBtn;
    IntegerField xField, yField;
    ListView playerList;

    /*──────── 数据 ────────*/
    readonly Dictionary<string, Vector2Int> players = new();
    readonly List<string> display = new();          // IList 供 ListView 用

    ClientWebSocket ws;
    CancellationTokenSource cts;

    void Awake()
    {
        var root = uiDoc.rootVisualElement;
        statusLbl     = root.Q<Label>("StatusLabel");
        connectBtn    = root.Q<Button>("ConnectBtn");
        disconnectBtn = root.Q<Button>("DisconnectBtn");
        sendBtn       = root.Q<Button>("SendBtn");
        xField        = root.Q<IntegerField>("XField");
        yField        = root.Q<IntegerField>("YField");
        playerList    = root.Q<ListView>("PlayerList");

        connectBtn.clicked    += Connect;
        disconnectBtn.clicked += Disconnect;
        sendBtn.clicked       += SendPosition;

        /* ListView 基础配置 */
        playerList.makeItem = () => new Label();
        playerList.bindItem = (e, i) =>
            (e as Label).text = display[i];
        playerList.itemsSource = display;           // 必须是 IList
    }

    /*──────── WebSocket ────────*/
    async void Connect()
    {
        if (ws is { State: WebSocketState.Open }) return;

        ws  = new ClientWebSocket();
        cts = new CancellationTokenSource();
        try
        {
            statusLbl.text = "Connecting…";
            await ws.ConnectAsync(new Uri(wsUrl), cts.Token);
            statusLbl.text = "Connected";
            _ = ListenLoop();
        }
        catch (Exception ex)
        {
            statusLbl.text = $"Error: {ex.Message}";
        }
    }
    async void Disconnect()
    {
        if (ws == null) return;
        cts?.Cancel();
        try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None); }
        catch { }
        statusLbl.text = "Disconnected";
        ws = null;
    }

    /*──────── 收包 ────────*/
    async Task ListenLoop()
    {
        var buf = new byte[4096];
        while (ws.State == WebSocketState.Open)
        {
            var seg = new ArraySegment<byte>(buf);
            using var ms = new System.IO.MemoryStream();
            WebSocketReceiveResult r;
            do
            {
                r = await ws.ReceiveAsync(seg, cts.Token);
                ms.Write(seg.Array, seg.Offset, r.Count);
            } while (!r.EndOfMessage);

            HandleMessage(Encoding.UTF8.GetString(ms.ToArray()));
        }
    }

    void HandleMessage(string json)
    {
        var root = JObject.Parse(json);
        var type = root["type"]?.Value<string>();

        if (type == "all_players")
        {
            players.Clear();
            foreach (var p in root["players"])
                players[p["id"].ToString()] = new Vector2Int(
                    (int)p["position"]["x"], (int)p["position"]["y"]);
        }
        else if (type == "player_moved")
        {
            var id = root["id"].ToString();
            players[id] = new Vector2Int(
                (int)root["position"]["x"], (int)root["position"]["y"]);
        }
        RefreshList();
    }

    void RefreshList()
    {
        display.Clear();
        foreach (var kv in players)
            display.Add($"{kv.Key}: ({kv.Value.x},{kv.Value.y})");
        playerList.Rebuild();
    }

    /*──────── 发包 ────────*/
    async void SendPosition()
    {
        if (ws is not { State: WebSocketState.Open }) return;

        var msg = new
        {
            type = "update_position",
            position = new { x = xField.value, y = yField.value }
        };
        var json = JsonConvert.SerializeObject(msg);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes),
                           WebSocketMessageType.Text, true, cts.Token);
    }
    void OnDestroy() => Disconnect();
}
