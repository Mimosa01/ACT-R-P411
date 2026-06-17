
// using actr.Core;
// using actr.Testing;
// // ============================================================
// // Демо: полный прогон агента на сгенерированных данных
// //
// // Таблица умножения 1..9 с намеренными "дырами" (15% пар отсутствуют
// // в памяти специально, чтобы проверить retrieval failure не как
// // редкий случай, а как часть систематического теста).
// // ============================================================

// Console.WriteLine("=== ACT-R Агент: полный прогон со статистикой ===\n");

// // ── Шаг 1: сгенерировать данные ──────────────────────────────

// var dataset = DataGenerator.Generate(maxFactor: 9, gapRatio: 0.15, seed: 42);

// Console.WriteLine($"Сгенерировано:");
// Console.WriteLine($"  Известных фактов: {dataset.KnownFacts.Count}");
// Console.WriteLine($"  Дыр (gaps):       {dataset.Gaps.Count}");
// Console.WriteLine($"  Всего задач:      {dataset.AllTestPairs.Count}");

// Console.WriteLine($"\nПримеры дыр: {string.Join(", ", dataset.Gaps.Take(5).Select(g => $"{g.a}*{g.b}"))}");

// // ── Шаг 2: создать агента и загрузить известные факты ────────

// var agent = new ACTRAgent();
// DataGenerator.LoadInto(agent.Memory, dataset, historySeconds: 60);

// Console.WriteLine($"\nЗагружено в память: {agent.Memory.Count} chunk'ов");

// // ── Шаг 3: полный прогон по всем задачам, без построчных логов ──

// Console.WriteLine("\nЗапускаю прогон по всем задачам...");

// var stats = BatchRunner.Run(
//     agent,
//     dataset,
//     rewardCorrect: 1.0,
//     rewardFailure: 0.0,
//     snapshotEvery: 20
// );

// // ── Шаг 4: отчёт ───────────────────────────────────────────

// BatchRunner.PrintReport(stats);

// // ── Шаг 5: ручная проверка нескольких конкретных случаев ────
// // (с подробными логами, чтобы увидеть механику изнутри)

// Console.WriteLine("========== Детальный прогон отдельных случаев ==========\n");

// Console.WriteLine(">>> Known: " +
//     $"{dataset.KnownFacts[0].a} * {dataset.KnownFacts[0].b} = ?\n");
// agent.SetGoal(new Chunk(
//     name: "detail-known",
//     chunkType: "find-product",
//     slots: new Dictionary<string, object?>
//     {
//         ["multiplicand"] = dataset.KnownFacts[0].a,
//         ["multiplier"]   = dataset.KnownFacts[0].b
//     }
// ));
// agent.Run();
// agent.RewardLastAction(1.0);

// Console.WriteLine($"\n>>> Gap: {dataset.Gaps[0].a} * {dataset.Gaps[0].b} = ? (нет в памяти)\n");
// agent.SetGoal(new Chunk(
//     name: "detail-gap",
//     chunkType: "find-product",
//     slots: new Dictionary<string, object?>
//     {
//         ["multiplicand"] = dataset.Gaps[0].a,
//         ["multiplier"]   = dataset.Gaps[0].b
//     }
// ));
// agent.Run();

// // ── Итог ───────────────────────────────────────────────────

// Console.WriteLine("\n=== Что увидели ===");
// Console.WriteLine("DataGenerator   → таблица умножения с управляемой долей дыр (gapRatio)");
// Console.WriteLine("BatchRunner     → массовый прогон без шума в консоли, статистика на выходе");
// Console.WriteLine("Stats           → известные/дыры обрабатываются разными путями,");
// Console.WriteLine("                  это видно по CorrectlySolved vs HandledFailures");
// Console.WriteLine("UtilityHistory  → снимки utility продукций каждые N задач — видно обучение");

using actr.Core;

// ============================================================
// Демо: Этап 5 — Utility и Conflict Resolution
//
// Часть 1: одна награда, смотрим изменение utility
// Часть 2: несколько циклов обучения, utility приближается к 1.0
// Часть 3: conflict set с lazy-guess — влияние utility на выбор
// ============================================================

Console.WriteLine("=== ACT-R Агент. Этап 5: Utility ===\n");

