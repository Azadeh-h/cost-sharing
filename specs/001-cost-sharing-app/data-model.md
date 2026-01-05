# Data Model: Cost-Sharing Application

**Date**: 2026-01-05  
**Purpose**: Define domain entities, relationships, and validation rules

## Entity Relationship Diagram

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐
│    User     │────┬───→│ GroupMember  │←───┬────│    Group    │
└─────────────┘    │    └──────────────┘    │    └─────────────┘
       │           │                         │           │
       │           │    ┌──────────────┐    │           │
       │           └───→│  Invitation  │←───┘           │
       │                └──────────────┘                │
       │                                                │
       │           ┌──────────────┐                     │
       └──────────→│   Expense    │←────────────────────┘
                   └──────────────┘
                          │
                          ├──→┌───────────────┐
                          │   │ ExpenseSplit  │
                          │   └───────────────┘
                          │
                          └──→┌──────────────┐
                              │     Debt     │
                              └──────────────┘
                              
                   ┌──────────────┐
                   │  Settlement  │
                   └──────────────┘
```

## Entities

### 1. User

Represents an application user with authentication credentials and profile information.

**Properties**:
| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | Guid | Yes | - | Unique identifier |
| Email | string | Yes | Valid email format, unique | Primary contact and login |
| Phone | string | No | E.164 format if provided | Optional phone number |
| PasswordHash | string | Yes* | - | Hashed password (*if not using magic link only) |
| Name | string | Yes | 1-100 chars | Display name |
| CreatedAt | DateTime | Yes | - | Account creation timestamp |
| LastLoginAt | DateTime | No | - | Last successful login |
| IsEmailVerified | bool | Yes | Default: false | Email verification status |

**Relationships**:
- One-to-many with GroupMember (a user can be in multiple groups)
- One-to-many with Expense (a user can create multiple expenses)
- One-to-many with Invitation (a user can send multiple invitations)

**Business Rules**:
- Email must be unique across all users
- Password must be at least 8 characters with at least one digit
- Email must be verified before creating groups or expenses
- User soft-deletion retains data for audit purposes

**File Storage**:
```json
// users/{userId}.json
{
  "id": "uuid",
  "email": "user@example.com",
  "phone": "+61412345678",
  "passwordHash": "hashed_value",
  "name": "John Doe",
  "createdAt": "2026-01-05T10:30:00Z",
  "lastLoginAt": "2026-01-05T14:20:00Z",
  "isEmailVerified": true
}
```

---

### 2. Group

Represents a cost-sharing group containing members and expenses.

**Properties**:
| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | Guid | Yes | - | Unique identifier |
| Name | string | Yes | 1-100 chars, non-empty | Group display name |
| CreatorId | Guid | Yes | Valid User.Id | User who created the group |
| CreatedAt | DateTime | Yes | - | Group creation timestamp |
| UpdatedAt | DateTime | Yes | - | Last modification timestamp |
| Currency | string | Yes | "AUD" | Currency code (fixed to AUD) |

**Relationships**:
- Many-to-one with User (creator)
- One-to-many with GroupMember (multiple members in group)
- One-to-many with Expense (group contains expenses)
- One-to-many with Invitation (pending member invitations)

**Business Rules**:
- Group creator is automatically added as admin member
- Group name must be unique per creator (can have duplicates across creators)
- Group cannot be deleted if it has outstanding unsettled debts (must show warning)
- Minimum 2 members required for meaningful expense tracking

**File Storage**:
```json
// groups/{groupId}.json
{
  "id": "uuid",
  "name": "Weekend Trip",
  "creatorId": "user_uuid",
  "createdAt": "2026-01-05T10:30:00Z",
  "updatedAt": "2026-01-05T14:20:00Z",
  "currency": "AUD",
  "members": [ /* GroupMember array */ ],
  "expenses": [ /* Expense array */ ],
  "pendingInvitations": [ /* Invitation array */ ]
}
```

---

### 3. GroupMember

Represents a user's membership in a specific group with role information.

**Properties**:
| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | Guid | Yes | - | Unique identifier |
| GroupId | Guid | Yes | Valid Group.Id | Group reference |
| UserId | Guid | Yes | Valid User.Id | User reference |
| Role | enum | Yes | Admin or Member | Member role |
| JoinedAt | DateTime | Yes | - | When user joined group |
| AddedBy | Guid | Yes | Valid User.Id | Who invited/added this member |

**Enums**:
```csharp
public enum GroupRole
{
    Member = 0,  // Standard member
    Admin = 1    // Can invite/remove members, delete group
}
```

**Relationships**:
- Many-to-one with User
- Many-to-one with Group

**Business Rules**:
- User can only be a member of a group once (unique constraint on GroupId + UserId)
- Group creator is automatically Admin
- At least one Admin must exist per group
- Member removal requires Admin role
- Member who created expenses cannot be removed until expenses are deleted

**Embedded in Group File**: Stored as array within group JSON for efficient access.

---

### 4. Invitation

Represents a pending invitation to join a group.

**Properties**:
| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | Guid | Yes | - | Unique identifier |
| GroupId | Guid | Yes | Valid Group.Id | Target group |
| InvitedBy | Guid | Yes | Valid User.Id | Who sent invitation |
| RecipientEmail | string | Conditional | Valid email if phone not provided | Invitee's email |
| RecipientPhone | string | Conditional | E.164 format if email not provided | Invitee's phone |
| Token | string | Yes | Cryptographically secure, unique | Unique invitation token |
| Status | enum | Yes | Pending/Accepted/Expired/Cancelled | Current status |
| CreatedAt | DateTime | Yes | - | When invitation was sent |
| ExpiresAt | DateTime | Yes | > CreatedAt | Invitation expiration (7 days default) |
| AcceptedAt | DateTime | No | - | When invitation was accepted |

**Enums**:
```csharp
public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Cancelled = 3
}
```

**Relationships**:
- Many-to-one with Group
- Many-to-one with User (inviter)

**Business Rules**:
- Either RecipientEmail OR RecipientPhone must be provided (not both required)
- Token must be cryptographically random (256-bit minimum)
- Invitations expire after 7 days by default
- Expired invitations can be resent (generates new token)
- Accepting invitation automatically creates GroupMember record
- One active invitation per email/phone per group

**Embedded in Group File**: Stored as array within group JSON.

---

### 5. Expense

Represents a shared cost that needs to be split among group members.

**Properties**:
| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | Guid | Yes | - | Unique identifier |
| GroupId | Guid | Yes | Valid Group.Id | Group reference |
| Description | string | Yes | 1-200 chars | What was purchased |
| Amount | decimal | Yes | > 0, 2 decimal places | Total expense amount (AUD) |
| PayerId | Guid | Yes | Valid User.Id, member of group | Who paid |
| CreatedAt | DateTime | Yes | - | When expense was created |
| UpdatedAt | DateTime | Yes | - | Last modification |
| CreatedBy | Guid | Yes | Valid User.Id | Who created the entry |
| SplitType | enum | Yes | Even or Custom | How expense is split |

**Enums**:
```csharp
public enum SplitType
{
    Even = 0,    // Split equally among all participants
    Custom = 1   // Custom percentage per participant
}
```

**Relationships**:
- Many-to-one with Group
- Many-to-one with User (payer)
- One-to-many with ExpenseSplit (split details per member)

**Business Rules**:
- Amount must be positive and have max 2 decimal places
- Payer must be a current member of the group
- Expense can only be edited/deleted by creator
- Deleting expense recalculates all group debts
- Description required and non-empty

**Embedded in Group File**: Stored as array within group JSON with nested splits.

---

### 6. ExpenseSplit

Represents an individual member's portion of an expense.

**Properties**:
| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | Guid | Yes | - | Unique identifier |
| ExpenseId | Guid | Yes | Valid Expense.Id | Parent expense |
| MemberId | Guid | Yes | Valid GroupMember.UserId | Who owes this portion |
| Amount | decimal | Yes | >= 0, 2 decimal places | Amount owed (calculated from percentage) |
| Percentage | decimal | Yes | 0-100, 2 decimal places | Percentage of total (0 = excluded) |

**Relationships**:
- Many-to-one with Expense
- Many-to-one with User (via MemberId)

**Business Rules**:
- Sum of all percentages for an expense MUST equal 100%
- Amount calculated as: `Expense.Amount * (Percentage / 100)`
- Percentage of 0% means member is excluded from this expense
- Payer must be included in splits (payer pays their own share)
- Cannot split expense with only the payer (minimum 2 participants)

**Calculation Example**:
```
Expense: $150 dinner, split evenly among 3 people
- Alice (payer): $150 * (33.33 / 100) = $50
- Bob: $150 * (33.33 / 100) = $50
- Carol: $150 * (33.34 / 100) = $50 (extra penny for rounding)
```

**Embedded in Expense**: Stored as array within expense object.

---

### 7. Debt

Represents calculated money owed between two users within a group. This is a **derived/calculated entity**, not directly persisted but computed from expenses.

**Properties**:
| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| GroupId | Guid | Yes | Valid Group.Id | Group context |
| DebtorId | Guid | Yes | Valid User.Id | Who owes money |
| CreditorId | Guid | Yes | Valid User.Id | Who is owed money |
| Amount | decimal | Yes | > 0, 2 decimal places | Amount owed (AUD) |
| IsSimplified | bool | Yes | - | Whether this is from simplified calculation |

**Relationships**:
- Many-to-one with Group
- Many-to-one with User (debtor)
- Many-to-one with User (creditor)

**Business Rules**:
- Calculated dynamically from expenses when requested
- DebtorId and CreditorId must be different
- Amount always positive (direction indicated by debtor/creditor)
- Simplified debts reduce total transaction count

**Calculation Logic**:
```csharp
// Raw debts: Direct from each expense
foreach (var expense in group.Expenses)
{
    foreach (var split in expense.Splits)
    {
        if (split.MemberId != expense.PayerId)
        {
            debts.Add(new Debt
            {
                DebtorId = split.MemberId,
                CreditorId = expense.PayerId,
                Amount = split.Amount,
                IsSimplified = false
            });
        }
    }
}

