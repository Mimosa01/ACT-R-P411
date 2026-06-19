using actr.Core;

/// <summary>
/// Продукции для задачи умножения.
///
/// Этап 6: добавлен флаг "retrieval-attempted" в Goal chunk —
/// решение проблемы бесконечного повтора при retrieval failure (Вариант А).
///
/// start-retrieval теперь срабатывает только один раз на цель:
/// после срабатывания выставляет флаг, и условие больше не подходит,
/// даже если Retrieval buffer остался пустым (неудача).
/// </summary>
internal static class ArithmeticProductions
{
    internal static IEnumerable<Production> Create(DeclarativeModule dm, GoalModule gm)
    {
        // ── Продукция 1: start-retrieval ──────────────────────────
        //
        // Условие: цель find-product, retrieval пуст,
        // запрос ещё НЕ был сделан (флага нет)
        yield return new Production(
            name: "start-retrieval",
            initialUtility: 0.5,

            condition: buffers =>
            {
                if (buffers.Goal.IsEmpty()) return false;
                var goal = buffers.Goal.Get()!;
                if (goal.ChunkType != "find-product") return false;
                if (!buffers.Retrieval.IsEmpty()) return false;

                // Ключевая проверка: запрос ещё не делали
                var attempted = goal.GetSlot("retrieval-attempted");
                return attempted is not true;
            },

            action: buffers =>
            {
                var goal = buffers.Goal.Get()!;
                var a    = goal.GetSlot("multiplicand");
                var b    = goal.GetSlot("multiplier");

                Console.WriteLine($"[start-retrieval] Ищу в памяти: {a} * {b}");

                // Помечаем что попытка была — больше start-retrieval не сработает
                // для этой цели, независимо от результата запроса.
                goal.SetSlot("retrieval-attempted", true);

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
        // Условие: цель find-product, retrieval НЕ пуст (нашли факт)
        yield return new Production(
            name: "retrieve-answer",
            initialUtility: 0.5,

            condition: buffers =>
            {
                if (buffers.Goal.IsEmpty())      return false;
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

                gm.ClearGoal();
                buffers.Retrieval.Clear();
            }
        );

        // ── Продукция 3: handle-failure ───────────────────────────
        //
        // Условие: цель find-product, retrieval пуст, попытка УЖЕ была
        // (флаг стоит) — значит это настоящая неудача, не "ещё не пробовали".
        yield return new Production(
            name: "handle-failure",
            initialUtility: 0.5,

            condition: buffers =>
            {
                if (buffers.Goal.IsEmpty())      return false;
                if (!buffers.Retrieval.IsEmpty()) return false;
                var goal = buffers.Goal.Get()!;
                if (goal.ChunkType != "find-product") return false;

                var attempted = goal.GetSlot("retrieval-attempted");
                return attempted is true;
            },

            action: buffers =>
            {
                var goal = buffers.Goal.Get()!;
                var a    = goal.GetSlot("multiplicand");
                var b    = goal.GetSlot("multiplier");

                Console.WriteLine($"[handle-failure] Не знаю ответа: {a} * {b} = ?");
                gm.ClearGoal();
            }
        );

        // ── Продукция 4: lazy-guess (из этапа 5, для conflict set) ──
        yield return new Production(
            name: "lazy-guess",
            initialUtility: 0.5,

            condition: buffers =>
            {
                if (buffers.Goal.IsEmpty()) return false;
                var goal = buffers.Goal.Get()!;
                if (goal.ChunkType != "find-product") return false;
                if (!buffers.Retrieval.IsEmpty()) return false;
                var attempted = goal.GetSlot("retrieval-attempted");
                return attempted is not true;
            },

            action: buffers =>
            {
                Console.WriteLine("[lazy-guess] Пропускаю поиск, ничего не делаю.");
            }
        );
    }
}
