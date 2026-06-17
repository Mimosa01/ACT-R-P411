class Production
{
  public readonly string Name;
  private readonly Func<Buffers, bool> _condition;
  private readonly Action<Buffers> _action;
  public double Utility { get; private set; }

  public Production (
    string name, 
    Func<Buffers, bool> condition, 
    Action<Buffers> action,
    double initialUtility = 0.0
    )
  {
    Name = name;
    Utility = initialUtility;
    _condition = condition;
    _action = action;
  }

  public bool Matches (Buffers buffers) => _condition(buffers);

  public void Fire (Buffers buffers)
  {
    Console.WriteLine($"[Production] Срабатывает: \"{Name}\"");
    _action(buffers);
  }

  public void UpdateUtility (double reward, double alpha = 0.2)
  {
    var old = Utility;
    Utility = Utility + alpha * (reward - Utility);
    Console.WriteLine($"Production {Name} utility {old} -> {Utility}");
  }
}