using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
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
    bool m_is_connceted_to_master;
    List<RoomInfo> m_rooms = new List<RoomInfo>();

    public static NetCode Instance;

    void Awake()
    {
        Instance = this;

        Debug.Log("Connecting");
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = GAME_VERSION;
    }

    public override void OnConnectedToMaster()
    {
        //Debug.Log("ConnectedToMaster");
        //PhotonNetwork.JoinRandomRoom();
        m_is_connceted_to_master = true;

        PhotonNetwork.JoinLobby();
    }

    public void CreateRoom(string room_name)
    {
        PhotonNetwork.CreateRoom(room_name, new RoomOptions());
    }

    public void JoinRoom(string room_name)
    {
        PhotonNetwork.JoinRoom(room_name);
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
        return m_is_connceted_to_master;
    }

    public List<RoomInfo> GetRooms() { return m_rooms; }
}