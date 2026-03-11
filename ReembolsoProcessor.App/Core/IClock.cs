namespace ReembolsoProcessor.App.Core;

public interface IClock
{
    DateOnly Today { get; }
}
