using ReembolsoProcessor.App.Core;

namespace ReembolsoProcessor.App.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.Date);
}
