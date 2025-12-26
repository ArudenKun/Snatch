namespace Snatch.Core.Youtube.Tagging;

internal record MusicBrainzRecording(
    string Artist,
    string? ArtistSort,
    string Title,
    string? Album
);
