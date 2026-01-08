# Feature Specification: Cost-Sharing Application

**Feature Branch**: `001-cost-sharing-app`  
**Created**: 2026-01-05  
**Status**: Draft  
**Input**: User description: "Build a SPA that can be used for cost sharing between different people. In which I can create a group, add people to the group using their email address or their phone number. Then a link would be sent to them to accept the invitation and get added to the group. I need to be able to add a cost of something (description and the price) and be able to either split it evenly between the members of the team or split it with a particular share from 0 to 100. then it should be able to simplify the group debts meaning automatically combines debts to reduce the total number of repayments between group members."

**⚠️ IMPLEMENTATION NOTE**: This specification document describes the original architecture with Google Drive storage and synchronization. The actual implementation uses **local SQLite storage only** - Google Drive integration was removed during development. This document is preserved for historical/reference purposes.

## Clarifications

### Session 2026-01-05

- Q: User authentication is required but the authentication method is not specified. Which authentication method(s) should be supported? → A: Email/password + passwordless magic link (email-based one-time login links)
- Q: The system handles monetary amounts but currency handling is not specified. Which currency model should be used? → A: Single currency only (AUD assumed for all transactions)
- Q: When a user pays for an expense, does the payer participate in the split or are they automatically excluded? → A: The payer participates in the split
- Q: The system needs to send invitation emails and SMS messages, but the implementation approach is not specified. How should email/SMS delivery be implemented? → A: Third-party service (use SendGrid for email, Twilio for SMS)
- Q: The system persists data reliably but the storage technology is not specified. What storage solution should be used? → A: File-based in Google Drive
- Q: What deployment architecture should be used? Should it be a hosted web application or a distributed application? → A: .NET MAUI cross-platform app (native mobile + desktop, no hosting required, local-first with Google Drive sync)
- Q: Should there be a separate backend server or should apps connect directly to Google Drive? → A: No backend server (apps connect directly to Google Drive, fully peer-to-peer)
- Q: How will users obtain and install the app? → A: Direct download (website/GitHub releases - .exe/.apk/.dmg files, users install manually)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Group Creation and Basic Management (Priority: P1) 🎯 MVP

A user creates a cost-sharing group to track shared expenses among friends, family, or colleagues.

**Why this priority**: This is the foundational capability - without groups, no cost-sharing can occur. This delivers immediate value by allowing users to organize their shared expense tracking.

**Independent Test**: Can be fully tested by creating a group, viewing its details, and managing basic group information. Delivers the value of having an organized space for tracking shared expenses.

**Acceptance Scenarios**:

1. **Given** I am on the application homepage, **When** I click "Create Group" and enter a group name "Weekend Trip", **Then** a new group is created and I am shown the group dashboard
2. **Given** I have created a group, **When** I view the group dashboard, **Then** I see the group name, creation date, my role as creator/admin, and an empty member list
3. **Given** I am viewing my group, **When** I update the group name to "Summer Vacation 2026", **Then** the group name is updated and all members see the new name
4. **Given** I am a group admin, **When** I choose to delete the group, **Then** I receive a confirmation prompt and upon confirmation, the group is permanently deleted

---

### User Story 2 - Member Invitation and Management (Priority: P1) 🎯 MVP

A group creator invites members via email or phone number, and invited users accept invitations to join the group.

**Why this priority**: Groups are useless without members. This enables the core collaborative aspect of cost-sharing. Combined with US1, it creates a complete MVP for group formation.

**Independent Test**: Can be fully tested by creating a group, sending invitations via email/phone, and having recipients accept invitations. Delivers the value of collaborative expense tracking.

**Acceptance Scenarios**:

1. **Given** I am viewing my group dashboard, **When** I click "Invite Member" and enter "friend@example.com", **Then** an invitation email is sent with a unique invitation link
2. **Given** I am inviting a member, **When** I enter a phone number "+1234567890", **Then** an invitation SMS is sent with a unique invitation link
3. **Given** I receive an invitation link, **When** I click the link and I don't have an account, **Then** I am prompted to create an account and then automatically join the group
4. **Given** I receive an invitation link, **When** I click the link and I already have an account, **Then** I am automatically added to the group upon login
5. **Given** I am a group member, **When** I view the group members list, **Then** I see all accepted members with their names and contact information
6. **Given** I am a group admin, **When** I remove a member from the group, **Then** that member loses access to the group and all its data
7. **Given** I have sent an invitation that hasn't been accepted, **When** I view pending invitations, **Then** I see the invitation status and can resend or cancel it

---

### User Story 3 - Add and Split Expenses Evenly (Priority: P1) 🎯 MVP

A group member adds an expense and splits it evenly among selected group members.

**Why this priority**: This is the core value proposition - tracking who owes what. Even splitting is the most common use case and delivers immediate utility. With US1 and US2, this completes the essential MVP.

