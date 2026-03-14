# JC.Communication: Messaging — Guide

Covers thread management, sending messages, reply-to, per-user deletion, group chat participant management, thread metadata, and read tracking. See [Setup](Messaging-Setup.md) for registration.

## Chat threads

### Creating a thread

The simplest way to start a conversation is `GetOrCreateDefaultChat`, which returns the existing default thread for a participant set or creates one if none exists:

```csharp
public class ChatController(ChatThreadService threadService) : Controller
{
    public async Task<IActionResult> StartConversation(string otherUserId)
    {
        var params = new ChatThreadParams("My Chat");
        var participants = new[]
        {
            new ChatParticipant(otherUserId) // thread ID is assigned during creation
        };

        var (chat, response) = await threadService.GetOrCreateDefaultChat(params, participants);

        if (chat == null)
            return BadRequest(response.ErrorMessage);

        return RedirectToAction("Thread", new { id = chat.ThreadId });
    }
}
```

The current user is automatically added as a participant — you only need to supply the other user IDs. If a default thread already exists for this participant set, it is returned without creating a duplicate.

**Nuance:** When `PreventDuplicateChatThreads` is `true` (the default), you cannot create a second thread for the same participant set. To create additional threads, set the option to `false` and use `CreateAndGetNewChat` instead.

### Creating a thread with full control

Use `TryCreateChat` when you need to set metadata, control the default flag, or handle validation errors granularly:

```csharp
var thread = new ChatThread
{
    Name = "Project Alpha",
    Description = "Discussion for Project Alpha",
    IsDefaultThread = true,
    IsGroupThread = true
};

var metadata = new ChatMetadata
{
    Icon = "bi-briefcase",
    ColourHex = "#4A90D9"
};

var participants = new List<ChatParticipant>
{
    new(userIdA),
    new(userIdB),
    new(userIdC)
};

var response = await threadService.TryCreateChat(thread, metadata, participants);

if (!response.IsValid)
    // response.ErrorMessage contains accumulated validation errors
    return BadRequest(response.ErrorMessage);
```

All validation errors (participant, metadata, and thread) are accumulated into the final response — you do not need to check each individually.

### Querying threads

Retrieve all threads for the current user:

```csharp
// All threads — returns List<ChatModel>
var chats = await threadService.GetUserChats();

// Paginated — returns IPagination<ChatModel>
var pagedChats = await threadService.GetUserChats(pageNumber: 1, pageSize: 20);
```

Retrieve a specific thread by ID:

```csharp
var chat = await threadService.GetChatModelById(threadId);
if (chat == null)
    return NotFound();
```

Both `GetChatModelById` and `GetDefaultUserChat` automatically log a read event for the most recent message when the thread is loaded.

### ChatModel projections

All query methods return `ChatModel` — a read-only projection that excludes soft-deleted messages, participants, and metadata. Key properties:

- `ThreadId`, `ChatName`, `ChatDescription`
- `IsGroupChat` — `true` when more than two participants
- `LastActivity` — formatted timestamp string, or `"Never"` if no activity
- `Messages` — `List<MessageModel>` (non-deleted only)
- `Participants` — `List<ParticipantModel>` (non-deleted only)
- `ChatMetadata` — `MetadataModel` or `null` if deleted

**Nuance:** If the user's `CanSeeHistory` is `false`, messages before their `JoinedAtUtc` are automatically filtered out of the `ChatModel`.

### ChatThreadParams

Controls thread creation and query behaviour:

```csharp
// Minimal — just a name
var params = new ChatThreadParams("Chat Name");

// Full control
var params = new ChatThreadParams(
    name: "Chat Name",
    description: "Optional description",
    dateFormat: "dd/MM/yyyy HH:mm",
    preferHexCode: true,
    asNoTracking: true,
    deletedQueryType: DeletedQueryType.OnlyActive
);
```

If `name` is null or whitespace, it defaults to `"Direct Message"` for two participants or `"Group Chat"` for three or more.

## Sending messages

### Basic send

```csharp
public class MessageController(ChatMessageService messageService) : Controller
{
    public async Task<IActionResult> Send(string threadId, string message)
    {
        var response = await messageService.TrySendMessage(threadId, message, replyToId: null);

        if (!response.IsValid)
            return BadRequest(response.ErrorMessage);

        return Ok();
    }
}
```

Sending a message automatically updates the thread's `LastActivityUtc` and logs the activity.

### Reply-to

Pass the ID of the message being replied to:

