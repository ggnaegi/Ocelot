using Microsoft.IO;

namespace Ocelot.Cache;

public class MemoryStreamManager : IMemoryStreamManager
{
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public MemoryStreamManager()
    {
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
    }

    public MemoryStream GetStream()
    {
        return _recyclableMemoryStreamManager.GetStream();
    }
}
