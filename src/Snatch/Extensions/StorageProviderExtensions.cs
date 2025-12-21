using Avalonia.Platform.Storage;

namespace Snatch.Extensions;

public static class StorageProviderExtensions
{
    extension(IStorageProvider storageProvider)
    {
        public async Task<string?> PromptSaveFilePathAsync(
            IReadOnlyList<FilePickerFileType>? fileTypes = null,
            string defaultFilePath = ""
        )
        {
            var file = await storageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    FileTypeChoices = fileTypes,
                    SuggestedFileName = defaultFilePath,
                    DefaultExtension = Path.GetExtension(defaultFilePath).TrimStart('.'),
                }
            );

            return file?.TryGetLocalPath() ?? file?.Path.ToString();
        }

        public async Task<string?> PromptDirectoryPathAsync(string defaultDirPath = "")
        {
            var result = await storageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    AllowMultiple = false,
                    SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(
                        defaultDirPath
                    ),
                }
            );

            var directory = result.FirstOrDefault();
            if (directory is null)
                return null;

            return directory.TryGetLocalPath() ?? directory.Path.ToString();
        }
    }
}
