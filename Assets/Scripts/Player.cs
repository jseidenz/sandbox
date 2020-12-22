using UnityEngine;
using System.Collections.Generic;

public class Player
{
    const string PLAYER_NAME_ID = "PlayerName";

    static string[] m_names = new string[]
    {
        "Nibbles",
        "Chippy",
        "Breezer",
        "Racer",
        "Peanut",
        "Echo",
        "Roco"
    };

    public static string GetPlayerName()
    {
        if(!PlayerPrefs.HasKey(PLAYER_NAME_ID))
        {
            SetPlayerName(m_names[UnityEngine.Random.Range(0, m_names.Length)]);
        }

        return PlayerPrefs.GetString(PLAYER_NAME_ID);
        
    }

    public static void SetPlayerName(string player_name)
    {
        PlayerPrefs.SetString(PLAYER_NAME_ID, player_name);
    }
}