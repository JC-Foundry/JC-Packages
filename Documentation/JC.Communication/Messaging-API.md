# JC.Communication: Messaging — API reference

Complete reference for all public types in the messaging module. See [Setup](Messaging-Setup.md) for registration and options, and [Guide](Messaging-Guide.md) for usage examples.

> **Note:** Registration extensions (`AddMessaging`, `ConfigureMessagingBackgroundJobs`) and options classes (`MessagingOptions`, `MessagingBackgroundJobOptions`) are documented in [Setup](Messaging-Setup.md), not here.

## Models

### ChatThread

**Namespace:** `JC.Communication.Messaging.Models.DomainModels`

Represents a chat thread (conversation) between two or more participants. Extends `AuditModel` for soft-delete and audit trail support.

#### Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `DirectMessageName` | `"Direct Message"` | Default display name assigned to direct message threads. |
| `GroupChatName` | `"Group Chat"` | Default display name assigned to group chat threads. |

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Unique identifier for the thread. Max length 36. |
| `Name` | `string` | — | get; set; | Display name of the thread. Required. Max length 256. |
| `Description` | `string?` | `null` | get; set; | Optional description for the thread. Max length 1024. |
| `IsDefaultThread` | `bool` | `false` | get; set; | Whether this is the default thread for its participant set. |
| `LastActivityUtc` | `DateTime?` | `null` | get; set; | UTC timestamp of the most recent activity in this thread. |
| `IsGroupThread` | `bool` | `false` | get; set; | Whether this thread is a group chat. Persisted for use when participants are not loaded. |
| `Messages` | `ICollection<ChatMessage>` | — | get; set; | Navigation property to the messages belonging to this thread. |
| `Participants` | `ICollection<ChatParticipant>` | — | get; set; | Navigation property to the participants in this thread. |
| `ChatMetadata` | `ChatMetadata?` | `null` | get; set; | Navigation property to the optional visual metadata for this thread. |
| `UserThreadDeletions` | `ICollection<ThreadDeleted>` | — | get; set; | Navigation property to the per-user deletion records for this thread. |

#### Methods

##### IsGroup()

**Returns:** `bool`

Determines whether this thread is a group chat. Uses the loaded `Participants` collection when available (returns `true` if more than two participants); otherwise falls back to the persisted `IsGroupThread` flag.

---

### ChatMessage

**Namespace:** `JC.Communication.Messaging.Models.DomainModels`

Represents a single message within a `ChatThread`. Derives sender and timestamp from the `AuditModel` creation fields. Extends `AuditModel`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Unique identifier for the message. Max length 36. |
| `ThreadId` | `string` | — | get; set; | ID of the thread this message belongs to. Required. Max length 36. |
| `Thread` | `ChatThread` | — | get; set; | Navigation property to the parent thread. |
| `ReplyToMessageId` | `string?` | `null` | get; set; | ID of the message this message is replying to. Max length 36. |
| `ReplyToMessage` | `ChatMessage?` | `null` | get; set; | Navigation property to the message being replied to. |
| `Message` | `string` | — | get; set; | The message content. Required. Max length 8192. |
| `SenderUserId` | `string` | — | get; (computed) | User ID of the message sender, derived from `CreatedById`. Throws `InvalidOperationException` if `CreatedById` is null. Not mapped to the database. |
| `SentAtUtc` | `DateTime` | — | get; (computed) | UTC timestamp when the message was sent, derived from `CreatedUtc`. Not mapped to the database. |

#### Constructors

##### ChatMessage()

Parameterless constructor required by EF Core.

##### ChatMessage(string threadId, string message, string? replyToMessageId = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to send the message in. |
| `message` | `string` | — | The message content. |
| `replyToMessageId` | `string?` | `null` | The ID of the message to reply to. |

Creates a new message for the specified thread.

---

### ChatParticipant

**Namespace:** `JC.Communication.Messaging.Models.DomainModels`

Represents a user's membership in a `ChatThread`. Uses a composite primary key of (`ThreadId`, `UserId`). Extends `AuditModel` for soft-delete support, enabling participant removal and re-join scenarios.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ThreadId` | `string` | — | get; set; | ID of the thread this participant belongs to. Required. Max length 36. |
| `Thread` | `ChatThread` | — | get; set; | Navigation property to the parent thread. |
| `UserId` | `string` | — | get; set; | User ID of the participant. Required. Max length 36. |
| `CanSeeHistory` | `bool` | `true` | get; set; | Whether this participant can see messages sent before they joined. |
| `JoinedAtUtc` | `DateTime` | — | get; set; | UTC timestamp when the participant joined (or re-joined) the thread. |

#### Constructors

##### ChatParticipant()

Parameterless constructor required by EF Core.

##### ChatParticipant(string userId)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string` | — | The user ID of the participant. |

Creates a new participant with the specified user ID. Sets `JoinedAtUtc` to `DateTime.UtcNow`. The `ThreadId` is assigned during thread creation.

##### ChatParticipant(string threadId, string userId)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to join. |
| `userId` | `string` | — | The user ID of the participant. |

Creates a new participant for the specified thread. Chains to `ChatParticipant(userId)`, setting `JoinedAtUtc` to `DateTime.UtcNow` and assigning `ThreadId`.

---

### ChatMetadata

**Namespace:** `JC.Communication.Messaging.Models.DomainModels`