```csharp
var response = await messageService.TrySendMessage(threadId, "Sounds good!", replyToId: originalMessageId);
```

The `ReplyToMessageId` is stored on the message and included in `MessageModel` projections. The reply-to message itself is not eagerly loaded — it is resolved from the thread's message list at display time.

### Editing a message

Only the sender can edit their own messages:

```csharp
var response = await messageService.TryEditMessage(threadId, messageId, "Updated message text");

if (!response.IsValid)
    return BadRequest(response.ErrorMessage);
```

### Deleting and restoring messages

```csharp
// Soft-delete a single message (sender only)
var response = await messageService.TryDeleteMessage(threadId, messageId);

// Restore a soft-deleted message (sender only)
var response = await messageService.TryRestoreMessage(threadId, messageId);
```

Restoring an already-active message returns success without changes.

### Bulk operations

```csharp
// Delete all of my messages in a thread
await messageService.TryDeleteAllMyMessages(threadId);

// Restore all of my deleted messages in a thread
await messageService.TryRestoreAllMyDeletedMessages(threadId);
```

**Unsafe variants** affect messages from all participants — use with caution (e.g. admin operations):

```csharp
await messageService.TryDeleteAllMessages(threadId);
await messageService.TryRestoreAllDeletedMessages(threadId);
```

## Thread deletion

### Per-user deletion

Per-user deletion hides the thread from one user without affecting others. Internally, a `ThreadDeleted` record is created — soft-deleting that record restores the thread:

```csharp
// Delete the thread for the current user only
await threadService.TryDeleteThreadForUser(threadId);

// Restore it for the current user
await threadService.TryRestoreThreadForUser(threadId);
```

Per-user deleted threads are automatically excluded from `GetUserChats` and `GetChatModelById`. The underlying thread and all messages remain intact for other participants.

**Nuance:** Calling `TryDeleteThreadForUser` when the thread is already deleted for the user returns `true` (idempotent). Similarly, `TryRestoreThreadForUser` returns `true` if the thread is not deleted for the user.

### Delete for all users

Soft-deletes the thread itself, affecting all participants:

```csharp
// Soft-delete the thread for everyone
await threadService.TryDeleteChatThreadForAll(threadId);

// Restore the thread for everyone
await threadService.TryRestoreChatThreadForAll(threadId, DefaultThreadRestoreMode.DemoteExisting);
```

`TryDeleteChatThreadForAll` also creates `ThreadDeleted` records for all participants who do not already have one.

### Restore modes

When restoring a thread that was the default, and another default now exists for the same participants, the `DefaultThreadRestoreMode` controls what happens:

```csharp
// Block the restore if a new default exists
await threadService.TryRestoreChatThreadForAll(threadId, DefaultThreadRestoreMode.Block);

// Demote the existing default, restore this one as default
await threadService.TryRestoreChatThreadForAll(threadId, DefaultThreadRestoreMode.DemoteExisting);

// Restore this thread but strip its default status
await threadService.TryRestoreChatThreadForAll(threadId, DefaultThreadRestoreMode.DemoteRestored);
```

## Promoting a thread to default

```csharp
// Promote — fails if another default exists
var success = await threadService.PromoteChatToDefault(threadId, demoteExisting: false);

// Promote — demotes the existing default
var success = await threadService.PromoteChatToDefault(threadId, demoteExisting: true);
```

Returns `true` if the thread is now the default (including if it already was). Returns `false` if the thread is not found, the user is not a participant, or promotion was blocked.

## Participants

### Adding participants

```csharp
public class ParticipantController(ChatParticipantService participantService) : Controller
{
    // Add by user ID
    public async Task<IActionResult> AddUser(string threadId, string userId)
    {
        var response = await participantService.TryAddParticipantToChat(threadId, userId);

        if (!response.IsValid)
            return BadRequest(response.ErrorMessage);

        return Ok();
    }

    // Add multiple by user IDs
    public async Task<IActionResult> AddUsers(string threadId, List<string> userIds)
    {
        var response = await participantService.TryAddParticipantsToChat(threadId, userIds);

        if (!response.IsValid)
            return BadRequest(response.ErrorMessage);

        return Ok();
    }
}
```

If a participant was previously soft-deleted (removed), they are automatically restored with an updated `JoinedAtUtc` instead of creating a duplicate record.

**Nuance:** When `ImmutableDirectMessageParticipants` is `true` (the default), adding participants to a DM returns an error. This only applies to threads with exactly two participants.

