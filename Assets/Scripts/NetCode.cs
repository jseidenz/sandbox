using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class NetCode : MonoBehaviourPunCallbacks
{
    static string VERSION_NUMBER = "1";
#if UNITY_EDITOR
    static string GAME_VERSION = $"UNITY_EDITOR_{VERSION_NUMBER}";
#else
    static string GAME_VERSION = $"UNITY_BUILD_{VERSION_NUMBER}";
#endif

    bool m_has_joined_room;
    bool m_is_connected_to_master;
    List<RoomInfo> m_rooms = new List<RoomInfo>();

    public static NetCode Instance;

    void Awake()
    {
        Instance = this;

        Debug.Log("Connecting");
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = GAME_VERSION;

        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    public override void OnConnectedToMaster()
    {
        //Debug.Log("ConnectedToMaster");
        //PhotonNetwork.JoinRandomRoom();
        m_is_connected_to_master = true;

        PhotonNetwork.JoinLobby();
    }

    public void CreateRoom(string room_id)
    {
        PhotonNetwork.CreateRoom(room_id, new RoomOptions());
    }

    public void JoinRoom(string room_id)
    {
        PhotonNetwork.JoinRoom(room_id);
    }

    public override void OnJoinRandomFailed(short return_code, string message)
    {
        Debug.Log("JoinRoomFailed");
        PhotonNetwork.CreateRoom(null, new RoomOptions());
    }

    public override void OnCreateRoomFailed(short return_code, string message)
    {
        Debug.LogError($"CreateRoomFailed: {message} return_code={return_code}");
    }

    public override void OnRoomListUpdate(List<RoomInfo> rooms)
    {
        m_rooms = rooms;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("JoinedRoom");
        m_has_joined_room = true;
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("OnJoinedLobby");
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public bool HasJoinedRoom()
    {
        return m_has_joined_room;
    }

    public bool IsConnectedToMaster()
    {
        return m_is_connected_to_master;
    }

    public const byte LOAD_SAVE_DATA_ID = 1;
    public const byte COMMAND_BUFFER_FROM_CLIENT_ID = 2;
    public const byte COMMAND_BUFFER_FROM_SERVER_ID = 3;

    public void SendCommandsToServer(byte[] command_buffer)
    {
        var content = new object[] { command_buffer };
        var raise_event_options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(COMMAND_BUFFER_FROM_CLIENT_ID, content, raise_event_options, SendOptions.SendReliable);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player new_player)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var content = new object[] { Game.Instance.CreateSaveData() };
        var raise_event_options = new RaiseEventOptions { TargetActors = new int[] { new_player.ActorNumber } };
        PhotonNetwork.RaiseEvent(LOAD_SAVE_DATA_ID, content, raise_event_options, SendOptions.SendReliable);
    }

    private void OnEvent(EventData evt)
    {
        byte event_code = evt.Code;
        if (event_code == LOAD_SAVE_DATA_ID)
        {
            Debug.Log("LoadSaveData!");
            var content = (object[])evt.CustomData;
            var save_data = (byte[])content[0];
            Game.Instance.LoadCompressedSaveFileBytes(save_data);
        }
        else if(event_code == COMMAND_BUFFER_FROM_CLIENT_ID)
        {
            var raise_event_options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(COMMAND_BUFFER_FROM_SERVER_ID, (object[])evt.CustomData, raise_event_options, SendOptions.SendReliable);
        }
        else if(event_code == COMMAND_BUFFER_FROM_SERVER_ID)
        {
            var content = (object[])evt.CustomData;
            var command_buffer = (byte[])content[0];

            CommandHandlerManager.RunCommands(command_buffer, 0, command_buffer.Length);
        }
    }

    public List<RoomInfo> GetRooms() { return m_rooms; }
}