Optional visual metadata for a `ChatThread`, including icon, image, and colour settings. Keyed by `ThreadId` (one-to-one with the thread). Extends `AuditModel`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ThreadId` | `string` | — | get; set; | ID of the thread this metadata belongs to. Also serves as the primary key. Max length 36. |
| `Thread` | `ChatThread` | — | get; set; | Navigation property to the parent thread. |
| `Icon` | `string?` | `null` | get; set; | Optional icon identifier (e.g. emoji, icon class name). Max length 256. |
| `ImgPath` | `string?` | `null` | get; set; | Optional image path for the thread avatar. Max length 512. |
| `ColourHex` | `string?` | `null` | get; set; | Thread colour in normalised hex format (e.g. "#FF00AA"). Max length 7. |
| `ColourRgb` | `string?` | `null` | get; set; | Thread colour in normalised RGB format (e.g. "rgb(255,0,170)"). Max length 16. |
| `IsColourHex` | `bool` | — | get; (computed) | Whether a hex colour value has been set. Not mapped to the database. |
| `IsColourRgb` | `bool` | — | get; (computed) | Whether an RGB colour value has been set. Not mapped to the database. |

---

### ThreadDeleted

**Namespace:** `JC.Communication.Messaging.Models.DomainModels`

Tracks per-user thread deletions for soft-delete functionality. Each record indicates that a specific user has deleted a specific thread for themselves. Extends `AuditModel`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Unique identifier for this deletion record. Max length 36. |
| `ThreadId` | `string` | — | get; set; | ID of the deleted thread. Required. Max length 36. |
| `Thread` | `ChatThread` | — | get; set; | Navigation property to the deleted thread. |
| `UserId` | `string` | — | get; set; | User ID of the user who deleted the thread. Required. Max length 36. |
| `DateDeletedUtc` | `DateTime` | — | get; (computed) | UTC timestamp when the thread was deleted, derived from `CreatedUtc`. Not mapped to the database. |

---

### ThreadActivityLog

**Namespace:** `JC.Communication.Logging.Models.Messaging`

Logs thread activity events (message sends, participant additions and removals). Extends `LogModel`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; set; | Unique identifier for this log entry. Max length 36. |
| `ThreadId` | `string` | — | get; set; | ID of the thread the activity occurred in. Required. Max length 36. |
| `Thread` | `ChatThread` | — | get; set; | Navigation property to the thread. |
| `ActivityTimestampUtc` | `DateTime` | — | get; set; | UTC timestamp when the activity occurred. |
| `ActivityType` | `ThreadActivityType` | — | get; set; | The type of activity that occurred. |
| `ActivityDetails` | `string?` | `null` | get; set; | Descriptive details about the activity. Max length 512. |

---

### MessageReadLog

**Namespace:** `JC.Communication.Logging.Models.Messaging`

Logs when a user reads the most recent message in a thread. Uses a composite primary key of (`MessageId`, `UserId`). Extends `LogModel`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `MessageId` | `string` | — | get; set; | ID of the message that was read. Required. Max length 36. |
| `Message` | `ChatMessage` | — | get; set; | Navigation property to the read message. |
| `ReadAtUtc` | `DateTime` | — | get; set; | UTC timestamp when the message was read. |
| `UserId` | `string` | — | get; set; | User ID of the reader. Required. Max length 36. |

---

### ChatModel

**Namespace:** `JC.Communication.Messaging.Models`

Read-only projection of a `ChatThread` for consumption by the UI or API layer. Includes flattened messages, participants, and metadata. Soft-deleted children are excluded during construction.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ThreadId` | `string` | — | get; | Unique identifier of the thread. |
| `ChatName` | `string` | — | get; | Display name of the chat. |
| `ChatDescription` | `string?` | `null` | get; | Optional description of the chat. |
| `IsDefaultThread` | `bool` | — | get; | Whether this is the default thread for its participant set. |
| `LastActivity` | `string` | — | get; | Formatted timestamp string representing the last activity time, or `"Never"` if no activity has occurred. |
| `IsGroupChat` | `bool` | — | get; | Whether this chat is a group chat (more than two participants). |
| `Messages` | `List<MessageModel>` | `[]` | get; internal set; | Messages in this chat, excluding soft-deleted entries. |
| `Participants` | `List<ParticipantModel>` | `[]` | get; | Active participants in this chat. |
| `ChatMetadata` | `MetadataModel?` | `null` | get; | Visual metadata for this chat, or `null` if none exists or it has been soft-deleted. |

#### Constructors

##### ChatModel(ChatThread thread, string dateFormat = "g", bool preferHexCode = true)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `thread` | `ChatThread` | — | The thread entity to project. |
| `dateFormat` | `string` | `"g"` | Format string used to display the last activity date. |
| `preferHexCode` | `bool` | `true` | If `true`, colour values prefer hex over RGB in the metadata model. |

Projects a `ChatThread` entity into a read-only chat model. Filters out soft-deleted messages, participants, and metadata during construction. Converts `LastActivityUtc` to local time using the specified `dateFormat`, or sets it to `"Never"` if no activity has occurred.

---

### MessageModel

**Namespace:** `JC.Communication.Messaging.Models`

Read-only projection of a `ChatMessage` for consumption by the UI or API layer.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `MessageId` | `string` | — | get; | Unique identifier of the message. |
| `ThreadId` | `string` | — | get; | ID of the thread this message belongs to. |
| `ReplyToMessageId` | `string?` | `null` | get; | ID of the message this message is replying to. |
| `Message` | `string` | — | get; | The message content. |
| `SenderUserId` | `string` | — | get; | User ID of the message sender. |
| `SentAtUtc` | `DateTime` | — | get; | UTC timestamp when the message was sent. |

#### Constructors

##### MessageModel(ChatMessage message)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `ChatMessage` | — | The message entity to project. |

---

### ParticipantModel

**Namespace:** `JC.Communication.Messaging.Models`

Read-only projection of a `ChatParticipant` for consumption by the UI or API layer.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ThreadId` | `string` | — | get; | ID of the thread this participant belongs to. |
| `UserId` | `string` | — | get; | User ID of the participant. |
| `CanSeeHistory` | `bool` | — | get; | Whether this participant can see messages sent before they joined. |
| `JoinedAtUtc` | `DateTime` | — | get; | UTC timestamp when the participant joined the thread. |

#### Constructors

##### ParticipantModel(ChatParticipant participant)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `participant` | `ChatParticipant` | — | The participant entity to project. |

---

### MetadataModel

**Namespace:** `JC.Communication.Messaging.Models`

Read-only projection of `ChatMetadata` for consumption by the UI or API layer. Resolves the colour value to a single string based on the `preferHexCode` preference.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ThreadId` | `string` | — | get; | ID of the thread this metadata belongs to. |
| `Icon` | `string?` | `null` | get; | Optional icon identifier. |
| `ImgPath` | `string?` | `null` | get; | Optional image path. |
| `Colour` | `string?` | `null` | get; | Resolved colour value (hex or RGB based on preference), or `null` if no colour is set. |

#### Constructors

##### MetadataModel(ChatMetadata metadata, bool preferHexCode = true)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `metadata` | `ChatMetadata` | — | The metadata entity to project. |
| `preferHexCode` | `bool` | `true` | If `true`, returns the hex colour when available; otherwise returns the RGB value. |

When both `IsColourHex` and `IsColourRgb` are `false`, `Colour` is set to `null`. When `preferHexCode` is `true` and a hex colour is set, `Colour` returns `ColourHex`. Otherwise, `Colour` returns `ColourRgb`.

---

### QueryParams

**Namespace:** `JC.Communication.Messaging.Models`

Base query parameters controlling change tracking and soft-delete filtering behaviour.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `AsNoTracking` | `bool` | `true` | get; | Whether queries should use no-tracking mode for read-only access. |
| `DeletedQueryType` | `DeletedQueryType` | `OnlyActive` | get; | Soft-delete filter mode for queries. |

