using actr.Core;

class DeclarativeModule
{
  private readonly DeclarativeMemory _memory;
  private readonly Buffers _buffers;
  public DeclarativeModule (DeclarativeMemory m, Buffers b)
  {
    _memory = m;
    _buffers = b;
  }

  public void RequestRetrieval(
    string? chunkType = null, 
    Dictionary<string, object?>? request = null
  )
  {
    _buffers.Retrieval.Clear();

    var result = _memory.Retrieve(chunkType, request);

    if (result != null)
    {
      _buffers.Retrieval.Set(result);
    }
  }
}