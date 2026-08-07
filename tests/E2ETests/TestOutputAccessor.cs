using MartinCostello.Logging.XUnit;

namespace E2ETests;

internal sealed class TestOutputAccessor : ITestOutputHelperAccessor
{
    private readonly Lock _sync = new();
    private ITestOutputHelper? _current;

    public ITestOutputHelper? OutputHelper
    {
        get
        {
            lock (_sync)
            {
                return _current;
            }
        }
        set
        {
            lock (_sync)
            {
                _current = value;
            }
        }
    }
}
