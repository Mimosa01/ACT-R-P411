using actr.Core;
Console.WriteLine("=== ACT-R Агент. Этап 2: Activation ===\n");

var memory = new DeclarativeMemory();

// Загружаем факты умножения
var facts = new (int a, int b, int result)[]
{
    (2, 3, 6), (2, 4, 8), (3, 3, 9),
    (3, 4, 12), (4, 4, 16), (5, 6, 30)
};

foreach (var (a, b, result) in facts)
{
    memory.Add(new Chunk(
        name: $"mult-{a}-{b}",
        chunkType: "multiplication-fact",
        slots: new Dictionary<string, object?>
        {
            ["multiplicand"] = a,
            ["multiplier"]   = b,
            ["product"]      = result
        }
    ));
}

// ── Сценарий: два chunk'а с разной историей ────────────────
//
// Добавим второй факт для 3*4 с другим именем — чтобы оба
// подходили под один запрос и система выбирала по активации.
//
// В реальном ACT-R одинаковых фактов обычно нет, но нам нужно
// показать механизм выбора — поэтому идём на это допущение.

var chunkFrequent = memory.GetByName("mult-3-4")!;  // будет "свежим"
var chunkStale    = new Chunk(
    name: "mult-3-4-old",
    chunkType: "multiplication-fact",
    slots: new Dictionary<string, object?>
    {
        ["multiplicand"] = 3,
        ["multiplier"]   = 4,
        ["product"]      = 12
    }
);
memory.Add(chunkStale);

// Симулируем историю обращений
// chunkFrequent: вспоминали недавно и часто
chunkFrequent.RecordReference(DateTime.UtcNow.AddSeconds(-5));
chunkFrequent.RecordReference(DateTime.UtcNow.AddSeconds(-10));
chunkFrequent.RecordReference(DateTime.UtcNow.AddSeconds(-30));

// chunkStale: вспоминали давно
chunkStale.RecordReference(DateTime.UtcNow.AddSeconds(-1));
chunkStale.RecordReference(DateTime.UtcNow.AddSeconds(-5));
chunkStale.RecordReference(DateTime.UtcNow.AddSeconds(-10));
chunkStale.RecordReference(DateTime.UtcNow.AddSeconds(-30));

Console.WriteLine("\n--- Активации перед запросом ---");
Console.WriteLine($"  {chunkFrequent.Name,-15} activation = {chunkFrequent.GetActivation():F3}");
Console.WriteLine($"  {chunkStale.Name,-15} activation = {chunkStale.GetActivation():F3}");

Console.WriteLine("\n--- Запрос: multiplicand=3, multiplier=4 ---\n");

var answer = memory.Retrieve(
    chunkType: "multiplication-fact",
    request: new Dictionary<string, object?>
    {
        ["multiplicand"] = 3,
        ["multiplier"]   = 4
    }
);

Console.WriteLine($"\nВыбран chunk: {answer?.Name ?? "никто"}");
Console.WriteLine($"Ответ: 3 * 4 = {answer?.GetSlot("product") ?? "?"}");

// ── Retrieval failure ──────────────────────────────────────
Console.WriteLine("\n--- Retrieval failure: факта нет в памяти ---\n");
memory.Retrieve(
    chunkType: "multiplication-fact",
    request: new Dictionary<string, object?>
    {
        ["multiplicand"] = 7,
        ["multiplier"]   = 8
    }
);

// ── Итог ──────────────────────────────────────────────────
Console.WriteLine("\n=== Что увидели ===");
Console.WriteLine("Activation    → чем свежее и чаще обращения, тем выше");
Console.WriteLine("Retrieve      → выбирает chunk с max activation");
Console.WriteLine("RecordRef     → каждый Retrieve поднимает активацию");
Console.WriteLine("NegativeInf   → chunk без истории практически недостижим");
Console.WriteLine("\nДальше (Этап 3): буферы — как модули общаются друг с другом.");
