class ProceduralModule
{
  private readonly List<Production> _productions = new List<Production>();
  private readonly Buffers _buffers;

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
    var names = string.Join(", ", conflictSet.Select(p => $"\"{p.Name}\""));
    Console.WriteLine($"[Procedural] Conflict set: [{names}]");

    // Select: сейчас — первая подходящая
    // Этап 5: заменим на выбор по utility
    var selected = conflictSet.First();
    Console.WriteLine($"[Procedural] Выбрана: \"{selected.Name}\"");

    // Fire
    selected.Fire(_buffers);

    return true;
  }
}