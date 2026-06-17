using actr.Core;

namespace actr.Testing;

/// <summary>
/// Генератор данных для тестирования агента.
///
/// Создаёт таблицу умножения 1..N с намеренными "дырами" —
/// парами, которых НЕТ в памяти. Это даёт возможность проверить
/// retrieval failure не как редкое исключение, а как системную часть теста.
/// </summary>
public static class DataGenerator
{
    public class Dataset
    {
        // Все пары, которые реально загружены в память (есть ответ)
        public List<(int a, int b, int result)> KnownFacts { get; } = new();

        // Пары, которые умышленно НЕ загружены — для проверки retrieval failure
        public List<(int a, int b)> Gaps { get; } = new();

        // Полный список пар для прогона (known + gaps вместе, в случайном порядке)
        public List<(int a, int b)> AllTestPairs { get; } = new();
    }

    /// <summary>
    /// Сгенерировать таблицу умножения от 1 до maxFactor с дырами.
    /// gapRatio — доля пар, которые нужно исключить из памяти (0.0 - 1.0).
    /// seed — для повторяемости результатов между запусками.
    /// </summary>
    public static Dataset Generate(int maxFactor = 9, double gapRatio = 0.15, int seed = 42)
    {
        var rnd = new Random(seed);
        var dataset = new Dataset();

        var allPairs = new List<(int a, int b)>();
        for (int a = 1; a <= maxFactor; a++)
            for (int b = a; b <= maxFactor; b++) // b >= a — без дублей вида (2,3)/(3,2)
                allPairs.Add((a, b));

        // Перемешиваем, чтобы дыры были не только "в конце таблицы"
        Shuffle(allPairs, rnd);

        int gapCount = (int)(allPairs.Count * gapRatio);

        for (int i = 0; i < allPairs.Count; i++)
        {
            var (a, b) = allPairs[i];

            if (i < gapCount)
            {
                dataset.Gaps.Add((a, b));
            }
            else
            {
                dataset.KnownFacts.Add((a, b, a * b));
            }
        }

        // Финальный список для прогона — known + gaps, снова перемешанные,
        // чтобы агент не мог угадать паттерн "сначала всё известное, потом дыры"
        dataset.AllTestPairs.AddRange(dataset.KnownFacts.Select(f => (f.a, f.b)));
        dataset.AllTestPairs.AddRange(dataset.Gaps);
        Shuffle(dataset.AllTestPairs, rnd);

        return dataset;
    }

    /// <summary>
    /// Загрузить известные факты из датасета в DeclarativeMemory.
    /// historySeconds — насколько "давно" симулируется первое обращение
    /// (нужно чтобы GetActivation не возвращал NegativeInfinity сразу).
    /// </summary>
    public static void LoadInto(DeclarativeMemory memory, Dataset dataset, double historySeconds = 60)
    {
        foreach (var (a, b, result) in dataset.KnownFacts)
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
            chunk.RecordReference(DateTime.UtcNow.AddSeconds(-historySeconds));
            memory.Add(chunk);
        }
    }

    private static void Shuffle<T>(List<T> list, Random rnd)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
