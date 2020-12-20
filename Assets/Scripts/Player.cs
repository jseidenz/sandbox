using UnityEngine;
using System.Collections.Generic;

public class Player
{
    const string PLAYER_NAME_ID = "PlayerName";
    public static string GetPlayerName()
    {
        var default_name = $"Visitor #{UnityEngine.Random.Range(100, 999)}";
        return PlayerPrefs.GetString(PLAYER_NAME_ID, default_name);
    }

    public static void SetPlayerName(string player_name)
    {
        PlayerPrefs.SetString(PLAYER_NAME_ID, player_name);
    }
}