using actr.Core;

// ============================================================
// Демо: Этап 1 — Chunk и DeclarativeMemory
// 
// Агент "знает" факты умножения.
// Мы загружаем их в DeclarativeMemory и делаем запросы.
// ============================================================

Console.WriteLine("=== ACT-R Агент. Этап 1: Chunk и DeclarativeMemory ===\n");

var memory = new DeclarativeMemory();

// Загружаем факты умножения в память.
// Теория: это как будто агент когда-то выучил таблицу умножения,
// и теперь эти знания хранятся в его декларативной памяти.
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

Console.WriteLine($"\nЗагружено в память: {memory.Count} chunk'ов\n");

// ---- Запросы (Retrieval) ----
Console.WriteLine("--- Запросы к памяти ---\n");

// Запрос 1: что такое 3 * 4?
Console.Write("Вопрос: 3 * 4 = ? → ");
var answer1 = memory.Retrieve(
    chunkType: "multiplication-fact",
    request: new Dictionary<string, object?>
    {
        ["multiplicand"] = 3,
        ["multiplier"]   = 4
    }
);
Console.WriteLine($"Ответ: {answer1?.GetSlot("product") ?? "не знаю"}\n");

// Запрос 2: какой факт даёт произведение 8?
Console.Write("Вопрос: что умножается и даёт 8? → ");
var answer2 = memory.Retrieve(
    chunkType: "multiplication-fact",
    request: new Dictionary<string, object?>
    {
        ["product"] = 8
    }
);
if (answer2 != null)
    Console.WriteLine($"Ответ: {answer2.GetSlot("multiplicand")} * {answer2.GetSlot("multiplier")}\n");

// Запрос 3: retrieval failure — факта нет в памяти
Console.Write("Вопрос: 7 * 8 = ? → ");
var answer3 = memory.Retrieve(
    chunkType: "multiplication-fact",
    request: new Dictionary<string, object?>
    {
        ["multiplicand"] = 7,
        ["multiplier"]   = 8
    }
);
Console.WriteLine($"Ответ: {answer3?.GetSlot("product") ?? "не знаю (retrieval failure)"}\n");

// ---- Итог ----
Console.WriteLine("=== Что увидели ===");
Console.WriteLine("Chunk      → единица знания с типом и слотами");
Console.WriteLine("Retrieve   → поиск по паттерну (тип + частичные слоты)");
Console.WriteLine("Failure    → агент может НЕ вспомнить — это нормально в ACT-R");
Console.WriteLine("\nДальше (Этап 2): каждый chunk получит activation,");
Console.WriteLine("и Retrieve будет выбирать самый 'активный' из подходящих.");
