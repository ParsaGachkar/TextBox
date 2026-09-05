namespace TextBox.Services;

public sealed class MessageStoreOptions
{
    public const string SectionName = "MessageStore";

    public string Path { get; set; } = "Data/textbox.db";
}