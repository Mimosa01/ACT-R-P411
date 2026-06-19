using actr.Core;

/// <summary>
/// ACTRAgent — фасад над всей архитектурой.
///
/// Владеет памятью, буферами, модулями и продукциями.
/// Снаружи доступны три действия: загрузить факты (через Memory),
/// поставить цель (SetGoal), запустить цикл (Run).
///
/// Это интеграция этапов 1-5 в единый работающий агент.
/// </summary>
public class ACTRAgent
{
    public DeclarativeMemory Memory { get; }

    private readonly Buffers _buffers;
    private readonly GoalModule _goalModule;
    private readonly DeclarativeModule _declarativeModule;
    private readonly ProceduralModule _proceduralModule;

    public ACTRAgent()
    {
        Memory = new DeclarativeMemory();
        _buffers = new Buffers();

        _goalModule        = new GoalModule(_buffers);
        _declarativeModule = new DeclarativeModule(Memory, _buffers);
        _proceduralModule  = new ProceduralModule(_buffers);

        foreach (var p in ArithmeticProductions.Create(_declarativeModule, _goalModule))
            _proceduralModule.AddProduction(p);
    }

    /// <summary>
    /// Результат одного прогона Run() — для сбора статистики (Testing).
    /// </summary>
    public enum RunOutcome { Solved, Stuck, CycleLimitExceeded }

    public record RunResult(RunOutcome Outcome, int Cycles, string? LastFiredProduction);

    /// <summary>
    /// Установить новую цель агенту.
    /// </summary>
    public void SetGoal(Chunk goal) => _goalModule.SetGoal(goal);

    /// <summary>
    /// Запустить когнитивный цикл до завершения цели,
    /// до отсутствия подходящих продукций, или до лимита тактов.
    /// </summary>
    public void Run(int maxCycles = 20)
    {
        for (int cycle = 1; cycle <= maxCycles; cycle++)
        {
            if (_buffers.Goal.IsEmpty())
            {
                Console.WriteLine($"[Agent] Цель достигнута за {cycle - 1} тактов.");
                return;
            }

            Console.WriteLine($"\n--- Такт {cycle} ---\n");
            var fired = _proceduralModule.SelectAndFire();

            if (!fired)
            {
                Console.WriteLine("[Agent] Застрял: нет подходящих продукций, цель не достигнута.");
                return;
            }
        }

        Console.WriteLine($"[Agent] Превышен лимит тактов ({maxCycles}).");
    }

    /// <summary>
    /// Та же логика что Run(), но без логов в консоль и с возвращаемым
    /// структурированным результатом. Используется для массовых прогонов
    /// со статистикой (Testing.BatchRunner), где консольный вывод на
    /// каждую из сотен задач был бы бесполезным шумом.
    /// </summary>
    public RunResult RunWithResult(int maxCycles = 20)
    {
        for (int cycle = 1; cycle <= maxCycles; cycle++)
        {
            if (_buffers.Goal.IsEmpty())
                return new RunResult(RunOutcome.Solved, cycle - 1, _proceduralModule.LastFired?.Name);

            var fired = _proceduralModule.SelectAndFire();

            if (!fired)
                return new RunResult(RunOutcome.Stuck, cycle, _proceduralModule.LastFired?.Name);
        }

        return new RunResult(RunOutcome.CycleLimitExceeded, maxCycles, _proceduralModule.LastFired?.Name);
    }

    /// <summary>
    /// Наградить последнюю сработавшую продукцию.
    /// Вызывается снаружи после Run(), когда известен результат.
    /// </summary>
    public void RewardLastAction(double reward) =>
        _proceduralModule.RewardLastFired(reward);

    /// <summary>
    /// Получить текущие utility всех продукций — для диагностики.
    /// </summary>
    public Dictionary<string, double> GetProductionUtilities() =>
        _proceduralModule.GetUtilities();
}