// Simplified debts: Use Min-Cash-Flow algorithm (see research.md)
var simplifiedDebts = DebtSimplificationService.SimplifyDebts(members, expenses);
```

**Not Persisted**: Calculated on-demand for performance and consistency.

---

### 8. Settlement

Represents a recorded payment between users to settle a debt.

**Properties**:
| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | Guid | Yes | - | Unique identifier |
| GroupId | Guid | Yes | Valid Group.Id | Group context |
| PayerId | Guid | Yes | Valid User.Id | Who made payment |
| RecipientId | Guid | Yes | Valid User.Id | Who received payment |
| Amount | decimal | Yes | > 0, 2 decimal places | Amount paid (AUD) |
| SettledAt | DateTime | Yes | - | When payment was recorded |
| RecordedBy | Guid | Yes | Valid User.Id | Who recorded this settlement |
| Notes | string | No | Max 500 chars | Optional payment notes |
| Status | enum | Yes | Pending or Completed | Settlement status |

**Enums**:
```csharp
public enum SettlementStatus
{
    Pending = 0,     // Recorded but not confirmed
    Completed = 1    // Both parties confirmed
}
```

**Relationships**:
- Many-to-one with Group
- Many-to-one with User (payer)
- Many-to-one with User (recipient)

**Business Rules**:
- PayerId and RecipientId must be different
- Both users must be current group members
- Settlement reduces calculated debts but doesn't modify expenses
- Settlements can be disputed (future feature - marked as NEEDS CLARIFICATION in spec)
- Optional: Require recipient confirmation before marking Completed

**File Storage**: Could be embedded in group file or separate for audit trail. Recommend separate file for settlements:
```json
// settlements/{groupId}.json
{
  "groupId": "uuid",
  "settlements": [
    {
      "id": "uuid",
      "payerId": "user_uuid",
      "recipientId": "user_uuid",
      "amount": 45.50,
      "settledAt": "2026-01-05T16:00:00Z",
      "recordedBy": "user_uuid",
      "notes": "Venmo transfer",
      "status": "Completed"
    }
  ]
}
```

---

## Validation Rules Summary

### Global Constraints
- All monetary amounts: 2 decimal places, positive values
- All GUIDs: RFC 4122 compliant
- All timestamps: ISO 8601 format with UTC timezone
- All strings: No leading/trailing whitespace
- Currency: Always "AUD" (hardcoded for MVP)

### Key Invariants
1. **Expense Split Integrity**: Sum of all ExpenseSplit percentages for an expense = 100%
2. **Group Membership**: Cannot add expense for non-member
3. **Payer Participation**: Payer must be included in expense splits
4. **Balance Conservation**: Sum of all debts in group = 0 (money in = money out)
5. **Invitation Uniqueness**: One active invitation per recipient per group

### Cascading Rules
- Deleting User → Soft delete, keep expense history
- Deleting Group → Require confirmation, archive data
- Removing GroupMember → Prevent if member has expenses (must delete expenses first)
- Deleting Expense → Recalculate all debts

---

## File Storage Structure in Google Drive

```
costsharing/
├── users/
│   └── {userId}.json                    # User profiles
├── groups/
│   └── {groupId}.json                   # Group data (members, expenses, invitations)
├── settlements/
│   └── {groupId}.json                   # Settlement records per group
└── metadata/
    └── user_groups_{userId}.json        # User's group memberships index
