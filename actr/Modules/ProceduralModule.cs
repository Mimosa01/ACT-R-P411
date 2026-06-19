using actr.Core;

/// <summary>
/// Procedural Module — реализует цикл Match–Select–Fire.
///
/// Этап 5: Select теперь учитывает Utility + случайный шум.
/// Шум нужен чтобы система иногда пробовала менее полезные продукции,
/// а не зацикливалась навсегда на одном правиле.
/// </summary>
class ProceduralModule
{
    private readonly List<Production> _productions = new();
    private readonly Buffers _buffers;
    private readonly Random _random = new();

    // Константа шума. Чем больше — тем чаще выбор "ошибается"
    // в сторону менее полезной продукции.
    private const double NoiseScale = 0.1;

    public Production? LastFired { get; set; }

    public ProceduralModule(Buffers buffers)
    {
        _buffers = buffers;
    }

    public void AddProduction(Production p) => _productions.Add(p);

    /// <summary>
    /// Один когнитивный такт: Match → Select (с шумом) → Fire.
    /// </summary>
    public bool SelectAndFire()
    {
        var conflictSet = _productions
            .Where(p => p.Matches(_buffers))
            .ToList();

        if (conflictSet.Count == 0)
        {
            Console.WriteLine("[Procedural] Нет подходящих продукций.");
            return false;
        }

        // Лог conflict set с текущими utility (без шума — для прозрачности)
        var names = string.Join(", ", conflictSet.Select(p => $"\"{p.Name}\" (U={p.Utility:F3})"));
        Console.WriteLine($"[Procedural] Conflict set: [{names}]");

        // Select: utility + шум, без мутации самого Utility
        Production? selected = null;
        double bestScore = double.NegativeInfinity;

        foreach (var p in conflictSet)
        {
            var noise = (_random.NextDouble() - 0.5) * NoiseScale;
            var score = p.Utility + noise;

            Console.WriteLine($"  -> \"{p.Name}\": U={p.Utility:F3} + noise={noise:F3} = {score:F3}");

            if (score > bestScore)
            {
                bestScore = score;
                selected  = p;
            }
        }

        Console.WriteLine($"[Procedural] Выбрана: \"{selected!.Name}\"");

        selected.Fire(_buffers);
        LastFired = selected; // обновляем после успешного Fire

        return true;
    }

    /// <summary>
    /// Наградить последнюю сработавшую продукцию.
    /// Используется внешним кодом после получения результата.
    /// </summary>
    public void RewardLastFired(double reward, double alpha = 0.2)
    {
        if (LastFired == null)
        {
            Console.WriteLine("[Procedural] Нет продукции для награждения.");
            return;
        }

        LastFired.UpdateUtility(reward, alpha);
    }

    /// <summary>
    /// Текущие utility всех продукций — для диагностики (Этап 6).
    /// </summary>
    public Dictionary<string, double> GetUtilities() =>
        _productions.ToDictionary(p => p.Name, p => p.Utility);
}
