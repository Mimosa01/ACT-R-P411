using actr.Core;

// ============================================================
// Демо: Этап 3 — Буферы и модули
//
// Полный цикл: цель → запрос → результат — всё через буферы.
// Прямых обращений к памяти из основного кода нет.
// ============================================================

Console.WriteLine("=== ACT-R Агент. Этап 3: Буферы и модули ===\n");

// ── Инициализация памяти ───────────────────────────────────

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
    // Добавляем историю чтобы активация работала
    chunk.RecordReference(DateTime.UtcNow.AddSeconds(-10));
    memory.Add(chunk);
}

// ── Сборка архитектуры ─────────────────────────────────────
//
// Buffers создаётся один раз и передаётся во все модули.
// Это единственный экземпляр — все пишут и читают одни и те же буферы.

var buffers = new Buffers();
var goalModule = new GoalModule(buffers);
var dm         = new DeclarativeModule(memory, buffers);

// ── Сценарий 1: успешное вспоминание ──────────────────────

Console.WriteLine("\n--- Сценарий 1: найти 3 * 4 ---\n");

// Шаг 1: установить цель
goalModule.SetGoal(new Chunk(
    name: "current-goal",
    chunkType: "find-product",
    slots: new Dictionary<string, object?>
    {
        ["multiplicand"] = 3,
        ["multiplier"]   = 4
    }
));

// Шаг 2: прочитать цель из буфера (не из локальной переменной!)
var currentGoal = buffers.Goal.Get();

if (!buffers.Goal.IsEmpty() && currentGoal != null)
{
    var a = currentGoal.GetSlot("multiplicand");
    var b = currentGoal.GetSlot("multiplier");

    Console.WriteLine($"\nЧитаем из Goal buffer: {a} * {b} = ?");

    // Шаг 3: запросить память через модуль
    dm.RequestRetrieval(
        chunkType: "multiplication-fact",
        request: new Dictionary<string, object?>
        {
            ["multiplicand"] = a,
            ["multiplier"]   = b
        }
    );
}

// Шаг 4: прочитать результат из Retrieval buffer
Console.WriteLine($"\nRetrieval buffer пуст: {buffers.Retrieval.IsEmpty}");

if (!buffers.Retrieval.IsEmpty())
{
    var retrieved = buffers.Retrieval.Get()!;
    Console.WriteLine($"Ответ: {currentGoal?.GetSlot("multiplicand")} * " +
                      $"{currentGoal?.GetSlot("multiplier")} = " +
                      $"{retrieved.GetSlot("product")}");
}

// ── Сценарий 2: retrieval failure ─────────────────────────

Console.WriteLine("\n--- Сценарий 2: retrieval failure (7 * 8) ---\n");

goalModule.SetGoal(new Chunk(
    name: "current-goal-2",
    chunkType: "find-product",
    slots: new Dictionary<string, object?>
    {
        ["multiplicand"] = 7,
        ["multiplier"]   = 8
    }
));

var goal2 = buffers.Goal.Get();
if (!buffers.Goal.IsEmpty() && goal2 != null)
{
    dm.RequestRetrieval(
        chunkType: "multiplication-fact",
        request: new Dictionary<string, object?>
        {
            ["multiplicand"] = goal2.GetSlot("multiplicand"),
            ["multiplier"]   = goal2.GetSlot("multiplier")
        }
    );
}

Console.WriteLine($"\nRetrieval buffer пуст: {buffers.Retrieval.IsEmpty}");
Console.WriteLine(buffers.Retrieval.IsEmpty()
    ? "Агент не знает ответа (retrieval failure)."
    : $"Найдено: {buffers.Retrieval.Get()}");

// ── Итог ──────────────────────────────────────────────────

Console.WriteLine("\n=== Что увидели ===");
Console.WriteLine("Buffer          → один chunk, Set/Clear/Get/IsEmpty");
Console.WriteLine("Buffers         → один объект на всю систему");
Console.WriteLine("GoalModule      → пишет в Goal buffer");
Console.WriteLine("DeclarativeModule → пишет в Retrieval buffer, сам ничего не возвращает");
Console.WriteLine("Retrieval failure → IsEmpty == true, продукция это обработает");
Console.WriteLine("\nДальше (Этап 4): продукции — IF <буферы> THEN <действие>.");
