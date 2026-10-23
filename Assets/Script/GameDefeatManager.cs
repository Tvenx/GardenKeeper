using System;
using TMPro;
using UnityEngine;

public enum GameDefeatReason
{
    ForestOverrun,
    MonsterCaught
}

[DisallowMultipleComponent]
public class GameDefeatManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameOverPanel2;
    [SerializeField] private TextMeshProUGUI reasonHint;

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

        // Выбираем панель в зависимости от причины поражения:
        // заражение деревьев -> вторая панель, иначе -> первая.
        bool useSecondPanel = reason == GameDefeatReason.ForestOverrun;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(!useSecondPanel);
        }

        if (gameOverPanel2 != null)
        {
            gameOverPanel2.SetActive(useSecondPanel);
        }

        if (reasonHint != null)
        {
            reasonHint.text = message;
        }

        return true;
    }
}