#### Constructors

##### QueryParams()

Creates query parameters with default values (no-tracking enabled, active records only).

##### QueryParams(bool asNoTracking, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `asNoTracking` | `bool` | — | If `true`, entities are queried without change tracking. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Controls whether active, deleted, or all records are included. |

---

### ChatThreadParams

**Namespace:** `JC.Communication.Messaging.Models`

Parameters for creating and querying chat threads. Extends `QueryParams` with thread-specific properties.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Name` | `string?` | — | get; internal set; | Display name for the thread. May be cleared to `null` to use a default name. |
| `Description` | `string?` | `null` | get; | Optional description for the thread. |
| `DateFormat` | `string` | `"g"` | get; | Format string used to display dates. |
| `PreferHexCode` | `bool` | `true` | get; | Whether colour values should prefer hex over RGB in the returned model. |

Inherits `AsNoTracking` and `DeletedQueryType` from `QueryParams`.

#### Constructors

##### ChatThreadParams(string name, bool asNoTracking = false, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The display name for the thread. |
| `asNoTracking` | `bool` | `false` | If `true`, entities are queried without change tracking. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Controls whether active, deleted, or all records are included. |

Creates thread parameters with a name and query options. Note: `AsNoTracking` defaults to `false` in this overload, unlike the base `QueryParams` default of `true`.

##### ChatThreadParams(string name, string? description = null, string? dateFormat = null, bool preferHexCode = true, bool asNoTracking = true, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The display name for the thread. |
| `description` | `string?` | `null` | An optional description for the thread. |
| `dateFormat` | `string?` | `null` | Format string used to display dates. Defaults to `"g"` if `null`. |
| `preferHexCode` | `bool` | `true` | If `true`, colour values prefer hex over RGB. |
| `asNoTracking` | `bool` | `true` | If `true`, entities are queried without change tracking. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Controls whether active, deleted, or all records are included. |

Creates thread parameters with full control over name, description, formatting, and query options.

---

### MessagingValidationResponse

**Namespace:** `JC.Communication.Messaging.Models`

Base validation response for messaging operations. Contains the validation result and an optional error message.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `IsValid` | `bool` | — | get; | Whether the validation passed. |
| `ErrorMessage` | `string?` | `null` | get; | Error message when validation fails, or `null` when valid. |

#### Constructors

##### MessagingValidationResponse()

Creates a successful validation response (`IsValid` = `true`).

##### MessagingValidationResponse(string errorMessage)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `errorMessage` | `string` | — | The validation error message. |

Creates a failed validation response (`IsValid` = `false`).

---

### ParticipantValidationResponse

**Namespace:** `JC.Communication.Messaging.Models`

Validation response for participant operations. Extends `MessagingValidationResponse`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ValidatedParticipants` | `List<ChatParticipant>` | `[]` | get; | The validated participant list. Empty when validation fails. |

Inherits `IsValid` and `ErrorMessage` from `MessagingValidationResponse`.

#### Constructors

##### ParticipantValidationResponse()

Creates a successful validation response with no participants.

##### ParticipantValidationResponse(List\<ChatParticipant> participant)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `participant` | `List<ChatParticipant>` | — | The validated and prepared participants. |

Creates a successful validation response with the validated participant list.

##### ParticipantValidationResponse(string errorMessage)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `errorMessage` | `string` | — | The validation error message. |

Creates a failed validation response.

---

### ChatThreadValidationResponse

**Namespace:** `JC.Communication.Messaging.Models`

Validation response for chat thread operations. Extends `MessagingValidationResponse`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ValidatedChatThread` | `ChatThread?` | `null` | get; | The validated chat thread, or `null` when validation fails. |

Inherits `IsValid` and `ErrorMessage` from `MessagingValidationResponse`.

#### Constructors

##### ChatThreadValidationResponse()

Creates a successful validation response with no thread.

##### ChatThreadValidationResponse(ChatThread chatThread)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `chatThread` | `ChatThread` | — | The validated chat thread entity. |

##### ChatThreadValidationResponse(string errorMessage)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `errorMessage` | `string` | — | The validation error message. |

---

### ChatMessageValidationResponse

**Namespace:** `JC.Communication.Messaging.Models`

Validation response for chat message operations. Extends `MessagingValidationResponse`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ValidatedChatMessage` | `ChatMessage?` | `null` | get; | The validated chat message, or `null` when validation fails. |

Inherits `IsValid` and `ErrorMessage` from `MessagingValidationResponse`.

#### Constructors

##### ChatMessageValidationResponse()

Creates a successful validation response with no message.

##### ChatMessageValidationResponse(ChatMessage chatMessage)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `chatMessage` | `ChatMessage` | — | The validated chat message entity. |

##### ChatMessageValidationResponse(string errorMessage)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `errorMessage` | `string` | — | The validation error message. |

---

### ChatMetadataValidationResponse

**Namespace:** `JC.Communication.Messaging.Models`

Validation response for chat metadata operations. Extends `MessagingValidationResponse`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ValidatedChatMetadata` | `ChatMetadata?` | `null` | get; | The validated chat metadata, or `null` when validation fails. |

Inherits `IsValid` and `ErrorMessage` from `MessagingValidationResponse`.

#### Constructors

##### ChatMetadataValidationResponse()

Creates a successful validation response with no metadata.

##### ChatMetadataValidationResponse(ChatMetadata chatMetadata)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `chatMetadata` | `ChatMetadata` | — | The validated chat metadata entity. |

##### ChatMetadataValidationResponse(string errorMessage)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `errorMessage` | `string` | — | The validation error message. |

---

## Enums

### DefaultThreadRestoreMode

**Namespace:** `JC.Communication.Messaging.Models.DomainModels`

Controls how default-thread conflicts are resolved when restoring a soft-deleted thread that was previously the default for its participant set.

| Member | Value | Description |
|--------|-------|-------------|
| `Block` | `0` | Prevents the restore if another default thread already exists. |
| `DemoteExisting` | `1` | Demotes the existing default thread and restores this one as the default. |
| `DemoteRestored` | `2` | Restores this thread but removes its default status, keeping the existing default. |

---

### ThreadActivityType

**Namespace:** `JC.Communication.Logging.Models.Messaging`

Identifies the type of activity that occurred in a thread.

| Member | Value | Description |
|--------|-------|-------------|
| `Message` | `0` | A message was sent. |
| `ParticipantAdded` | `1` | One or more participants were added. |
| `ParticipantRemoved` | `2` | One or more participants were removed. |

---

### ThreadActivityLoggingMode

**Namespace:** `JC.Communication.Messaging.Models.Options`

Flags enum controlling which thread activity types are logged by the messaging log service. Values can be combined.

| Member | Value | Description |
|--------|-------|-------------|
| `None` | `0` | No thread activity is logged. |
| `Message` | `1` | Message send events are logged. |
| `ParticipantAdded` | `2` | Participant addition events are logged. |
| `ParticipantRemoved` | `4` | Participant removal events are logged. |
| `All` | `7` | All thread activity types are logged. Equivalent to `Message | ParticipantAdded | ParticipantRemoved`. |

---

## Services

### ChatThreadService

**Namespace:** `JC.Communication.Messaging.Services`

Central service for chat thread operations including querying, creation, promotion, activity tracking, and standard CRUD. All queries are scoped to the current user's participation. Inject via `ChatThreadService`.

#### Methods

##### GetUserChats(string dateFormat = "g", bool preferHexCode = true, bool asNoTracking = true, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)

**Returns:** `Task<List<ChatModel>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `dateFormat` | `string` | `"g"` | Format string used to display dates. |
| `preferHexCode` | `bool` | `true` | If `true`, colour values prefer hex over RGB. |
| `asNoTracking` | `bool` | `true` | If `true`, entities are queried without change tracking. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Controls whether active, deleted, or all threads are returned. |

