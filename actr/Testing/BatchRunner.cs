using actr.Core;

namespace actr.Testing;

/// <summary>
/// Прогоняет агента по всему датасету (known facts + gaps) и собирает статистику:
/// сколько решено, сколько провалов, как менялась utility продукций по ходу прогона,
/// сколько тактов в среднем уходило на known vs gap.
/// </summary>
public static class BatchRunner
{
    public class Stats
    {
        public int TotalPairs;
        public int Solved;
        public int CorrectlySolved;     // решено И ответ совпал с реальным произведением
        public int IncorrectlySolved;   // решено, но ответ неверный (не должно случаться, но проверяем)
        public int HandledFailures;     // gap, агент корректно сдался (handle-failure)
        public int Stuck;               // застрял без обработки
        public int CycleLimitHit;

        public List<int> CyclesOnKnown { get; } = new();
        public List<int> CyclesOnGap { get; } = new();

        // Снимки utility продукций после каждой N-й задачи — для графика "обучения"
        public List<(int taskIndex, Dictionary<string, double> utilities)> UtilityHistory { get; } = new();

        public double AvgCyclesKnown => CyclesOnKnown.Count > 0 ? CyclesOnKnown.Average() : 0;
        public double AvgCyclesGap   => CyclesOnGap.Count > 0 ? CyclesOnGap.Average() : 0;
    }

    /// <summary>
    /// Запустить агента по всем парам датасета.
    /// rewardCorrect — reward за правильно решённую задачу.
    /// rewardFailure — reward за корректно обработанный retrieval failure
    ///   (можно сделать ниже, чтобы агент не "предпочитал" дыры; по умолчанию 0 — нейтрально).
    /// snapshotEvery — раз во сколько задач сохранять снимок utility (0 = не сохранять).
    /// </summary>
    public static Stats Run(
        ACTRAgent agent,
        DataGenerator.Dataset dataset,
        double rewardCorrect = 1.0,
        double rewardFailure = 0.0,
        int snapshotEvery = 10)
    {
        var stats = new Stats { TotalPairs = dataset.AllTestPairs.Count };
        var knownSet = new HashSet<(int, int)>(dataset.KnownFacts.Select(f => (f.a, f.b)));

        for (int i = 0; i < dataset.AllTestPairs.Count; i++)
        {
            var (a, b) = dataset.AllTestPairs[i];
            bool isKnown = knownSet.Contains((a, b));

            agent.SetGoal(new Chunk(
                name: $"goal-{i}-{a}-{b}",
                chunkType: "find-product",
                slots: new Dictionary<string, object?>
                {
                    ["multiplicand"] = a,
                    ["multiplier"]   = b
                }
            ));

            var result = agent.RunWithResult();

            switch (result.Outcome)
            {
                case ACTRAgent.RunOutcome.Solved when isKnown:
                    stats.Solved++;
                    stats.CorrectlySolved++; // retrieve-answer гарантированно даёт верный product
                    stats.CyclesOnKnown.Add(result.Cycles);
                    agent.RewardLastAction(rewardCorrect);
                    break;

                case ACTRAgent.RunOutcome.Solved when !isKnown:
                    // Цель закрылась (Goal очищен) на паре, которой нет в памяти —
                    // значит сработал handle-failure (он тоже зовёт ClearGoal).
                    stats.HandledFailures++;
                    stats.CyclesOnGap.Add(result.Cycles);
                    agent.RewardLastAction(rewardFailure);
                    break;

                case ACTRAgent.RunOutcome.Stuck:
                    // При текущем наборе продукций (start-retrieval, retrieve-answer,
                    // handle-failure) этот случай не должен происходить — handle-failure
                    // покрывает все retrieval failure. Если он всё же случился —
                    // это сигнал, что условия продукций где-то не пересекаются.
                    stats.Stuck++;
                    if (isKnown) stats.CyclesOnKnown.Add(result.Cycles);
                    else stats.CyclesOnGap.Add(result.Cycles);
                    break;

                case ACTRAgent.RunOutcome.CycleLimitExceeded:
                    stats.CycleLimitHit++;
                    break;
            }

            if (snapshotEvery > 0 && (i + 1) % snapshotEvery == 0)
                stats.UtilityHistory.Add((i + 1, agent.GetProductionUtilities()));
        }

        return stats;
    }

    /// <summary>
    /// Вывести отчёт по результатам прогона в консоль.
    /// </summary>
    public static void PrintReport(Stats stats)
    {
        Console.WriteLine("\n========== ОТЧЁТ ПО ПРОГОНУ ==========\n");

        Console.WriteLine($"Всего задач:               {stats.TotalPairs}");
        Console.WriteLine($"Решено верно:              {stats.CorrectlySolved}");
        Console.WriteLine($"Решено неверно:            {stats.IncorrectlySolved}  (если > 0 — баг в retrieve-answer)");
        Console.WriteLine($"Корректно обработан провал: {stats.HandledFailures}");
        Console.WriteLine($"Застрял без обработки:      {stats.Stuck}");
        Console.WriteLine($"Превышен лимит тактов:      {stats.CycleLimitHit}");

        Console.WriteLine();
        Console.WriteLine($"Среднее тактов на known:    {stats.AvgCyclesKnown:F2}");
        Console.WriteLine($"Среднее тактов на gap:      {stats.AvgCyclesGap:F2}");

        if (stats.UtilityHistory.Count > 0)
        {
            Console.WriteLine("\n--- Динамика utility продукций по ходу прогона ---\n");
            var productionNames = stats.UtilityHistory[0].utilities.Keys.ToList();

            Console.Write("Задача".PadRight(10));
            foreach (var name in productionNames)
                Console.Write(name.PadRight(20));
            Console.WriteLine();

            foreach (var (taskIndex, utilities) in stats.UtilityHistory)
            {
                Console.Write(taskIndex.ToString().PadRight(10));
                foreach (var name in productionNames)
                    Console.Write(utilities[name].ToString("F3").PadRight(20));
                Console.WriteLine();
            }
        }

        Console.WriteLine("\n=======================================\n");
    }
}