```

### Group File Structure Example
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Weekend Trip",
  "creatorId": "660e8400-e29b-41d4-a716-446655440000",
  "createdAt": "2026-01-05T10:00:00Z",
  "updatedAt": "2026-01-05T15:30:00Z",
  "currency": "AUD",
  "members": [
    {
      "id": "mem-1",
      "userId": "660e8400-e29b-41d4-a716-446655440000",
      "role": "Admin",
      "joinedAt": "2026-01-05T10:00:00Z",
      "addedBy": "660e8400-e29b-41d4-a716-446655440000"
    }
  ],
  "expenses": [
    {
      "id": "exp-1",
      "description": "Dinner at restaurant",
      "amount": 120.00,
      "payerId": "660e8400-e29b-41d4-a716-446655440000",
      "createdAt": "2026-01-05T12:00:00Z",
      "updatedAt": "2026-01-05T12:00:00Z",
      "createdBy": "660e8400-e29b-41d4-a716-446655440000",
      "splitType": "Even",
      "splits": [
        {
          "id": "split-1",
          "memberId": "660e8400-e29b-41d4-a716-446655440000",
          "amount": 40.00,
          "percentage": 33.33
        }
      ]
    }
  ],
  "pendingInvitations": []
}
```

---

## Migration & Versioning

**Schema Version**: 1.0.0

Future schema changes will be handled via:
1. Version field in each file
2. Migration scripts to transform old format to new
3. Backward compatibility for at least one major version

---

## Performance Considerations

- **Group File Size**: Estimated 10KB per group (50 members, 100 expenses) → Acceptable for Google Drive
- **Concurrent Access**: Optimistic concurrency with retry (see research.md)
- **Caching**: Server-side cache for frequently accessed groups (TTL: 5 minutes)
- **Indexing**: Maintain separate index file for user's groups to avoid scanning all files

---

## Testing Requirements

Each entity must have:
- Unit tests for validation rules
- Integration tests for CRUD operations
- Concurrency tests for file locking
- Performance tests for 50-member groups with 100 expenses
