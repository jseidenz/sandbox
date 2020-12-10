using UnityEngine;
using System.Collections.Generic;

public class Player
{
    const string PLAYER_NAME_ID = "PlayerName";
    public static string GetPlayerName()
    {
        return PlayerPrefs.GetString(PLAYER_NAME_ID, "Islander");
    }

    public static void SetPlayerName(string player_name)
    {
        PlayerPrefs.SetString(PLAYER_NAME_ID, player_name);
    }
}