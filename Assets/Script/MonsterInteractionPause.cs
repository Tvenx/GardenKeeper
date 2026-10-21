
using System.Collections.Generic;
using UnityEngine;

public static class MonsterInteractionPause
{
    private static readonly HashSet<MonsterController> monsters = new();

    public static void Register(MonsterController monster)
    {
        if (monster != null)
            monsters.Add(monster);
    }

    public static void Unregister(MonsterController monster)
    {
        monsters.Remove(monster);
    }

    public static void PauseAll(float duration = 3.5f)
    {
        foreach (MonsterController monster in monsters)
        {
            if (monster != null)
                monster.PauseForInteraction(duration);
        }
    }
}