**Independent Test**: Can be fully tested by adding an expense with description and amount, splitting it evenly among members, and verifying calculated debts. Delivers the value of knowing who owes whom and how much.

**Acceptance Scenarios**:

1. **Given** I am viewing my group, **When** I click "Add Expense" and enter description "Dinner at Restaurant" and amount "$120.00", **Then** the expense is created and ready to be split
2. **Given** I am adding an expense of $120.00, **When** I select "Split Evenly" among all 4 group members (including myself as payer), **Then** each member's share is calculated as $30.00
3. **Given** I am adding an expense, **When** I select only 3 out of 5 members to split with (including myself), **Then** the expense is split evenly among only those 3 members
4. **Given** I have added an expense I paid for, **When** I split it among members, **Then** I see who owes me money and how much
5. **Given** I am viewing the group dashboard, **When** I look at the expenses list, **Then** I see all expenses with description, amount, payer, and split details
6. **Given** I am viewing an expense I created, **When** I choose to edit it, **Then** I can modify the description, amount, and split configuration
7. **Given** I am viewing an expense I created, **When** I choose to delete it, **Then** the expense is removed and debts are recalculated

---

### User Story 4 - Custom Percentage Split (Priority: P2)

A group member adds an expense and splits it with custom percentages (0-100%) for each participant.

**Why this priority**: This handles more complex scenarios where costs aren't equal (e.g., different meal prices, unequal room sizes). It's less common than even splitting but important for real-world use cases.

**Independent Test**: Can be fully tested by adding an expense and assigning custom percentage shares to different members, verifying that shares total 100% and debts are calculated correctly.

**Acceptance Scenarios**:

1. **Given** I am adding an expense of $100.00, **When** I select "Custom Split" and assign Member A: 50%, Member B: 30%, Member C: 20%, **Then** the shares are $50, $30, and $20 respectively
2. **Given** I am using custom split, **When** my percentage allocations don't total 100%, **Then** I see a validation error and cannot save the expense
3. **Given** I am using custom split, **When** I set a member's share to 0%, **Then** that member is excluded from the expense and owes nothing
4. **Given** I am adding a $150 expense with custom split, **When** I assign percentages (60%, 25%, 15%), **Then** the system calculates exact amounts ($90, $37.50, $22.50) correctly
5. **Given** I am viewing my group expenses, **When** I see a custom-split expense, **Then** I can view the percentage and amount breakdown for each member

---

### User Story 5 - Debt Simplification (Priority: P2)

The system automatically simplifies group debts to minimize the number of transactions needed to settle all balances.

**Why this priority**: This is a key differentiator that saves time and reduces complexity. Instead of everyone paying the person who covered each expense, the system figures out the optimal payment flow.

**Independent Test**: Can be fully tested by creating multiple expenses with different payers, then viewing the simplified settlement plan that shows minimum transactions needed.

**Acceptance Scenarios**:

1. **Given** Alice paid $60, Bob paid $40, and both expenses were split evenly between Alice, Bob, and Carol, **When** I view simplified debts, **Then** Carol owes Alice $10 and Bob $10 (instead of Alice owing Bob or vice versa)
2. **Given** multiple complex expenses exist, **When** the system simplifies debts, **Then** the total number of transactions is minimized while maintaining correct balances
3. **Given** Alice paid $100 split between A, B, C (each owes $33.33) and Bob paid $90 split between A, B, C (each owes $30), **When** debts are simplified, **Then** the system shows A owes Alice $3.33 and Bob gets paid back from the group
4. **Given** I am viewing the simplified debts page, **When** I see the settlement plan, **Then** I see a clear list like "Bob pays Alice $45" with minimal transactions
5. **Given** debts are simplified, **When** a new expense is added, **Then** the simplified debt view is automatically recalculated
6. **Given** I am viewing simplified debts, **When** I mark a debt as "Settled", **Then** that transaction is recorded and the debt is removed from the active list

---

### User Story 6 - Personal Balance and History (Priority: P3)

A user views their personal balance across all groups and their transaction history.

**Why this priority**: This provides users with a comprehensive view of their financial position and activity. It's valuable but not essential for core cost-sharing functionality.

**Independent Test**: Can be fully tested by viewing a dashboard that shows total amounts owed and owing across all groups, with a history of all expenses the user is involved in.

**Acceptance Scenarios**:

