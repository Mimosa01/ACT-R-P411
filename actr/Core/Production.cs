class Production
{
  public readonly string Name;
  private readonly Func<Buffers, bool> _condition;
  private readonly Action<Buffers> _action;

  public Production (string name, Func<Buffers, bool> condition, Action<Buffers> action)
  {
    Name = name;
    _condition = condition;
    _action = action;
  }

  public bool Matches (Buffers buffers) => _condition(buffers);

  public void Fire (Buffers buffers)
  {
    Console.WriteLine($"[Production] Срабатывает: \"{Name}\"");
    _action(buffers);
  }
}