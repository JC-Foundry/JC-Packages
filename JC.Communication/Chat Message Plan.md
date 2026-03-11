# JC.Communication – Direct Messages & Group Chats Design Guide

This document captures the agreed design direction for adding **direct messaging** and **group chats** to `JC.Communication`.

This is **not** about in-app notifications.

Messaging and notifications may be linked together by a consuming app, but they are separate features with different architecture.

---

# 1. Purpose

`JC.Communication` messaging should provide **infrastructure and persistent communication features**, not a full social/chat product.

It should provide:

- direct user-to-user messaging
- group chats
- thread infrastructure
- participant/member infrastructure
- message persistence
- read-state tracking
- thread creation rules
- group membership/history rules
- batch send support

It should **not** provide app-specific “Discord-lite” features such as:

- typing indicators
- online badges
- reactions
- profile cards
- attachments
- rich embeds
- message edits
- fancy realtime UX
- social graph rules
- friend-only messaging rules

Those belong in the consuming app.

---

# 2. High-Level Architecture

Messaging should be built around a few core concepts:

- **Thread / Conversation**
- **Message**
- **Participants / Members**
- **Read state / Read logs**
- **Group chat infrastructure**
- **Thread creation rules**

The package should not dictate *why* a thread exists.

A consuming app may use the same infrastructure for:

- direct messages
- group chats
- game lobby messaging
- admin/user support conversations
- role request conversations
- task/issue discussion threads

Under the hood, they are still just:

- thread
- participants
- messages

---

# 3. Core Behaviour Rules

## 3.1 Direct Messaging

Default behaviour:

- one canonical/default direct thread per user pair

Meaning:
- if User A and User B already have a direct thread, reuse it by default
- do not automatically create duplicate default direct threads for the same pair

However, the infrastructure should also support:

- explicit creation of a **new direct thread** with the same user pair when requested

So the package should support both:

- **get or create default direct thread**
- **explicitly create new direct thread**

---

## 3.2 Group Chats

Default behaviour:

- one canonical/default thread for a group chat

The package should support:

- create group
- create group thread
- send messages into group thread
- optionally create a new separate thread for the same group if explicitly requested

So similar to direct messaging, group messaging should support both:

- default/canonical group thread
- explicit new thread creation

---

## 3.3 Batch Send

Messaging infrastructure should support two distinct behaviours:

### A. Shared group conversation
One group/thread, many participants, shared message history.

### B. Batch send separately
Send to multiple users, but each recipient gets a separate conversation/message path.

This is important because batch-send should not force apps to misuse group chat when they actually want separate private communication.

---

# 4. Thread Creation Rules

Thread creation is **not dictated by the package**.

The package provides the infrastructure and services to create/manage threads.

The consuming app decides when and why a thread should be created.

Examples:
- a new direct thread between two users
- a group thread for a team/support/admin chat
- a game-specific thread created when a Battleship game starts
- a role request conversation thread
- an issue/escalation discussion thread

So the package provides:
- thread creation
- thread retrieval
- message sending
- participant rules

The app provides:
- context
- naming
- UI
- workflow triggers

---

# 5. Suggested Core Models

A clean messaging foundation will likely need models along these lines:

- `MessageThread`
- `MessageThreadParticipant` or `MessageGroupMember`
- `ChatMessage`
- maybe `MessageGroup`
- `ChatMessageReadLog`

Exact naming can vary, but the concepts matter.

---

# 6. Message Thread / Conversation Model

A thread/conversation should represent the durable communication container.

Suggested responsibilities:

- identifies whether thread is direct or group
- stores title/name if applicable
- tracks creation time
- links participants
- holds messages
- may optionally store metadata/context in future

Potential useful properties:

- `Id`
- `IsGroupChat`
- `Title?`
- `CreatedUtc`
- `CreatedByUserId?`
- maybe `GroupId?` if groups are modelled separately
- maybe `IsDefaultThread`
- maybe `ThreadType` in future if apps want categorisation

## Important rule
The package should treat all threads as infrastructure.

A direct thread, game thread, support thread, or admin thread are all just threads with participants and messages.

---

# 7. Group Chat Support

Group chat support should include:

- create group
- add/remove members
- create group thread
- send messages to group
- retrieve group thread messages
- read-state support per member

## 7.1 Group History Visibility

This is an important messaging rule and belongs in the infrastructure.

When a new user is added to a group, the package should support policies like:

- **CanSeeHistory = true**
- **CanSeeHistory = false**

Meaning:
- some groups allow new members to see prior messages
- some groups only allow new members to see messages after joining

This should be enforced by retrieval/query rules, not just UI.

### Practical shape
A member/group participant can store things like:
- joined at timestamp
- can see history flag

Then message queries can decide:
- show all historical messages
- or only show messages after join time

---

# 8. Messages

A message model should stay simple and infrastructure-focused.

Suggested responsibilities:

- stores the message content
- links to the thread
- stores sender
- stores creation time
- optionally stores HTML if desired, though plain text is likely enough for v1

Potential useful properties:

- `Id`
- `ThreadId`
- `SenderUserId`
- `Body`
- `BodyHtml?`
- `CreatedUtc`
- maybe `IsSystemMessage`
- maybe `ReplyToMessageId?` later, but not necessary for v1

## Notes
Keep the message model lean.

Do not add:
- reactions
- attachments
- edits
- embeds
- typing metadata
- presence metadata

Those belong to the app layer if ever needed.

---

# 9. Read State and Logging

## 9.1 Current State

Messages need read state.

Unlike notifications, messages do **not** need unread toggling as a feature.

The normal lifecycle is:

- message exists
- recipient/member has not read it yet
- recipient/member reads it
- read is logged

There is no need for “mark unread again” behaviour.

---

## 9.2 Message Read Log

A read log is useful and should be shared across both direct and group messaging.

The event itself is simple:

- which message was read
- by which user
- when it was read

It does not fundamentally matter whether the message came from a direct thread or a group thread.

However, an `IsGroupChat` flag can be included as a useful denormalized reporting convenience.

### Why include `IsGroupChat`
Not required for correctness, but useful for:
- quick filtering
- admin reporting
- simpler query/report usage
- analytics

Source of truth is still the thread relationship.  
`IsGroupChat` is just a convenience field.

### Suggested model shape

```csharp
public class ChatMessageReadLog : AuditModel
{
    [Key]
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    [Required]
    public string MessageId { get; set; }

    [ForeignKey(nameof(MessageId))]
    public ChatMessage Message { get; set; }

    [Required]
    public string UserId { get; set; }

    public bool IsGroupChat { get; set; }

    public DateTime ReadAtUtc => CreatedUtc;
}