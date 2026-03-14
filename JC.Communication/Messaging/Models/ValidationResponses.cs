using JC.Communication.Messaging.Models.DomainModels;

namespace JC.Communication.Messaging.Models;

/// <summary>
/// Base validation response for messaging operations. Contains the validation result and an optional error message.
/// </summary>
public class MessagingValidationResponse
{
    /// <summary>Gets whether the validation passed.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the error message when validation fails, or <c>null</c> when valid.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Creates a successful validation response.</summary>
    public MessagingValidationResponse()
    {
        IsValid = true;
    }

    /// <summary>
    /// Creates a failed validation response with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The validation error message.</param>
    public MessagingValidationResponse(string errorMessage)
    {
        IsValid = false;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Validation response for participant operations. Contains the validated and prepared participant list on success.
/// </summary>
public class ParticipantValidationResponse : MessagingValidationResponse
{
    /// <summary>Gets the validated participant list. Empty when validation fails.</summary>
    public List<ChatParticipant> ValidatedParticipants { get; } = [];

    /// <summary>Creates a successful validation response with no participants.</summary>
    public ParticipantValidationResponse()
    {
    }

    /// <summary>
    /// Creates a successful validation response with the validated participant list.
    /// </summary>
    /// <param name="participant">The validated and prepared participants.</param>
    public ParticipantValidationResponse(List<ChatParticipant> participant)
    {
        ValidatedParticipants = participant;
    }

    /// <summary>
    /// Creates a failed validation response with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The validation error message.</param>
    public ParticipantValidationResponse(string errorMessage)
        : base(errorMessage)
    {
    }
}

/// <summary>
/// Validation response for chat thread operations. Contains the validated thread entity on success.
/// </summary>
public class ChatThreadValidationResponse : MessagingValidationResponse
{
    /// <summary>Gets the validated chat thread, or <c>null</c> when validation fails.</summary>
    public ChatThread? ValidatedChatThread { get; } = null;

    /// <summary>Creates a successful validation response with no thread.</summary>
    public ChatThreadValidationResponse()
    {
    }

    /// <summary>
    /// Creates a successful validation response with the validated thread.
    /// </summary>
    /// <param name="chatThread">The validated chat thread entity.</param>
    public ChatThreadValidationResponse(ChatThread chatThread)
    {
        ValidatedChatThread = chatThread;
    }

    /// <summary>
    /// Creates a failed validation response with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The validation error message.</param>
    public ChatThreadValidationResponse(string errorMessage)
        : base(errorMessage)
    {
    }
}

/// <summary>
/// Validation response for chat message operations. Contains the validated message entity on success.
/// </summary>
public class ChatMessageValidationResponse : MessagingValidationResponse
{
    /// <summary>Gets the validated chat message, or <c>null</c> when validation fails.</summary>
    public ChatMessage? ValidatedChatMessage { get; } = null;

    /// <summary>Creates a successful validation response with no message.</summary>
    public ChatMessageValidationResponse()
    {
    }

    /// <summary>
    /// Creates a successful validation response with the validated message.
    /// </summary>
    /// <param name="chatMessage">The validated chat message entity.</param>
    public ChatMessageValidationResponse(ChatMessage chatMessage)
    {
        ValidatedChatMessage = chatMessage;
    }

    /// <summary>
    /// Creates a failed validation response with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The validation error message.</param>
    public ChatMessageValidationResponse(string errorMessage)
        : base(errorMessage)
    {
    }
}

/// <summary>
/// Validation response for chat metadata operations. Contains the validated metadata entity on success.
/// </summary>
public class ChatMetadataValidationResponse : MessagingValidationResponse
{
    /// <summary>Gets the validated chat metadata, or <c>null</c> when validation fails.</summary>
    public ChatMetadata? ValidatedChatMetadata { get; } = null;

    /// <summary>Creates a successful validation response with no metadata.</summary>
    public ChatMetadataValidationResponse()
    {
    }

    /// <summary>
    /// Creates a successful validation response with the validated metadata.
    /// </summary>
    /// <param name="chatMetadata">The validated chat metadata entity.</param>
    public ChatMetadataValidationResponse(ChatMetadata chatMetadata)
    {
        ValidatedChatMetadata = chatMetadata;
    }

    /// <summary>
    /// Creates a failed validation response with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The validation error message.</param>
    public ChatMetadataValidationResponse(string errorMessage)
        : base(errorMessage)
    {
    }
}