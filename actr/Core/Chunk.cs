public class Chunk
{
  public string Name { get; }
  public string ChunkType { get; }
  private readonly Dictionary<string, object?> _slots;

  private readonly List<DateTime> _references = new List<DateTime>();

  public Chunk(string name, string chunkType, Dictionary<string, object?>? slots = null) {
    Name = name;
    ChunkType = chunkType;
    _slots = slots ?? new Dictionary<string, object?>();
  }

  public void RecordReference(DateTime? at = null)
  {
    _references.Add(at ?? DateTime.UtcNow);
  }

  public double GetActivation(double decay = 0.5, DateTime? now = null)
  {
    if (_references.Count == 0)
    {
      return double.NegativeInfinity;
    }

    var t = now ?? DateTime.UtcNow;

    var sum = _references
      .Select(refTime => (t - refTime).TotalSeconds)
      .Where(seconds => seconds > 0)
      .Sum(seconds => Math.Pow(seconds, -decay));

    if (sum <= 0)
      return double.NegativeInfinity;
    
    return Math.Log(sum);
  }

  public object? GetSlot(string slotName) {
    _slots.TryGetValue(slotName, out var value);
    return value;
  }

  public void SetSlot(string slotName, object? value) {
    _slots[slotName] = value;
  }

  public bool HasSlot(string slotName) => _slots.ContainsKey(slotName);

  public IEnumerable<string> SlotNames => _slots.Keys;

  public override string ToString()
  {
    var slotPairs = _slots.Select(kv => $"{kv.Key}={kv.Value}");
    var slotText = string.Join(", ", slotPairs);
    return $"[{Name} : {ChunkType} | {slotText}]";
  }
}