1. **Given** I am logged into the application, **When** I view my dashboard, **Then** I see my total balance (how much I owe minus how much I'm owed) across all groups
2. **Given** I am viewing my dashboard, **When** I look at my group list, **Then** I see my balance for each individual group
3. **Given** I am viewing my transaction history, **When** I filter by date range, **Then** I see all expenses I paid for or participated in during that period
4. **Given** I am viewing an expense in my history, **When** I see my portion, **Then** I know whether I paid it or owe money for it
5. **Given** I am viewing my balances, **When** I am owed money overall, **Then** the balance is shown in green/positive
6. **Given** I am viewing my balances, **When** I owe money overall, **Then** the balance is shown in red/negative

---

### Edge Cases

- What happens when an invited user never accepts the invitation? (System should track pending invitations with expiration)
- How does the system handle when a member who owes money is removed from the group? (Debts should be settled or frozen before removal)
- What happens if someone tries to split an expense with percentages that include decimal precision issues? (System should handle rounding to 2 decimal places)
- What happens when an expense is deleted after debts have been simplified? (System should recalculate all balances and simplifications)
- How does the system handle when someone marks a debt as paid but the recipient disputes it? (NEEDS CLARIFICATION: dispute resolution mechanism needed?)
- What happens when a group has only one member? (System should prevent or warn about meaningless operations)
- How does the system handle extremely large groups (50+ members)? (Should test performance and UX with pagination/search)
- How does the system handle concurrent modifications with file-based storage in Google Drive? (Must implement file locking or conflict resolution to prevent data corruption)
- How does the system handle concurrent modifications with file-based storage in Google Drive? (Must implement file locking or conflict resolution to prevent data corruption)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow authenticated users to create groups with a unique name
- **FR-002**: System MUST allow group admins to invite members via email address or phone number
- **FR-003**: System MUST generate unique, time-limited invitation links and send them via email (SendGrid) or SMS (Twilio)
- **FR-004**: System MUST allow invited users to accept invitations and join groups
- **FR-005**: System MUST allow group members to add expenses with description and amount
- **FR-006**: System MUST support even splitting of expenses among selected group members
- **FR-007**: System MUST include the payer as a participant in expense splits (payer pays their own share)
- **FR-008**: System MUST support custom percentage-based splitting (0-100%) with validation that total equals 100%
- **FR-008**: System MUST calculate and track debts between group members based on expenses
- **FR-009**: System MUST implement debt simplification algorithm to minimize transaction count
- **FR-010**: System MUST allow users to view simplified settlement plans for groups
- **FR-011**: System MUST allow users to mark debts asvia email/password and passwordless magic link (email-based one-time login links)
- **FR-012**: System MUST allow group admins to remove members from groups
- **FR-013**: System MUST allow expense creators to edit or delete their expenses
- **FR-014**: System MUST recalculate all debts when expenses are added, modified, or deleted
- **FR-015**: System MUST display user's total balance across all groups
- **FR-016**: System MUST persist all data (groups, members, expenses, debts) reliably using file-based storage in Google Drive accessed directly from the application
- **FR-017**: System MUST support user authentication via email/password and passwordless magic link (email-based one-time login links) handled within the application
- **FR-018**: System MUST enforce access control (only group members can view group data)
- **FR-019**: System MUST handle monetary calculations in AUD with appropriate precision (2 decimal places)
- **FR-020**: System MUST provide transaction history for users
- **FR-021**: System MUST display all monetary amounts with AUD currency symbol ($)
- **FR-022**: Application MUST be distributed as installable packages (.exe for Windows, .apk for Android, .dmg for macOS, .ipa for iOS) via direct download

### Key Entities *(include if feature involves data)*

- **User**: Represents an application user with authentication credentials (email/phone), profile information; can belong to multiple groups
- **Group**: Represents a cost-sharing group with name, creator, creation date; contains members and expenses
- **GroupMember**: Represents a user's membership in a group with role (admin/member), join date; links users to groups
- **Invitation**: Represents a pending group invitation with unique token, recipient contact (email/phone), expiration date, status (pending/accepted/expired)
- **Expense**: Represents a shared cost with description, amount, payer (user), date, group; contains split information
- **ExpenseSplit**: Represents an individual's portion of an expense with member reference, amount or percentage, split type (even/custom)
- **Debt**: Represents money owed between two users within a group with debtor, creditor, amount; can be calculated or simplified
- **Settlement**: Represents a recorded payment between users with payer, recipient, amount, date, status (pending/completed)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create a group and invite members in under 2 minutes
- **SC-002**: Users can add an expense and split it evenly in under 1 minute
- **SC-003**: Debt simplification reduces transaction count by at least 30% for groups with 4+ members and 5+ expenses
- **SC-004**: 90% of invited users successfully join groups within 24 hours of invitation
- **SC-005**: System correctly calculates all balances with 100% accuracy (no rounding errors that accumulate)
- **SC-006**: Application loads and displays group dashboard in under 2 seconds
- **SC-007**: System handles groups with up to 50 members without performance degradation
- **SC-008**: 95% of users successfully complete their first expense entry without assistance
- **SC-009**: Mobile responsive interface works correctly on devices with screen widths from 320px to 1920px
- **SC-010**: Zero data loss or corruption during concurrent expense additions by multiple group members