Retrieves all chat threads the current user participates in, projected as `ChatModel`s. Eagerly loads messages, participants, and metadata. Excludes threads that the current user has per-user deleted. Filters message history based on each user's `CanSeeHistory` setting. Results are ordered by creation date descending.

##### GetUserChats(int pageNumber, int pageSize, string dateFormat = "g", bool preferHexCode = true, bool asNoTracking = true, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)

**Returns:** `Task<IPagination<ChatModel>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `pageNumber` | `int` | — | The 1-based page number to retrieve. |
| `pageSize` | `int` | — | The number of items per page. |
| `dateFormat` | `string` | `"g"` | Format string used to display dates. |
| `preferHexCode` | `bool` | `true` | If `true`, colour values prefer hex over RGB. |
| `asNoTracking` | `bool` | `true` | If `true`, entities are queried without change tracking. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Controls whether active, deleted, or all threads are returned. |

Paginated overload of `GetUserChats`. Pagination is applied at the database level. Returns an `IPagination<ChatModel>` containing the page items and total count.

##### GetDefaultUserChat(string dateFormat = "g", bool preferHexCode = true, bool asNoTracking = false, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive, params IEnumerable\<string> participantUserIds)

**Returns:** `Task<ChatModel?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `dateFormat` | `string` | `"g"` | Format string used to display dates. |
| `preferHexCode` | `bool` | `true` | If `true`, colour values prefer hex over RGB. |
| `asNoTracking` | `bool` | `false` | If `true`, entities are queried without change tracking. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Controls whether active, deleted, or all threads are searched. |
| `participantUserIds` | `IEnumerable<string>` | — | The user IDs of the expected participants. |

Finds the default chat thread between the current user and the specified participants. The current user is automatically included if not already present in the participant list. Matches on exact participant count and membership. Logs a read event for the most recent message in the thread via `MessagingLogService.LogMessageReadAsync`. Returns `null` if no default thread exists for these participants. Filters message history based on the current user's `CanSeeHistory` setting.

##### GetChatModelById(string chatThreadId, string dateFormat = "g", bool preferHexCode = true, bool asNoTracking = false, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)

**Returns:** `Task<ChatModel?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `chatThreadId` | `string` | — | The unique identifier of the chat thread. |
| `dateFormat` | `string` | `"g"` | Format string used to display dates. |
| `preferHexCode` | `bool` | `true` | If `true`, colour values prefer hex over RGB. |
| `asNoTracking` | `bool` | `false` | If `true`, the entity is queried without change tracking. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Controls whether active, deleted, or all threads are searched. |

Retrieves a single chat thread by its ID, provided the current user is a participant. Eagerly loads messages, participants, and metadata. Logs a read event for the most recent message in the thread via `MessagingLogService.LogMessageReadAsync`. Returns `null` if not found or the user is not a participant. Filters message history based on the current user's `CanSeeHistory` setting.

##### VerifyChatExists(string threadId)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to verify. |

Verifies that an active chat thread exists and the current user is a non-deleted participant. Returns `true` if both conditions are met; otherwise `false`.

##### GetOrCreateDefaultChat(ChatThreadParams chatThreadParams, params IEnumerable\<ChatParticipant> participantsParams)

**Returns:** `Task<(ChatModel? Chat, ParticipantValidationResponse ParticipantsResponse)>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `chatThreadParams` | `ChatThreadParams` | — | Parameters controlling thread creation and query behaviour. |
| `participantsParams` | `IEnumerable<ChatParticipant>` | — | The participants to include in the thread. |

Returns the existing default chat thread for the given participants, or creates a new default thread if none exists. Looks up the default thread using participant user IDs extracted from the provided participants. If no default exists and `PreventDuplicateChatThreads` is `true`, the new thread is created as the default. If creation fails validation, `Chat` is `null` and `ParticipantsResponse` contains the error.

##### GetOrCreateChat(string threadId, ChatThreadParams chatThreadParams, params IEnumerable\<ChatParticipant> participantsParams)

**Returns:** `Task<(ChatModel? Chat, ParticipantValidationResponse ParticipantsResponse)>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to look up. |
| `chatThreadParams` | `ChatThreadParams` | — | Parameters controlling thread creation and query behaviour. |
| `participantsParams` | `IEnumerable<ChatParticipant>` | — | The participants to include if a new thread is created. |

Returns the chat thread matching `threadId` if it exists, or creates a new thread if not found. Uses `GetChatModelById` for lookup and `CreateAndGetNewChat` for creation.

##### CreateAndGetNewChat(ChatThreadParams chatThreadParams, params IEnumerable\<ChatParticipant> participantsParams)

**Returns:** `Task<(ChatModel? Chat, ParticipantValidationResponse ParticipantsResponse)>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `chatThreadParams` | `ChatThreadParams` | — | Parameters controlling thread creation. |
| `participantsParams` | `IEnumerable<ChatParticipant>` | — | The participants to include in the new thread. |

Creates a new chat thread and returns it as a `ChatModel`. Automatically determines whether the thread should be marked as default based on whether a default already exists for the given participants. If a default exists and `PreventDuplicateChatThreads` is `true`, returns an error. If `PreventDuplicateChatThreads` is `false`, the new thread is created as a non-default thread.

##### TryCreateChat(ChatThread thread, ChatMetadata? metadata = null, params IEnumerable\<ChatParticipant> participantParams)

