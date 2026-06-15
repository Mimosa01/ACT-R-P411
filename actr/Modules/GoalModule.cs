public class GoalModule
{
  private Buffers _buffers;
  public GoalModule (Buffers b) { _buffers = b; }

  public void SetGoal (Chunk goal) { 
    Console.WriteLine($"[GoalModule] Установлена цель: {goal}");
    _buffers.Goal.Set(goal); 
  }

  public void ClearGoal () { 
    Console.WriteLine("[GoalModule] Цель очищена.");
    _buffers.Goal.Clear(); 
  }
}