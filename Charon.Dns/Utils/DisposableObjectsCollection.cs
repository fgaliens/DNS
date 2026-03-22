namespace Charon.Dns.Utils;

public class DisposableObjectsCollection<T>(int count = 0) : IDisposable where T : IDisposable
{
    private readonly List<T> _items = new(count);
    private bool _disposed;

    public IList<T> Collection => _items;
    
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        _disposed = true;
        
        foreach (var item in _items)
        {
            item.Dispose();
        }
    }
}