**Returns:** `Task<ChatThreadValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `thread` | `ChatThread` | — | The chat thread entity to create. |
| `metadata` | `ChatMetadata?` | `null` | Optional metadata (icon, colour, image) to associate with the thread. |
| `participantParams` | `IEnumerable<ChatParticipant>` | — | The participants to add to the thread. |

Validates and persists a new chat thread with its participants and optional metadata within a transaction. Performs three validation stages in order: participants, metadata, then thread. All accumulated errors from all stages are returned in the final `ChatThreadValidationResponse`. The current user is automatically added as a participant. If `PreventDuplicateChatThreads` is `true` and a default already exists for the participant set, an error is returned. Sets `IsGroupThread` and `IsDefaultThread` automatically based on participant count and existing defaults.

##### TryUpdateChatThread(string threadId, ChatThread updatedThread)

**Returns:** `Task<ChatThreadValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to update. |
| `updatedThread` | `ChatThread` | — | The thread entity containing the updated values. |

Validates and applies updates to an existing chat thread. The current user must be a participant. Immutable properties (`IsDefaultThread`, `IsGroupThread`) are enforced by validation — any attempt to change them produces an error. Returns a `ChatThreadValidationResponse` with the validated thread on success.

##### TryDeleteChatThreadForAll(string threadId)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to delete. |

Soft-deletes an active chat thread for all participants. The current user must be a participant. Loads participants via eager loading, then creates `ThreadDeleted` records for all participants who do not already have an active deletion record. Operates within a transaction. Returns `true` if the thread was found and soft-deleted; `false` if not found or the user is not a participant.

##### TryRestoreChatThreadForAll(string threadId, DefaultThreadRestoreMode mode = DefaultThreadRestoreMode.DemoteExisting)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to restore. |
| `mode` | `DefaultThreadRestoreMode` | `DemoteExisting` | Strategy for resolving default-thread conflicts. |

Restores a soft-deleted chat thread. If the thread is already active, returns `true` immediately. Loads the thread with `DeletedQueryType.All` to find deleted threads. When restoring a default thread and another default already exists for the same participants, the `mode` parameter determines the resolution: `Block` prevents the restore, `DemoteExisting` removes default status from the existing thread, `DemoteRestored` removes default status from the restored thread. Also soft-deletes any active `ThreadDeleted` records for the thread's participants. Operates within a transaction. Returns `false` if not found, the user is not a participant, or the restore was blocked.

##### TryDeleteThreadForUser(string threadId)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to delete for the current user. |

Deletes the thread for the current user only by creating a `ThreadDeleted` record. Does not affect other participants. Returns `true` if the thread exists and the operation completed (including if the thread was already deleted for the user — idempotent). Returns `false` if the thread does not exist or the user is not a participant.

##### TryRestoreThreadForUser(string threadId)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to restore for the current user. |

Restores a per-user deleted thread by soft-deleting the `ThreadDeleted` record. Returns `true` if the thread exists and the operation completed (including if the thread was not deleted for the user — idempotent). Returns `false` if the thread does not exist or the user is not a participant.

##### PromoteChatToDefault(string threadId, bool demoteExisting = false)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to promote. |
| `demoteExisting` | `bool` | `false` | If `true`, the existing default thread is demoted to allow promotion. If `false`, the operation returns `false` when another default already exists. |

Promotes a non-default chat thread to default status. The current user must be a participant. If the thread is already the default, returns `true` immediately. Loads participants via eager loading to find the existing default for the same participant set. When another default exists and `demoteExisting` is `false`, returns `false`. When `demoteExisting` is `true`, the existing default is demoted and both threads are updated within a transaction. Returns `false` if not found or the user is not a participant.

---

### ChatMessageService

**Namespace:** `JC.Communication.Messaging.Services`

Manages chat message operations including sending, editing, deleting, restoring, and bulk operations. All operations verify thread existence and user participation before proceeding. Inject via `ChatMessageService`.

#### Methods

##### TrySendMessage(string threadId, string message, string? replyToId)

**Returns:** `Task<ChatMessageValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to send the message in. |
| `message` | `string` | — | The message content to send. |
| `replyToId` | `string?` | — | The ID of the message to reply to, or `null` for a new message. |

Validates and sends a new message in the specified thread within a transaction. Creates a `ChatMessage` with the provided content and optional reply-to reference. Validates message length against `MessagingOptions.MaxMessageLength`. Updates the thread's `LastActivityUtc` timestamp and logs a `ThreadActivityType.Message` activity via `MessagingLogService`. Returns a `ChatMessageValidationResponse` with the created message on success.

##### TryEditMessage(string threadId, string messageId, string newMessage)

**Returns:** `Task<ChatMessageValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread the message belongs to. |
| `messageId` | `string` | — | The ID of the message to edit. |
| `newMessage` | `string` | — | The new message content. |

Validates and updates the content of an existing message. Only the original sender (matched by `CreatedById`) can edit their messages. Validates the new content against `MessagingOptions.MaxMessageLength`. Returns an error if the thread does not exist, the message is not found, or the user is not the sender.

##### TryDeleteMessage(string threadId, string messageId)

**Returns:** `Task<ChatMessageValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread the message belongs to. |
| `messageId` | `string` | — | The ID of the message to delete. |

Soft-deletes a message owned by the current user. Returns the deleted message in the response on success. Returns an error if the thread does not exist, the message is not found, or the user is not the sender.

##### TryRestoreMessage(string threadId, string messageId)

**Returns:** `Task<ChatMessageValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread the message belongs to. |
| `messageId` | `string` | — | The ID of the message to restore. |

Restores a soft-deleted message owned by the current user. Searches across all deletion states (`DeletedQueryType.All`). If the message is already active, returns success with the message without making changes. Returns an error if the thread does not exist, the message is not found, or the user is not the sender.

##### TryDeleteAllMyMessages(string threadId)

**Returns:** `Task<ChatMessageValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to delete messages from. |

Soft-deletes all active messages sent by the current user in the specified thread. Returns an error if the thread does not exist or no messages are found.

##### TryRestoreAllMyDeletedMessages(string threadId)

**Returns:** `Task<ChatMessageValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to restore messages in. |

Restores all soft-deleted messages sent by the current user in the specified thread. Queries all messages (`DeletedQueryType.All`) scoped to the current user, then restores only those that are currently deleted. Returns success if no messages need restoring.

##### TryDeleteAllMessages(string threadId)

