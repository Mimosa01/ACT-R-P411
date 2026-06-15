public class Buffer
{
  public string Name;
  private Chunk? _content;

  public Buffer (string n) { Name = n; }

  public void Set (Chunk chunk) { 
    _content = chunk;
    Console.WriteLine($"[Buffer: {Name}] ← {chunk}"); 
  }

  public void Clear () { 
    _content = null; 
    Console.WriteLine($"[Buffer: {Name}] очищен");
  }

  public Chunk? Get () => _content;

  public bool IsEmpty () => _content == null;

  public override string ToString() =>
    IsEmpty() ? $"[Buffer: {Name} | пуст]" : $"[Buffer: {Name} | {_content}]";
}