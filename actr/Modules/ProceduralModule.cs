class ProceduralModule
{
  private const double NoiseScale = 0.1;
  private readonly List<Production> _productions = new List<Production>();
  private readonly Buffers _buffers;
  private readonly Random _random = new Random();
  public Production? LastFired { get; private set; }

  public ProceduralModule (Buffers b) { _buffers = b; }

  public void AddProduction (Production p) => _productions.Add(p);

  public bool SelectAndFire ()
  {
    var conflictSet = _productions
      .Where(p => p.Matches(_buffers) == true)
      .ToList();

    if (conflictSet.Count == 0)
    {
      Console.WriteLine("[Procedural] Нет подходящих продукций.");
      return false;
    }

    // Лог conflict set
    var names = string.Join(", ", conflictSet.Select(p => $"  {p.Name} (U={p.Utility})"));
    Console.WriteLine($"[Procedural] Conflict set: [{names}]");

    Production? selected = null;
    double bestScore = double.NegativeInfinity;

    foreach (var p in conflictSet)
    {
      var noise = (_random.NextDouble() - 0.5) * NoiseScale;
      var score = p.Utility + noise;

      if (score > bestScore)
      {
        bestScore = score;
        selected = p;
      }
    }

    // Select: сейчас — первая подходящая
    // Этап 5: заменим на выбор по utility
    Console.WriteLine($"[Procedural] Выбрана: \"{selected!.Name}\"");

    // Fire
    selected.Fire(_buffers);

    return true;
  }

  public void RewardLastFired (double reward, double alpha = 0.2)
  {
    if (LastFired == null)
    {
      Console.WriteLine("[Procedural] Нет продукции для награждения.");
      return;
    }

    LastFired.UpdateUtility(reward, alpha);
  }

  public Dictionary<string, double> GetUtilities() => _productions.ToDictionary(p => p.Name, p => p.Utility);
}