**Returns:** `Task<ChatMessageValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to delete all messages from. |

**Unsafe.** Soft-deletes all active messages in the specified thread regardless of sender. Affects messages from all participants. Returns an error if the thread does not exist or no messages are found.

##### TryRestoreAllDeletedMessages(string threadId)

**Returns:** `Task<ChatMessageValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to restore all messages in. |

**Unsafe.** Restores all soft-deleted messages in the specified thread regardless of sender. Affects messages from all participants. Queries all messages (`DeletedQueryType.All`) without user filtering, then restores only those that are currently deleted. Returns success if no messages need restoring.

---

### ChatParticipantService

**Namespace:** `JC.Communication.Messaging.Services`

Manages chat participant operations including adding, removing, and leaving chat threads. Handles participant restoration, default-thread demotion, and activity logging within transactions. Inject via `ChatParticipantService`.

#### Methods

##### TryAddParticipantToChat(string threadId, string userId)

**Returns:** `Task<ParticipantValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to add the participant to. |
| `userId` | `string` | — | The user ID of the participant to add. |

Convenience overload that creates a `ChatParticipant` from the user ID and delegates to the multi-participant overload.

##### TryAddParticipantToChat(string threadId, ChatParticipant participant)

**Returns:** `Task<ParticipantValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to add the participant to. |
| `participant` | `ChatParticipant` | — | The participant entity to add. |

Convenience overload that delegates to the multi-participant overload.

##### TryAddParticipantsToChat(string threadId, params IEnumerable\<string> participants)

**Returns:** `Task<ParticipantValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to add participants to. |
| `participants` | `IEnumerable<string>` | — | The user IDs of the participants to add. |

Convenience overload that creates `ChatParticipant` entities from user IDs and delegates to the entity-based overload.

##### TryAddParticipantsToChat(string threadId, params IEnumerable\<ChatParticipant> participantParams)

**Returns:** `Task<ParticipantValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to add participants to. |
| `participantParams` | `IEnumerable<ChatParticipant>` | — | The participant entities to add. |

Adds multiple participants to a chat thread. Loads the thread via `GetChatModelById` and validates the full participant set (existing + new). When `ImmutableDirectMessageParticipants` is `true` and the thread is a direct message, returns an error. If a participant was previously soft-deleted (removed), they are automatically restored with an updated `JoinedAtUtc` instead of creating a duplicate record. Genuinely new participants are added. If the thread is a default thread and the new participants already have a default thread, the current thread is automatically demoted from default status. Logs a `ThreadActivityType.ParticipantAdded` activity. Operates within a transaction.

##### TryRemoveParticipantFromChat(string threadId, string userId)

**Returns:** `Task<ParticipantValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to remove the participant from. |
| `userId` | `string` | — | The user ID of the participant to remove. |

Convenience overload that creates a `ChatParticipant` from the user ID and delegates to the multi-participant overload.

##### TryRemoveParticipantFromChat(string threadId, ChatParticipant participant)

**Returns:** `Task<ParticipantValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to remove the participant from. |
| `participant` | `ChatParticipant` | — | The participant entity to remove. |

Convenience overload that delegates to the multi-participant overload.

##### TryRemoveParticipantsFromChat(string threadId, params IEnumerable\<string> participants)

**Returns:** `Task<ParticipantValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to remove participants from. |
| `participants` | `IEnumerable<string>` | — | The user IDs of the participants to remove. |

Convenience overload that creates `ChatParticipant` entities from user IDs and delegates to the entity-based overload.

##### TryRemoveParticipantsFromChat(string threadId, params IEnumerable\<ChatParticipant> participantParams)

**Returns:** `Task<ParticipantValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to remove participants from. |
| `participantParams` | `IEnumerable<ChatParticipant>` | — | The participant entities to remove. |

Removes multiple participants from a chat thread. The current user cannot remove themselves — use `TryLeaveGroupChat` instead. When `ImmutableDirectMessageParticipants` is `true` and the thread is a direct message, returns an error. Validates both the removal set and the post-removal participant state to ensure the thread remains valid (e.g. at least two participants). If the thread is a default thread, checks whether the remaining participants already have a default thread and demotes if necessary. Logs a `ThreadActivityType.ParticipantRemoved` activity. Operates within a transaction.

##### TryLeaveGroupChat(string threadId)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the group chat thread to leave. |

Allows the current user to leave a group chat. Returns `false` if the thread is not found, is not a group chat, or the user is not a participant. Soft-deletes the current user's `ChatParticipant` record.

---

### ChatMetadataService

**Namespace:** `JC.Communication.Messaging.Services`

Manages chat metadata operations including creating, updating, deleting, and restoring thread metadata. All operations verify thread existence before proceeding. Inject via `ChatMetadataService`.

#### Methods

##### TryCreateChatMetadata(string threadId, ChatMetadata metadata)

**Returns:** `Task<ChatMetadataValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread to create metadata for. |
| `metadata` | `ChatMetadata` | — | The metadata entity to validate and persist. |

Validates and creates new metadata for the specified thread. Throws `ArgumentNullException` if `metadata` is `null`. If soft-deleted metadata already exists for the thread, it is hard-deleted before creating the new record. If active metadata already exists, returns an error. Validates that at least one field (icon, image path, colour hex, or colour RGB) is populated. Normalises colour values via `ColourHelper`. Assigns the `threadId` to the metadata entity.

##### TryUpdateChatMetadata(string threadId, ChatMetadata metadata)

**Returns:** `Task<ChatMetadataValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread whose metadata is being updated. |
| `metadata` | `ChatMetadata` | — | The metadata entity containing the updated values. |

Validates and updates existing metadata for the specified thread. Throws `ArgumentNullException` if `metadata` is `null`. Returns an error if the thread does not exist or no active metadata exists. Validates and normalises colour values.

##### TryDeleteChatMetadata(string threadId)

**Returns:** `Task<ChatMetadataValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread whose metadata is being deleted. |

Soft-deletes the active metadata for the specified thread. Returns an error if the thread does not exist or no active metadata exists. Returns the deleted metadata in the response on success.

##### TryRestoreChatMetadata(string threadId)

**Returns:** `Task<ChatMetadataValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread whose metadata is being restored. |

Restores soft-deleted metadata for the specified thread. Searches across all deletion states (`DeletedQueryType.All`). If the metadata is already active, returns success with the metadata without making changes. Returns an error if the thread does not exist or no metadata record exists.

---

### MessagingValidationService

**Namespace:** `JC.Communication.Messaging.Services`

Provides validation and preparation logic for all messaging entities including threads, participants, messages, and metadata. Methods prefixed with "ValidateAndPrepare" mutate the input entities to set computed properties before validation. Inject via `MessagingValidationService`.

