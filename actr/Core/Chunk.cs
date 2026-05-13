public class Chunk
{
  public string Name { get; }
  public string ChunkType { get; }
  private readonly Dictionary<string, object?> _slots;

  public Chunk(string name, string chunkType, Dictionary<string, object?>? slots = null) {
    Name = name;
    ChunkType = chunkType;
    _slots = slots ?? new Dictionary<string, object?>();
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
