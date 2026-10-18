
using System.Text;
using TMPro;
using UnityEngine;

public class GameDebugHUD : MonoBehaviour
{
    [Header("TextMeshPro")]
    [SerializeField] private TMP_Text outputText;

    [Min(0.05f)]
    [SerializeField] private float refreshInterval = 0.2f;

    [Header("References")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerInteraction interaction;
    [SerializeField] private PlayerCandleUse candleUse;
    [SerializeField] private InfectionManager infectionManager;
    [SerializeField] private StatueCandleStation statue;

    private float refreshTimer;

    private void Awake()
    {
        if (outputText == null)
        {
            outputText = GetComponent<TMP_Text>();
        }

        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();

        if (interaction == null)
            interaction = FindFirstObjectByType<PlayerInteraction>();

        if (candleUse == null)
            candleUse = FindFirstObjectByType<PlayerCandleUse>();

        if (infectionManager == null)
            infectionManager = FindFirstObjectByType<InfectionManager>();

        if (statue == null)
            statue = FindFirstObjectByType<StatueCandleStation>();
    }

    private void Update()
    {
        if (outputText == null)
            return;

        refreshTimer += Time.unscaledDeltaTime;

        if (refreshTimer >= Mathf.Max(0.05f, refreshInterval))
        {
            refreshTimer = 0f;
            RefreshDebugText();
        }
    }

    private void RefreshDebugText()
    {
        StringBuilder text = new StringBuilder();

        AddLine(text, "Предмет в руке",
            inventory != null
                ? GetItemName(inventory.CurrentItem)
                : "Инвентарь не найден");

        AddLine(text, "Кнопка взаимодействия",
            interaction != null
                ? interaction.InteractionButtonDisplayName
                : "PlayerInteraction не найден");

        AddLine(text, "Действие при нажатии",
            interaction != null
                ? interaction.GetCurrentActionDescription()
                : "Недоступно");

        if (inventory != null &&
            inventory.CurrentItem == InventoryItemType.Candle)
        {
            AddLine(text, "До сгорания свечи",
                FormatSeconds(inventory.CandleTimeRemaining));
        }
        else
        {
            AddLine(text, "До сгорания свечи", "Нет свечи в руке");
        }

        AppendTreeStatistics(text);
        AppendLookTarget(text);
        AppendLookedAtTreeInfo(text);
        AppendStatueInfo(text);
        AppendInfectionInfo(text);

        outputText.text = text.ToString();
    }

    private void AppendTreeStatistics(StringBuilder text)
    {
        int total = 0;
        int infected = 0;
        int healthy = 0;
        int bleeding = 0;

        if (infectionManager != null)
        {
            infectionManager.GetTreeStateCounts(
                out total,
                out infected,
                out healthy,
                out bleeding
            );
        }
        else
        {
            InfectableTree[] trees =
                FindObjectsByType<InfectableTree>(
                    FindObjectsSortMode.None
                );

            total = trees.Length;

            foreach (InfectableTree tree in trees)
            {
                if (tree.IsCurrentlyInfected)
                    infected++;
                else if (tree.IsBleeding)
                    bleeding++;
                else
                    healthy++;
            }
        }

        AddLine(text, "Всего активных деревьев", total.ToString());
        AddLine(text, "Заражённых деревьев", infected.ToString());
        AddLine(text, "Здоровых деревьев", healthy.ToString());
        AddLine(text, "Кровоточащих деревьев", bleeding.ToString());
    }

    private void AppendLookTarget(StringBuilder text)
    {
        if (interaction == null)
        {
            AddLine(text, "Объект под прицелом", "PlayerInteraction не найден");
            return;
        }

        InteractableObject target = interaction.LookedAtInteractable;

        if (target is StatueCandleStation)
        {
            AddLine(text, "Объект под прицелом", $"Статуя ({target.name})");
        }
        else if (target is InfectableTree)
        {
            AddLine(text, "Объект под прицелом", $"Дерево ({target.name})");
        }
        else if (target != null)
        {
            AddLine(text, "Объект под прицелом",
                $"Другое интерактивное: {target.name}");
        }
        else if (interaction.LookedAtObject != null)
        {
            AddLine(text, "Объект под прицелом",
                $"Другое: {interaction.LookedAtObject.name}");
        }
        else
        {
            AddLine(text, "Объект под прицелом", "Ничего");
        }
    }

    private void AppendLookedAtTreeInfo(StringBuilder text)
    {
        InfectableTree tree = interaction != null
            ? interaction.LookedAtInteractable as InfectableTree
            : null;

        if (tree == null)
        {
            AddLine(text, "Тип дерева под прицелом", "Нет дерева");
            AddLine(text, "История заражения выбранного дерева", "Нет дерева");
            AddLine(text, "До распространения выбранного дерева", "Нет дерева");
            AddLine(text, "До следующей попытки кровоточивости", "Нет дерева");
            return;
        }

        string state;

        if (tree.IsCurrentlyInfected)
            state = "Заражённое";
        else if (tree.IsBleeding)
            state = "Кровоточащее";
        else
            state = "Здоровое";

        AddLine(text, "Тип дерева под прицелом", state);

        string bleedingTimerStatus;

        if (tree.IsBleedingTimerRunning)
        {
            bleedingTimerStatus = "отсчёт идёт";
        }
        else if (!tree.HasEverBeenInfected)
        {
            bleedingTimerStatus = "пауза: ещё не заражалось";
        }
        else if (tree.IsCurrentlyInfected)
        {
            bleedingTimerStatus = "пауза: дерево заражено";
        }
        else if (tree.IsBleeding)
        {
            bleedingTimerStatus = "пауза: дерево уже кровоточит";
        }
        else
        {
            bleedingTimerStatus = "пауза";
        }

        AddLine(
            text,
            "До следующей попытки кровоточивости",
            $"{FormatSeconds(tree.BleedingTimeRemaining)} " +
            $"({bleedingTimerStatus})"
        );

        AddLine(text, "История заражения выбранного дерева",
            tree.HasEverBeenInfected ? "Заражалось раньше" : "Не заражалось");

        if (!tree.IsCurrentlyInfected)
        {
            AddLine(text, "До распространения выбранного дерева",
                "Дерево сейчас не заражено");
        }
        else if (infectionManager == null)
        {
            AddLine(text, "До распространения выбранного дерева",
                "Менеджер заражения не найден");
        }
        else if (!infectionManager.SpreadEnabled)
        {
            AddLine(text, "До распространения выбранного дерева",
                "Распространение отключено");
        }
        else if (infectionManager.TryGetSpreadTimeRemaining(
            tree, out float secondsRemaining))
        {
            AddLine(text, "До распространения выбранного дерева",
                FormatSeconds(secondsRemaining));
        }
        else
        {
            AddLine(text, "До распространения выбранного дерева",
                "Таймер не зарегистрирован; проверь список деревьев Manager");
        }
    }

    private void AppendStatueInfo(StringBuilder text)
    {
        StatueCandleStation displayStatue = statue;

        if (interaction != null &&
            interaction.LookedAtInteractable is StatueCandleStation aimedStatue)
        {
            displayStatue = aimedStatue;
        }

        if (displayStatue == null)
        {
            AddLine(text, "Кровь, переданная статуе", "Статуя не найдена");
            AddLine(text, "Свечей осталось у статуи", "Статуя не найдена");
            return;
        }

        AddLine(text, "Кровь, переданная статуе",
            $"{displayStatue.CurrentBloodDeposits}/" +
            $"{displayStatue.RequiredBloodDeposits}");

        AddLine(text, "Свечей осталось у статуи",
            displayStatue.AvailableCandles.ToString());

        AddLine(text, "Статуя заполнена",
            displayStatue.HasWon ? "Да — победа" : "Нет");
    }

    private void AppendInfectionInfo(StringBuilder text)
    {
        if (infectionManager == null)
        {
            AddLine(text, "Информация о заражении", "Менеджер не найден");
            return;
        }

        infectionManager.GetCurrentRandomInfectionIntervalRange(
            out float randomMin,
            out float randomMax
        );

        AddLine(text, "Интервал случайного заражения",
            infectionManager.RandomInfectionEnabled
                ? $"{randomMin:F1}–{randomMax:F1} сек."
                : "Отключено");

        AddLine(text, "До следующей случайной попытки заражения",
            infectionManager.RandomInfectionEnabled
                ? FormatSeconds(infectionManager.TimeUntilNextRandomInfection)
                : "Отключено");

        infectionManager.GetCurrentSpreadIntervalRange(
            out float spreadMin,
            out float spreadMax
        );

        AddLine(text, "Интервал распространения",
            infectionManager.SpreadEnabled
                ? $"{spreadMin:F1}–{spreadMax:F1} сек. (локальный)"
                : "Отключено");

        AddLine(text, "Глобальный таймер распространения",
            "Нет — таймер отдельный для каждого дерева");

        int elapsed = Mathf.FloorToInt(infectionManager.ElapsedGameTime);
        int minutes = elapsed / 60;
        int seconds = elapsed % 60;

        AddLine(text, "Время с начала игры",
            $"{minutes:00}:{seconds:00}");

        AddLine(text, "Возможность проиграть",
            infectionManager.CanLose ? "Включена" : "Отключена");

        AddLine(text, "Состояние поражения",
            infectionManager.HasLost ? "ПОРАЖЕНИЕ" : "Игра продолжается");

        string defeatThreshold =
            infectionManager.DefeatMode == InfectionDefeatMode.Percentage
                ? $"{infectionManager.LoseAtInfectedPercentage:F1}% заражённых"
                : $"{infectionManager.LoseAtInfectedTreeCount} деревьев";

        AddLine(text, "Порог поражения", defeatThreshold);
    }

    private static string GetItemName(InventoryItemType item)
    {
        switch (item)
        {
            case InventoryItemType.Candle:
                return "Свеча";

            case InventoryItemType.Blood:
                return "Кровь";

            default:
                return "Пусто";
        }
    }

    private static string FormatSeconds(float seconds)
    {
        return $"{Mathf.Max(0f, seconds):F1} сек.";
    }

    private static void AddLine(
        StringBuilder text,
        string label,
        string value)
    {
        text.Append(label)
            .Append(": ")
            .AppendLine(value);
    }
}