All methods on this service are `internal` — they are used by the other messaging services and are not intended to be called directly by consumers. This section is included for completeness.

#### Methods

##### CheckForDefaultChat(IEnumerable\<string> participantUserIds)

**Returns:** `Task<bool>` — **Access:** `internal`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `participantUserIds` | `IEnumerable<string>` | — | The user IDs of the expected participants. |

Checks whether an active default chat thread exists for the given participant set. The current user is automatically included if not already present. Matches on exact participant count and membership.

##### IsThreadDirectMessage(List\<ChatParticipant> participants)

**Returns:** `bool` — **Access:** `internal`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `participants` | `List<ChatParticipant>` | — | The participant list to evaluate. |

Determines whether a participant list represents a direct message. Returns `true` if the list contains two participants (one of which is the current user) or a single participant.

##### ValidateAndPrepareChatThread(ChatThread thread, List\<ChatParticipant> participants, string? errors = null)

**Returns:** `Task<ChatThreadValidationResponse>` — **Access:** `internal`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `thread` | `ChatThread` | — | The thread entity to validate and mutate. |
| `participants` | `List<ChatParticipant>` | — | The validated participant list for this thread. |
| `errors` | `string?` | `null` | Optional pre-existing errors from earlier validation stages. |

Validates and prepares a new chat thread for creation. Sets `IsGroupThread` based on participant count and `IsDefaultThread` based on whether a default already exists. Enforces `PreventDuplicateChatThreads` when a default already exists. Validates that `Name` is not null or whitespace.

##### ValidateChatThread(ChatThread thread, ChatThread updatedThread, string? errors = null)

**Returns:** `ChatThreadValidationResponse` — **Access:** `internal`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `thread` | `ChatThread` | — | The original thread entity from the database. |
| `updatedThread` | `ChatThread` | — | The thread entity containing the proposed changes. |
| `errors` | `string?` | `null` | Optional pre-existing errors from earlier validation stages. |

Validates an update to an existing chat thread. Enforces that `IsDefaultThread` and `IsGroupThread` are immutable — any attempt to change them produces an error.

##### ValidateAndPrepareParticipants(string threadId, List\<ChatParticipant> participants, string? errors = null, bool addCurrentUser = true)

**Returns:** `ParticipantValidationResponse` — **Access:** `internal`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The thread ID to assign to each participant. |
| `participants` | `List<ChatParticipant>` | — | The participant list to validate and mutate. |
| `errors` | `string?` | `null` | Optional pre-existing errors from earlier validation stages. |
| `addCurrentUser` | `bool` | `true` | If `true`, the current user is added when not already in the list. |

Validates and prepares a participant list. Adds the current user if not present (when `addCurrentUser` is `true`). Checks that the participant list does not consist of only the current user. Checks for duplicate user IDs. Enforces `DisableGroups` when more than two participants are present. Sets each participant's `ThreadId` and `CanSeeHistory` from `MessagingOptions.ParticipantsSeeChatHistory`.

##### ValidateChatMessage(ChatMessage message, string? errors = null)

**Returns:** `ChatMessageValidationResponse` — **Access:** `internal`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `ChatMessage` | — | The message entity to validate. |
| `errors` | `string?` | `null` | Optional pre-existing errors from earlier validation stages. |

Validates a chat message against `MessagingOptions.MaxMessageLength`.

##### ValidateAndPrepareChatMetadata(string threadId, ChatMetadata? metadata, string? errors = null)

**Returns:** `ChatMetadataValidationResponse` — **Access:** `internal`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The thread ID to assign to the metadata. |
| `metadata` | `ChatMetadata?` | — | The metadata entity to validate and mutate, or `null` to skip validation. |
| `errors` | `string?` | `null` | Optional pre-existing errors from earlier validation stages. |

Validates and prepares chat metadata. If `metadata` is `null`, returns a successful response immediately. Requires at least one field (icon, image path, colour hex, or colour RGB) to be populated. Normalises hex colours via `ColourHelper.ValidateAndNormaliseHexColour` (prepends `#`) and RGB colours via `ColourHelper.ValidateAndNormaliseRgbColour`. Assigns the `threadId` to the metadata entity.

---

### MessagingLogService

**Namespace:** `JC.Communication.Logging.Services`

Handles persistence of messaging-related log entries to the database. Respects the configured `MessagingOptions` to determine which events are logged. Inject via `MessagingLogService`.

#### Methods

##### LogThreadActivityAsync(string threadId, ThreadActivityType activityType, string? activityDetails = null, CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `threadId` | `string` | — | The ID of the thread the activity occurred in. |
| `activityType` | `ThreadActivityType` | — | The type of activity to log. |
| `activityDetails` | `string?` | `null` | Optional descriptive details about the activity. |
| `cancellationToken` | `CancellationToken` | `default` | Optional cancellation token. |

Logs a thread activity event to the database. Does nothing if `ThreadActivityLoggingMode` is `None` or does not include the flag corresponding to the given `activityType`. Creates a `ThreadActivityLog` record with the current UTC timestamp. **Does not call `SaveChanges`** — the caller is responsible for persisting changes, typically as part of a wider transaction. Exceptions are caught and logged rather than thrown.

##### LogMessageReadAsync(ChatMessage? message, CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `ChatMessage?` | — | The most recent message in the thread, or `null` if the thread has no messages. |
| `cancellationToken` | `CancellationToken` | `default` | Optional cancellation token. |

Logs that the current user has read up to the most recent message in a thread. Only the latest message is tracked — this avoids log churn from cleanup jobs. Does nothing if `LogChatReads` is `false` or `message` is `null`. Checks whether a `MessageReadLog` already exists for this user and message; if so, skips logging. Creates a `MessageReadLog` record with the current UTC timestamp and calls `SaveChanges` immediately. Exceptions are caught and logged rather than thrown.

---

### ActivityLogCleanupJob

**Namespace:** `JC.Communication.Messaging.Services`

Background job that cleans up old `ThreadActivityLog` records based on the configured retention settings. Implements `IBackgroundJob`.

#### Methods

##### ExecuteAsync(CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | Optional cancellation token. |

Executes the activity log cleanup. Does nothing if `EnableActivityLogCleanupJob` is `false`. Queries all logs with `CreatedUtc` older than the cutoff date (current UTC minus `ActivityLogRetentionMonths`). If `ActivityLogMinimumRetentionRecords` is greater than zero and greater than or equal to the matching log count, the cleanup is skipped entirely. When `ActivityLogCleanupChunkingValue` is greater than zero, the deletion set is limited to that many records. The minimum retention records are preserved by skipping the most recent records after ordering by descending `CreatedUtc`. Deletes matching records via hard delete.

