using System;
using UnityEngine;

public enum GameDefeatReason
{
    ForestOverrun,
    MonsterCaught
}

[DisallowMultipleComponent]
public class GameDefeatManager : MonoBehaviour
{
    public bool IsDefeated { get; private set; }

    public GameDefeatReason DefeatReason { get; private set; }

    public event Action<GameDefeatReason, string> DefeatTriggered;

    public bool TriggerDefeat(
        GameDefeatReason reason,
        string details = "",
        UnityEngine.Object context = null)
    {
        if (IsDefeated)
        {
            return false;
        }

        IsDefeated = true;
        DefeatReason = reason;

        string message = reason switch
        {
            GameDefeatReason.ForestOverrun =>
                "DEFEAT! The forest has been overrun by infection.",

            GameDefeatReason.MonsterCaught =>
                "DEFEAT! The monster caught the player.",

            _ => "DEFEAT!"
        };

        if (!string.IsNullOrWhiteSpace(details))
        {
            message += " " + details;
        }

        Debug.LogError(message, context != null ? context : this);

        DefeatTriggered?.Invoke(reason, details);

        return true;
    }
}