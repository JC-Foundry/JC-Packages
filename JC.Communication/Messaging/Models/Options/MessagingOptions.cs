namespace JC.Communication.Messaging.Models.Options;

/// <summary>
/// Configuration options for the messaging module. Controls message limits, thread behaviour,
/// participant rules, and logging preferences.
/// </summary>
public class MessagingOptions
{
    /// <summary>Gets or sets the maximum permitted message length in characters. Defaults to 10,000.</summary>
    public ushort MaxMessageLength { get; set; } = 10000;

    /// <summary>Gets or sets whether new participants can see messages sent before they joined. Defaults to <c>true</c>.</summary>
    public bool ParticipantsSeeChatHistory { get; set; } = true;

    /// <summary>
    /// Gets or sets whether duplicate (non-default) threads are prevented for the same participant set.
    /// When <c>true</c>, each participant set can only have a single default thread and no additional threads.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool PreventDuplicateChatThreads { get; set; } = true;

    /// <summary>
    /// Gets or sets whether direct message participant lists are immutable.
    /// When <c>true</c>, participants cannot be added to or removed from a DM. Defaults to <c>true</c>.
    /// </summary>
    public bool ImmutableDirectMessageParticipants { get; set; } = true;

    /// <summary>Gets or sets whether group chats (more than two participants) are disabled. Defaults to <c>false</c>.</summary>
    public bool DisableGroups { get; set; } = false;

    /// <summary>Gets or sets whether message read events are logged. Defaults to <c>true</c>.</summary>
    public bool LogChatReads { get; set; } = true;

    /// <summary>Gets or sets the thread activity types that should be logged. Defaults to <see cref="ThreadActivityLoggingMode.All"/>.</summary>
    public ThreadActivityLoggingMode ThreadActivityLoggingMode { get; set; } = ThreadActivityLoggingMode.All;
}

/// <summary>
/// Flags enum controlling which thread activity types are logged by the messaging log service.
/// </summary>
[Flags]
public enum ThreadActivityLoggingMode
{
    /// <summary>No thread activity is logged.</summary>
    None,

    /// <summary>Message send events are logged.</summary>
    Message,

    /// <summary>Participant addition events are logged.</summary>
    ParticipantAdded,

    /// <summary>Participant removal events are logged.</summary>
    ParticipantRemoved,

    /// <summary>All thread activity types are logged.</summary>
    All = Message | ParticipantAdded | ParticipantRemoved
}