---

### ReadLogCleanupJob

**Namespace:** `JC.Communication.Messaging.Services`

Background job that cleans up old `MessageReadLog` records based on the configured retention settings. Implements `IBackgroundJob`.

#### Methods

##### ExecuteAsync(CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | Optional cancellation token. |

Executes the read log cleanup. Does nothing if `EnableReadLogCleanupJob` is `false`. Queries all logs with `CreatedUtc` older than the cutoff date (current UTC minus `ReadLogRetentionMonths`). When `KeepMostRecentReadLog` is `true`, the most recent log per user per message (grouped by `UserId` and `MessageId`) is excluded from deletion regardless of age. If `ReadLogMinimumRetentionRecords` is greater than zero and greater than or equal to the remaining log count, the cleanup is skipped entirely. When `ReadLogCleanupChunkingValue` is greater than zero, the deletion set is limited to that many records. The minimum retention records are preserved by skipping the most recent records after ordering by descending `CreatedUtc`. Deletes matching records via hard delete.

---

## Helpers

### ActivityDetailsHelper

**Namespace:** `JC.Communication.Logging.Models.Messaging`

Static helper class that generates human-readable activity detail strings for `ThreadActivityLog` entries.

#### Methods

##### GetActivityDetails(ThreadActivityType activityType, List\<string> participant)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `activityType` | `ThreadActivityType` | — | The type of activity that occurred. |
| `participant` | `List<string>` | — | The user IDs involved in the activity. |

Returns a formatted string describing the activity:
- `Message` → `"Message from {userId}"` (uses the first participant, or `"Unknown User"` if the list is empty)
- `ParticipantAdded` → `"Participant(s) added: {userId1}, {userId2}, ..."`
- `ParticipantRemoved` → `"Participant(s) removed: {userId1}, {userId2}, ..."`

Throws `ArgumentOutOfRangeException` for unrecognised activity types.

---

## Data

### IMessagingDbContext

**Namespace:** `JC.Communication.Messaging.Data`

Interface defining the `DbSet` properties required by the messaging module. Implement this interface on the consuming application's `DbContext` to provide the necessary tables for chat threads, messages, participants, and metadata.

#### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `ChatThreads` | `DbSet<ChatThread>` | get; set; | Chat threads table. |
| `DeletedThreads` | `DbSet<ThreadDeleted>` | get; set; | Per-user thread deletions table. |
| `ChatMessages` | `DbSet<ChatMessage>` | get; set; | Chat messages table. |
| `ChatParticipants` | `DbSet<ChatParticipant>` | get; set; | Chat participants table. |
| `ChatMetadata` | `DbSet<ChatMetadata>` | get; set; | Chat metadata table. |
| `ThreadActivityLogs` | `DbSet<ThreadActivityLog>` | get; set; | Thread activity logs table. |
| `MessageReadLogs` | `DbSet<MessageReadLog>` | get; set; | Message read logs table. |

---

### Data mappings

The following `IEntityTypeConfiguration<T>` classes configure EF Core entity mappings for the messaging module. Apply them via `modelBuilder.ApplyMessagingMappings()` in your DbContext's `OnModelCreating`.

#### ChatThreadMap

**Namespace:** `JC.Communication.Messaging.Data.DataMappings`

| Configuration | Detail |
|---------------|--------|
| Primary key | `Id` (max length 36) |
| Required properties | `Name` (max 256), `IsDefaultThread`, `IsGroupThread` |
| Optional properties | `Description` (max 1024), `LastActivityUtc` (precision 0) |
| Relationships | HasMany `Messages` (FK `ThreadId`), HasMany `Participants` (FK `ThreadId`), HasOne `ChatMetadata` (FK `ThreadId`), HasMany `UserThreadDeletions` (FK `ThreadId`) |
| Audit | `AuditModelMapping` applied |

#### ChatMessageMap

**Namespace:** `JC.Communication.Messaging.Data.DataMappings`

| Configuration | Detail |
|---------------|--------|
| Primary key | `Id` (max length 36) |
| Required properties | `ThreadId` (max 36), `Message` (max 8192) |
| Optional properties | `ReplyToMessageId` (max 36) |
| Relationships | Self-referencing `ReplyToMessage` (FK `ReplyToMessageId`, `DeleteBehavior.Restrict`) |
| Indexes | `ThreadId` |
| Audit | `AuditModelMapping` applied |

#### ChatParticipantMap

**Namespace:** `JC.Communication.Messaging.Data.DataMappings`

| Configuration | Detail |
|---------------|--------|
| Primary key | Composite (`ThreadId`, `UserId`) |
| Required properties | `ThreadId` (max 36), `UserId` (max 36), `CanSeeHistory`, `JoinedAtUtc` (precision 0) |
| Indexes | `UserId` |
| Audit | `AuditModelMapping` applied |

#### ChatMetadataMap

**Namespace:** `JC.Communication.Messaging.Data.DataMappings`

| Configuration | Detail |
|---------------|--------|
| Primary key | `ThreadId` (max length 36) |
| Optional properties | `Icon` (max 256), `ImgPath` (max 512), `ColourHex` (max 7), `ColourRgb` (max 16) |
| Ignored properties | `IsColourHex`, `IsColourRgb` (computed) |
| Audit | `AuditModelMapping` applied |

#### ThreadDeletedMap

**Namespace:** `JC.Communication.Messaging.Data.DataMappings`

| Configuration | Detail |
|---------------|--------|
| Primary key | `Id` (max length 36) |
| Required properties | `ThreadId` (max 36), `UserId` (max 36) |
| Indexes | `ThreadId`, `UserId` |
| Audit | `AuditModelMapping` applied |

#### ThreadActivityLogMap

**Namespace:** `JC.Communication.Logging.Data.DataMappings.Messaging`

| Configuration | Detail |
|---------------|--------|
| Primary key | `Id` (max length 36) |
| Required properties | `ThreadId` (max 36), `ActivityTimestampUtc` (precision 0), `ActivityType` (stored as `int`) |
| Optional properties | `ActivityDetails` (max 512) |
| Indexes | `ThreadId` |
| Log | `LogModelMapping` applied |

#### MessageReadLogMap

**Namespace:** `JC.Communication.Logging.Data.DataMappings.Messaging`

| Configuration | Detail |
|---------------|--------|
| Primary key | Composite (`MessageId`, `UserId`) |
| Required properties | `MessageId` (max 36), `UserId` (max 36), `ReadAtUtc` (precision 0) |
| Indexes | `MessageId`, `UserId` |
| Log | `LogModelMapping` applied |
