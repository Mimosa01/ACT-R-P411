namespace actr.Core;

public class DeclarativeMemory {
  private readonly Dictionary<string, Chunk> _chunks = new();

  public void Add(Chunk chunk) {
    if(_chunks.ContainsKey(chunk.Name)) {
      throw new InvalidOperationException($"Chunk {chunk.Name} уже существует");
    }
    _chunks[chunk.Name] = chunk;
    Console.WriteLine($"[DM] Добавлен chunk {chunk}");
  }

  public Chunk? Retrieve(string? chunkType = null, Dictionary<string, object?>? request = null)
  {
      var candidates = _chunks.Values.AsEnumerable();

      // Фильтр по типу
      if (chunkType != null)
          candidates = candidates.Where(c => c.ChunkType == chunkType);

      // Фильтр по слотам запроса
      if (request != null)
      {
          candidates = candidates.Where(chunk =>
              request.All(kv =>
              {
                  var slotValue = chunk.GetSlot(kv.Key);
                  // Сравниваем через строку — для простоты на этом этапе
                  return slotValue?.ToString() == kv.Value?.ToString();
              })
          );
      }

      // Этап 1: возвращаем первый найденный
      // Этап 2: здесь будет выбор по максимальной активации
      var result = candidates.FirstOrDefault();

      if (result != null)
          Console.WriteLine($"[DM] Retrieval success: {result}");
      else
          Console.WriteLine($"[DM] Retrieval failure — ничего не найдено.");

      return result;
  }

  /// <summary>
  /// Получить chunk по имени напрямую (для диагностики).
  /// </summary>
  public Chunk? GetByName(string name)
  {
      _chunks.TryGetValue(name, out var chunk);
      return chunk;
  }

  public int Count => _chunks.Count;

  public override string ToString() =>
      $"DeclarativeMemory ({_chunks.Count} chunks)";
}
