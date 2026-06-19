using actr.Core;
using actr.Testing;

// ============================================================
// Демо: полный прогон агента на сгенерированных данных
//
// Таблица умножения 1..9 с намеренными "дырами" (15% пар отсутствуют
// в памяти специально, чтобы проверить retrieval failure не как
// редкий случай, а как часть систематического теста).
// ============================================================

Console.WriteLine("=== ACT-R Агент: полный прогон со статистикой ===\n");

// ── Шаг 1: сгенерировать данные ──────────────────────────────

var dataset = DataGenerator.Generate(maxFactor: 9, gapRatio: 0.15, seed: 42);

Console.WriteLine($"Сгенерировано:");
Console.WriteLine($"  Известных фактов: {dataset.KnownFacts.Count}");
Console.WriteLine($"  Дыр (gaps):       {dataset.Gaps.Count}");
Console.WriteLine($"  Всего задач:      {dataset.AllTestPairs.Count}");

Console.WriteLine($"\nПримеры дыр: {string.Join(", ", dataset.Gaps.Take(5).Select(g => $"{g.a}*{g.b}"))}");

// ── Шаг 2: создать агента и загрузить известные факты ────────

var agent = new ACTRAgent();
DataGenerator.LoadInto(agent.Memory, dataset, historySeconds: 60);

Console.WriteLine($"\nЗагружено в память: {agent.Memory.Count} chunk'ов");

// ── Шаг 3: полный прогон по всем задачам, без построчных логов ──

Console.WriteLine("\nЗапускаю прогон по всем задачам...");

var stats = BatchRunner.Run(
    agent,
    dataset,
    rewardCorrect: 1.0,
    rewardFailure: 0.0,
    snapshotEvery: 20
);

// ── Шаг 4: отчёт ───────────────────────────────────────────

BatchRunner.PrintReport(stats);

// ── Шаг 5: ручная проверка нескольких конкретных случаев ────
// (с подробными логами, чтобы увидеть механику изнутри)

Console.WriteLine("========== Детальный прогон отдельных случаев ==========\n");

Console.WriteLine(">>> Known: " +
    $"{dataset.KnownFacts[0].a} * {dataset.KnownFacts[0].b} = ?\n");
agent.SetGoal(new Chunk(
    name: "detail-known",
    chunkType: "find-product",
    slots: new Dictionary<string, object?>
    {
        ["multiplicand"] = dataset.KnownFacts[0].a,
        ["multiplier"]   = dataset.KnownFacts[0].b
    }
));
agent.Run();
agent.RewardLastAction(1.0);

Console.WriteLine($"\n>>> Gap: {dataset.Gaps[0].a} * {dataset.Gaps[0].b} = ? (нет в памяти)\n");
agent.SetGoal(new Chunk(
    name: "detail-gap",
    chunkType: "find-product",
    slots: new Dictionary<string, object?>
    {
        ["multiplicand"] = dataset.Gaps[0].a,
        ["multiplier"]   = dataset.Gaps[0].b
    }
));
agent.Run();

// ── Итог ───────────────────────────────────────────────────

Console.WriteLine("\n=== Что увидели ===");
Console.WriteLine("DataGenerator   → таблица умножения с управляемой долей дыр (gapRatio)");
Console.WriteLine("BatchRunner     → массовый прогон без шума в консоли, статистика на выходе");
Console.WriteLine("Stats           → известные/дыры обрабатываются разными путями,");
Console.WriteLine("                  это видно по CorrectlySolved vs HandledFailures");
Console.WriteLine("UtilityHistory  → снимки utility продукций каждые N задач — видно обучение");
