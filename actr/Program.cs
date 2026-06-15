
using actr.Core;

// ============================================================
// Демо: Этап 4 — Продукции и процедурный модуль
//
// Ручной цикл из трёх тактов.
// Такт 1: start-retrieval
// Такт 2: retrieve-answer
// Такт 3: нет подходящих продукций
// ============================================================

Console.WriteLine("=== ACT-R Агент. Этап 4: Продукции ===\n");

// ── Память ─────────────────────────────────────────────────

var memory = new DeclarativeMemory();

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
    memory.Add(chunk);
}

// ── Сборка архитектуры ─────────────────────────────────────

var buffers   = new Buffers();
var goalMod   = new GoalModule(buffers);
var dmMod     = new DeclarativeModule(memory, buffers);
var procMod   = new ProceduralModule(buffers);

foreach (var p in ArithmeticProductions.Create(dmMod, goalMod))
    procMod.AddProduction(p);

// ── Установить цель ────────────────────────────────────────

Console.WriteLine("\n--- Установка цели ---\n");

goalMod.SetGoal(new Chunk(
    name: "current-goal",
    chunkType: "find-product",
    slots: new Dictionary<string, object?>
    {
        ["multiplicand"] = 3,
        ["multiplier"]   = 4
    }
));

// ── Ручной цикл тактов ─────────────────────────────────────

Console.WriteLine("\n--- Такт 1 ---\n");
procMod.SelectAndFire();

Console.WriteLine("\n--- Такт 2 ---\n");
procMod.SelectAndFire();

Console.WriteLine("\n--- Такт 3 ---\n");
procMod.SelectAndFire();

// ── Итог ───────────────────────────────────────────────────

Console.WriteLine("\n=== Что увидели ===");
Console.WriteLine("Production      → имя + Func<Buffers,bool> + Action<Buffers>");
Console.WriteLine("Matches         → проверяет условие на текущих буферах");
Console.WriteLine("SelectAndFire   → Match → Select → Fire, один такт");
Console.WriteLine("Conflict set    → все подошедшие; сейчас берём первую");
Console.WriteLine("Такт 1          → start-retrieval (retrieval пуст)");
Console.WriteLine("Такт 2          → retrieve-answer (retrieval заполнен)");
Console.WriteLine("Такт 3          → пусто (goal очищен, продукции не подходят)");
Console.WriteLine("\nДальше (Этап 5): utility — агент учится выбирать лучшую продукцию.");
