namespace HttpTrafficProxy.Services.TestHelpers;

public sealed class TempFolder : IDisposable
{
    public string TestPath { get; }

    public TempFolder()
    {
        TestPath = Path.Combine(Path.GetTempPath(), "HttpTrafficProxyTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TestPath);
    }

    public string Combine(params string[] parts) => Path.Combine([TestPath, .. parts]);

    public void Dispose()
    {
        try
        {
            Directory.Delete(TestPath, recursive: true);
        } catch { /* ignore */ }
    }
}