**Nuance:** If the thread is a default thread and the new participants already have a default thread, the current thread is automatically demoted from default status.

### Removing participants

```csharp
// Remove by user ID
var response = await participantService.TryRemoveParticipantFromChat(threadId, userId);

// Remove multiple
var response = await participantService.TryRemoveParticipantsFromChat(threadId, userIdA, userIdB);
```

You cannot remove yourself — use `TryLeaveGroupChat` instead. The post-removal participant state is validated to ensure the thread remains valid (e.g. at least two participants).

### Leaving a group chat

```csharp
var success = await participantService.TryLeaveGroupChat(threadId);
```

Returns `false` if the thread is a direct message — you cannot leave a DM, only delete it.

## Thread metadata

Metadata controls the visual appearance of a thread (icon, image, colours). Each thread has at most one metadata record.

### Creating metadata

```csharp
public class MetadataController(ChatMetadataService metadataService) : Controller
{
    public async Task<IActionResult> SetMetadata(string threadId)
    {
        var metadata = new ChatMetadata
        {
            Icon = "bi-chat-heart",
            ImgPath = "/images/team-alpha.png",
            ColourHex = "#E74C3C",
            ColourRgb = "rgb(231,76,60)"
        };

        var response = await metadataService.TryCreateChatMetadata(threadId, metadata);

        if (!response.IsValid)
            return BadRequest(response.ErrorMessage);

        return Ok();
    }
}
```

If soft-deleted metadata already exists for the thread, it is hard-deleted before creating the new record. If active metadata already exists, an error is returned — use `TryUpdateChatMetadata` instead.

### Updating metadata

```csharp
var metadata = new ChatMetadata
{
    Icon = "bi-star",
    ColourHex = "#27AE60"
};

var response = await metadataService.TryUpdateChatMetadata(threadId, metadata);
```

### Deleting and restoring metadata

```csharp
// Soft-delete
var response = await metadataService.TryDeleteChatMetadata(threadId);

// Restore
var response = await metadataService.TryRestoreChatMetadata(threadId);
```

### Colour preference

`ChatModel` resolves the colour based on the `preferHexCode` parameter passed to query methods. In `MetadataModel`:

- When `preferHexCode` is `true`, `Colour` returns `ColourHex` if set, falling back to `ColourRgb`
- When `false`, `Colour` returns `ColourRgb` if set, falling back to `ColourHex`

## Validation responses

All mutation methods return a typed validation response inheriting from `MessagingValidationResponse`:

```csharp
var response = await messageService.TrySendMessage(threadId, message, null);

if (!response.IsValid)
{
    // response.ErrorMessage — human-readable error
    logger.LogWarning("Send failed: {Error}", response.ErrorMessage);
    return BadRequest(response.ErrorMessage);
}

// Typed responses include the validated entity:
// response.ValidatedChatMessage  — ChatMessageValidationResponse
// response.ValidatedChatThread   — ChatThreadValidationResponse
// response.ValidatedChatMetadata — ChatMetadataValidationResponse
// response.ValidatedParticipants — ParticipantValidationResponse
```

Validation errors are accumulated — when `TryCreateChat` validates participants, metadata, and the thread, all errors from all three stages appear in the final response's `ErrorMessage`.

## Read tracking

Read tracking happens automatically when a thread is loaded via `GetChatModelById` or `GetDefaultUserChat`. Only the most recent message per thread is tracked — this design avoids log churn from cleanup jobs that would otherwise cause old per-message read logs to be recreated on every thread load.

Read tracking can be disabled:

```csharp
builder.Services.AddMessaging<AppDbContext>(options =>
{
    options.LogChatReads = false;
});
```

## Activity logging

Thread activity (message sends, participant additions and removals) is logged to `ThreadActivityLog` automatically. The logging mode is configurable:

```csharp
builder.Services.AddMessaging<AppDbContext>(options =>
{
    // Only log messages, not participant changes
    options.ThreadActivityLoggingMode = ThreadActivityLoggingMode.Message;

    // Disable all activity logging
    options.ThreadActivityLoggingMode = ThreadActivityLoggingMode.None;
});
```

Activity logs include a `ThreadActivityType` enum and an `ActivityDetails` string describing what happened (e.g. "Message from user123", "Participant(s) added: userA, userB").

## Next steps

- [Setup](Messaging-Setup.md) — registration, options, and database configuration.
- [API Reference](Messaging-API.md)
