namespace Snatch.Options;

[AttributeUsage(AttributeTargets.Class)]
public class OptionAttribute : Attribute
{
    public OptionAttribute(string section = "", int order = int.MaxValue)
    {
        Section = section;
        Order = order;
    }

    public string Section { get; }
    public int Order { get; }
}