DeclarativeMemory BuildMemory()
{
    var mem = new DeclarativeMemory();
    var facts = new (int a, int b, int result)[]
    {
        (2, 3, 6), (2, 4, 8), (3, 3, 9),
        (3, 4, 12), (4, 4, 16), (5, 6, 30)
    };

    foreach (var (a, b, result) in facts)
    {
        var chunk = new Chunk(
            name: $"mult-{a}-{b}",
            chunkType: "multiplication-fact",
            slots: new Dictionary<string, object?>
            {
                ["multiplicand"] = a,
                ["multiplier"]   = b,
                ["product"]      = result
            }
        );
        chunk.RecordReference(DateTime.UtcNow.AddSeconds(-10));
        mem.Add(chunk);
    }
    return mem;
}

var memory  = BuildMemory();
var buffers = new Buffers();
var goalMod = new GoalModule(buffers);
var dmMod   = new DeclarativeModule(memory, buffers);
var procMod = new ProceduralModule(buffers);

var productions = ArithmeticProductions.Create(dmMod, goalMod).ToList();
foreach (var p in productions)
    procMod.AddProduction(p);

var retrieveAnswer = productions.First(p => p.Name == "retrieve-answer");
var startRetrieval = productions.First(p => p.Name == "start-retrieval");
// var lazyGuess      = productions.First(p => p.Name == "lazy-guess");

// ── Часть 1: одна награда ───────────────────────────────────

Console.WriteLine("\n========== ЧАСТЬ 1: единичная награда ==========\n");

void RunCycle(int a, int b, int goalNum)
{
    goalMod.SetGoal(new Chunk(
        name: $"current-goal-{goalNum}",
        chunkType: "find-product",
        slots: new Dictionary<string, object?>
        {
            ["multiplicand"] = a,
            ["multiplier"]   = b
        }
    ));

    procMod.SelectAndFire(); // ожидаем start-retrieval
    procMod.SelectAndFire(); // ожидаем retrieve-answer
}

Console.WriteLine($"\nretrieve-answer utility до: {retrieveAnswer.Utility:F3}");
RunCycle(3, 4, goalNum: 1);
procMod.RewardLastFired(reward: 1.0);
Console.WriteLine($"retrieve-answer utility после: {retrieveAnswer.Utility:F3}");

// ── Часть 2: несколько циклов обучения ──────────────────────

Console.WriteLine("\n========== ЧАСТЬ 2: цикл обучения (5 итераций) ==========\n");

var pairs = new (int a, int b)[] { (2,3), (2,4), (3,3), (4,4), (5,6) };

for (int i = 0; i < pairs.Length; i++)
{
    Console.WriteLine($"\n--- Итерация {i + 1}: {pairs[i].a} * {pairs[i].b} ---\n");
    RunCycle(pairs[i].a, pairs[i].b, goalNum: 100 + i);
    procMod.RewardLastFired(reward: 1.0);

    Console.WriteLine($"\nUtility после итерации {i + 1}:");
    Console.WriteLine($"  start-retrieval : {startRetrieval.Utility:F3}");
    Console.WriteLine($"  retrieve-answer : {retrieveAnswer.Utility:F3}");
}

// ── Часть 3: conflict set с lazy-guess ──────────────────────

Console.WriteLine("\n========== ЧАСТЬ 3: conflict set (start-retrieval vs lazy-guess) ==========\n");
Console.WriteLine($"start-retrieval utility: {startRetrieval.Utility:F3}");
// Console.WriteLine($"lazy-guess utility:      {lazyGuess.Utility:F3}\n");

Console.WriteLine("Запускаем такт 5 раз подряд — несмотря на шум,");
Console.WriteLine("start-retrieval должен выигрывать почти всегда:\n");

for (int i = 0; i < 5; i++)
{
    Console.WriteLine($"\n--- Попытка {i + 1} ---\n");
    goalMod.SetGoal(new Chunk(
        name: $"conflict-goal-{i}",
        chunkType: "find-product",
        slots: new Dictionary<string, object?>
        {
            ["multiplicand"] = 2,
            ["multiplier"]   = 3
        }
    ));
    procMod.SelectAndFire();

    // Сбросим goal вручную для следующей попытки
    goalMod.ClearGoal();
}

// ── Итог ───────────────────────────────────────────────────

Console.WriteLine("\n=== Что увидели ===");
Console.WriteLine("Utility           → растёт к reward по Rescorla-Wagner");
Console.WriteLine("UpdateUtility     → U += alpha * (R - U), не прямое присваивание");
Console.WriteLine("Шум при выборе    → не мутирует Utility, влияет только на сравнение");
Console.WriteLine("LastFired         → даёт внешнему коду наградить нужную продукцию");
Console.WriteLine("Conflict set      → высокий Utility почти всегда выигрывает у низкого");
Console.WriteLine("\nДальше (Этап 6): ACTRAgent — всё в одном цикле, без ручных тактов.");
