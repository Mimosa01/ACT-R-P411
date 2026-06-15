using actr.Core;
internal static class ArithmeticProductions
{
  internal static IEnumerable<Production> Create(DeclarativeModule dm, GoalModule gm)
  {
    // ── Продукция 1: start-retrieval ──────────────────────────
    //
    // Условие: есть цель find-product И память ещё не запрошена
    // Действие: запросить DeclarativeMemory
    yield return new Production(
      name: "start-retrieval",

      condition: buffers =>
      {
        if (buffers.Goal.IsEmpty()) return false;

        var goal = buffers.Goal.Get()!;
        if (goal.ChunkType != "find-product") return false;

        // Ключевая проверка: retrieval ещё пуст —
        // значит запрос ещё не делался
        return buffers.Retrieval.IsEmpty();
      },

      action: buffers =>
      {
        var goal = buffers.Goal.Get()!;
        var a    = goal.GetSlot("multiplicand");
        var b    = goal.GetSlot("multiplier");

        Console.WriteLine($"[start-retrieval] Ищу в памяти: {a} * {b}");

        dm.RequestRetrieval(
          chunkType: "multiplication-fact",
          request: new Dictionary<string, object?>
          {
            ["multiplicand"] = a,
            ["multiplier"]   = b
          }
        );
      }
    );

    // ── Продукция 2: retrieve-answer ──────────────────────────
    //
    // Условие: есть цель find-product И в retrieval буфере есть результат
    // Действие: вывести ответ, очистить цель
    yield return new Production(
      name: "retrieve-answer",

      condition: buffers =>
      {
        if (buffers.Goal.IsEmpty())     return false;
        if (buffers.Retrieval.IsEmpty()) return false;

        var goal = buffers.Goal.Get()!;
        return goal.ChunkType == "find-product";
      },

      action: buffers =>
      {
        var goal      = buffers.Goal.Get()!;
        var retrieved = buffers.Retrieval.Get()!;

        var a       = goal.GetSlot("multiplicand");
        var b       = goal.GetSlot("multiplier");
        var product = retrieved.GetSlot("product");

        Console.WriteLine($"[Answer] {a} * {b} = {product}");

        // Цель достигнута — очищаем
        gm.ClearGoal();
      }
    );
  }
}
