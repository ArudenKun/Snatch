using Avalonia.Metadata;
using R3.ObservableEvents;

[assembly: GenerateStaticEventObservables(typeof(TaskScheduler))]
[assembly: XmlnsDefinition("https://github.com/arudenkun/Snatch", "Snatch.Converters")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/Snatch", "Snatch.Models")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/Snatch", "Snatch.ViewModels")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/Snatch", "Snatch.ViewModels.Pages")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/Snatch", "Snatch.ViewModels.Dialogs")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/Snatch", "Snatch.Views")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/Snatch", "Snatch.Views.Pages")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/Snatch", "Snatch.Views.Dialogs")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/Snatch", "Snatch.Controls")]
[assembly: XmlnsDefinition("https://github.com/arudenkun/Snatch", "Snatch.Controls.WebView")]
[assembly: XmlnsPrefix("https://github.com/arudenkun/Snatch", "snatch")]
