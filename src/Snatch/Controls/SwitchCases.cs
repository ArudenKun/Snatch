using Avalonia;
using Avalonia.Collections;
using Snatch.Converters;

namespace Snatch.Controls;

public class SwitchCases : AvaloniaList<SwitchCase>
{
    internal SwitchCase? EvaluateCases(object? value, Type targetType)
    {
        if (Count == 0)
            return null;

        return this.FirstOrDefault(@case =>
                RelayConverter.CompareValues(value, @case.Value, targetType)
            )
            ?? this.FirstOrDefault(x => x.Value == AvaloniaProperty.UnsetValue)
            ?? this.FirstOrDefault();
    }
}
