# Research: Cost-Sharing Application

**Date**: 2026-01-05  
**Purpose**: Resolve technical unknowns and establish best practices for implementation

## Research Areas

### 1. Google Drive File-Based Storage for Collaborative SPA

**Challenge**: Implementing reliable, concurrent file-based storage in Google Drive for a multi-user web application.

#### Decision: Per-Group JSON Files with Optimistic Concurrency

**Rationale**:
- Each group stored as separate JSON file in user's Google Drive
- Enables granular access control per group
- Reduces blast radius of concurrent modifications
- Leverages Google Drive's built-in versioning and sharing

**Implementation Approach**:
1. **File Structure**: `costsharing/groups/{groupId}.json` contains all group data (members, expenses, debts)
2. **Concurrency Strategy**: Optimistic concurrency control using Google Drive's `modifiedTime` metadata
   - Read file with current `modifiedTime`
   - Perform modifications in memory
   - Write back with `If-Match` condition on `modifiedTime`
   - If conflict detected (412 Precondition Failed), retry with exponential backoff
3. **Performance Optimization**: Cache frequently accessed groups in memory (server-side) with TTL, invalidate on write
4. **Rate Limiting**: Implement request batching and caching to stay within Google Drive API limits (1000 req/100s/user)

**Alternatives Considered**:
- **Single monolithic file**: Rejected due to high conflict probability and performance issues with large datasets
- **Traditional database (SQL Server/PostgreSQL)**: Rejected per user requirement for file-based storage
- **Cloud Firestore/DynamoDB**: Rejected as not file-based

**Trade-offs**:
- ✅ Pros: User owns their data, simple backup/sharing, leverages Google's infrastructure
- ⚠️ Cons: Network latency, API rate limits, complex concurrency handling, limited query capabilities

**Best Practices**:
- Implement retry logic with exponential backoff for transient failures
- Use Google Drive's change notifications (webhooks) to invalidate caches
- Store user-specific data (auth tokens) separately from group data
- Implement file locking pattern for critical sections (expense creation, debt calculation)

---

### 2. SendGrid and Twilio Integration for Invitations

**Challenge**: Reliable email and SMS delivery for group invitations and notifications.

#### Decision: SendGrid for Email, Twilio for SMS with Template-Based Messaging

**Rationale**:
- Industry-standard services with high deliverability rates
- Built-in templates, tracking, and analytics
- .NET SDK support for easy integration
- Generous free tiers for MVP development

**Implementation Approach**:

**SendGrid (Email)**:
1. Use Dynamic Templates for consistent branding
2. Template variables: `{inviterName}`, `{groupName}`, `{invitationLink}`, `{expirationDate}`
3. Track email opens and clicks for invitation acceptance analytics
4. Implement rate limiting (100 emails/day on free tier)

**Twilio (SMS)**:
1. Use Programmable Messaging API
2. SMS template: "You've been invited to join {groupName} on CostSharing by {inviterName}. Accept: {shortLink} (Expires: {date})"
3. Implement URL shortening for SMS links (256 char limit)
4. Handle delivery status callbacks for failed messages

**Configuration**:
```csharp
// appsettings.json
{
  "SendGrid": {
    "ApiKey": "ENV:SENDGRID_API_KEY",
    "InvitationTemplateId": "d-xxxxx",
    "FromEmail": "noreply@costsharing.app",
    "FromName": "CostSharing"
  },
  "Twilio": {
    "AccountSid": "ENV:TWILIO_ACCOUNT_SID",
    "AuthToken": "ENV:TWILIO_AUTH_TOKEN",
    "PhoneNumber": "+1234567890"
  }
}
```

**Alternatives Considered**:
- **SMTP Server**: Rejected due to deliverability issues, no tracking, complexity
- **AWS SES**: Considered but SendGrid offers better .NET integration
- **Custom SMS Gateway**: Rejected due to compliance complexity (GDPR, TCPA)

**Error Handling**:
- Queue failed messages for retry (3 attempts with exponential backoff)
- Store invitation status (sent/delivered/failed) in group data
- Provide manual resend option for failed invitations
- Log all messaging activity for debugging

**Best Practices**:
- Use environment variables for API keys (never commit secrets)
- Implement exponential backoff for rate limit errors
- Validate email addresses before sending (regex + DNS MX lookup)
- Validate phone numbers using Twilio Lookup API
- Include unsubscribe links in emails (even transactional)
- Use test mode in development (SendGrid sandbox, Twilio test credentials)

---

### 3. Debt Simplification Algorithm

**Challenge**: Minimize the number of transactions required to settle all debts in a group while maintaining mathematical correctness.

#### Decision: Min-Cash-Flow Algorithm (Greedy Approach)

**Rationale**:
- Proven algorithm for multi-party debt settlement
- Reduces n*(n-1) potential transactions to minimum possible
- O(n²) time complexity acceptable for groups up to 50 members
- Mathematically guarantees correct balances

**Algorithm Overview**:
1. **Calculate Net Balance**: For each member, compute: `balance = total_paid - total_owed`
2. **Identify Debtors and Creditors**: 
   - Creditors: `balance > 0` (owed money)
   - Debtors: `balance < 0` (owe money)
   - Balanced: `balance = 0` (settled)
3. **Greedy Matching**:
   - Find max creditor (person owed most)
   - Find max debtor (person owing most)
   - Create transaction: debtor pays creditor min(|debtor_balance|, creditor_balance)
   - Update balances and repeat until all balanced

**Example**:
```
Initial State:
- Alice paid $100, owes $30 → balance: +$70
- Bob paid $50, owes $80 → balance: -$30
- Carol paid $0, owes $40 → balance: -$40

Step 1: Max creditor (Alice: +$70), Max debtor (Carol: -$40)
  → Carol pays Alice $40
  → Balances: Alice: +$30, Bob: -$30, Carol: $0

Step 2: Max creditor (Alice: +$30), Max debtor (Bob: -$30)
  → Bob pays Alice $30
  → Balances: Alice: $0, Bob: $0, Carol: $0

Result: 2 transactions (Carol→Alice $40, Bob→Alice $30)
```

**Implementation** (C#):
```csharp
public class DebtSimplificationService
{
    public List<Settlement> SimplifyDebts(List<Member> members, List<Expense> expenses)
    {
        // Step 1: Calculate net balances
        var balances = CalculateNetBalances(members, expenses);
        
        // Step 2: Separate creditors and debtors
        var creditors = balances.Where(b => b.Amount > 0).OrderByDescending(b => b.Amount).ToList();
        var debtors = balances.Where(b => b.Amount < 0).OrderBy(b => b.Amount).ToList();
        
        var settlements = new List<Settlement>();
        
        // Step 3: Greedy matching
        while (creditors.Any() && debtors.Any())
        {
            var maxCreditor = creditors.First();
            var maxDebtor = debtors.First();
            
            var settleAmount = Math.Min(maxCreditor.Amount, Math.Abs(maxDebtor.Amount));
            
            settlements.Add(new Settlement
            {
                From = maxDebtor.MemberId,
                To = maxCreditor.MemberId,
                Amount = settleAmount
            });
            
            maxCreditor.Amount -= settleAmount;
            maxDebtor.Amount += settleAmount;
            
            if (maxCreditor.Amount == 0) creditors.RemoveAt(0);
            if (maxDebtor.Amount == 0) debtors.RemoveAt(0);
        }
        
        return settlements;
    }
    
    private List<Balance> CalculateNetBalances(List<Member> members, List<Expense> expenses)
    {
        var balances = members.ToDictionary(m => m.Id, m => 0m);
        
        foreach (var expense in expenses)
        {
            balances[expense.PayerId] += expense.Amount;
            
            foreach (var split in expense.Splits)
            {
                balances[split.MemberId] -= split.Amount;
            }
        }
        
        return balances.Select(b => new Balance
        {
            MemberId = b.Key,
            Amount = b.Value
        }).ToList();
    }
}
```

**Alternatives Considered**:
- **Pairwise Direct Settlement**: Simple but creates n*(n-1)/2 transactions
- **Network Flow Algorithm**: More optimal but overkill for small groups
- **Graph-Based Minimum Spanning Tree**: Complex implementation, marginal benefit

**Performance**:
- Time: O(n²) where n = number of members
- Space: O(n)
- For 50 members: ~2,500 operations (negligible)

**Testing Strategy**:
- Unit tests with known examples (3-member, 5-member groups)
- Property-based testing: sum of settlements equals sum of initial imbalances
- Edge cases: single member, all balanced, circular debts

**Best Practices**:
- Always recalculate debts from scratch (don't maintain running state)
- Round to 2 decimal places consistently to avoid floating-point errors
- Cache simplified debts until expense changes
- Provide both simplified and detailed debt views

---

### 4. Authentication Implementation

**Challenge**: Implement secure email/password + passwordless magic link authentication.

#### Decision: ASP.NET Core Identity + Custom Magic Link Provider

**Rationale**:
- ASP.NET Core Identity provides robust auth framework
- Built-in password hashing (PBKDF2), account lockout, 2FA support
- Easy to extend with custom token providers
- Integrates seamlessly with ASP.NET Core middleware

**Implementation Approach**:

**Email/Password**:
```csharp
services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddTokenProvider<MagicLinkTokenProvider>("MagicLink");
```

**Magic Link (Passwordless)**:
1. User enters email address
2. Generate time-limited token (15-min expiration)
3. Send email via SendGrid with magic link: `https://app.costsharing.com/auth/magic?token={token}&email={email}`
4. User clicks link → validate token → auto sign-in
5. Token is single-use and invalidated after successful login

**Security Measures**:
- Tokens stored with SHA256 hash (not plain text)
- Rate limiting: 3 magic link requests per email per hour
- HTTPS only for auth endpoints
- HttpOnly, Secure cookies for session management
- CORS configuration for SPA frontend

**Best Practices**:
- Use refresh tokens for long-lived sessions
- Implement sliding expiration for auth cookies
- Log all authentication events (success/failure)
- Support account email verification before first use

---

### 5. React Best Practices for .NET Core SPA

**Challenge**: Optimize React SPA structure and state management for cost-sharing features.

#### Decision: React 18 with Context API + Custom Hooks

**Rationale**:
- Context API sufficient for moderate state complexity (avoids Redux overhead)
- Custom hooks promote reusability and testability
- React 18 concurrent features improve UX (Suspense, transitions)
- TypeScript for type safety with DTOs

**State Management Strategy**:
```typescript
// contexts/GroupContext.tsx
export const GroupContext = createContext<GroupContextType>();

export const GroupProvider = ({ children }) => {
  const [groups, setGroups] = useState<Group[]>([]);
  const [selectedGroup, setSelectedGroup] = useState<Group | null>(null);
  
  // CRUD operations
  const createGroup = async (name: string) => { /* ... */ };
  const updateGroup = async (groupId: string, updates: Partial<Group>) => { /* ... */ };
  const deleteGroup = async (groupId: string) => { /* ... */ };
  
  return (
    <GroupContext.Provider value={{ groups, selectedGroup, createGroup, /* ... */ }}>
      {children}
    </GroupContext.Provider>
  );
};

// Custom hook
export const useGroup = () => {
  const context = useContext(GroupContext);
  if (!context) throw new Error("useGroup must be used within GroupProvider");
  return context;
};
```

**Component Structure**:
- **Container Components**: Handle data fetching, state management
- **Presentational Components**: Pure UI, receive props only
- **Custom Hooks**: Encapsulate business logic (useDebts, useExpenses, useAuth)

**Performance Optimization**:
- `React.memo` for expensive list renders (MemberList, ExpenseList)
- `useMemo` for debt calculations
- `useCallback` for event handlers passed to children
- Code splitting with `React.lazy` for routes

**API Integration**:
```typescript
// services/api.ts
import axios from 'axios';

const api = axios.create({
  baseURL: process.env.REACT_APP_API_URL,
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true // Send cookies
});

// Add auth token interceptor
api.interceptors.request.use(config => {
  const token = localStorage.getItem('authToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export default api;
```

**Best Practices**:
- Use TypeScript strict mode
- Implement error boundaries for graceful error handling
- Follow component naming: PascalCase for components, camelCase for instances
- Keep components under 300 lines (split if larger)
- Use ESLint + Prettier for consistent formatting
- Write component tests with React Testing Library

---

## Summary of Decisions

| Research Area | Decision | Key Rationale |
|---------------|----------|---------------|
| **Storage** | Google Drive per-group JSON files with optimistic concurrency | User data ownership, granular access control, built-in versioning |
| **Messaging** | SendGrid (email) + Twilio (SMS) with templates | Industry standard, high deliverability, .NET SDK support |
| **Debt Algorithm** | Min-Cash-Flow greedy algorithm | Minimizes transactions, O(n²) acceptable, mathematically sound |
| **Authentication** | ASP.NET Core Identity + custom magic link provider | Robust framework, extensible, secure token handling |
| **Frontend** | React 18 with Context API + custom hooks | Sufficient for complexity, performant, maintainable |

## Implementation Readiness

All technical unknowns have been resolved with clear implementation paths. Ready to proceed to Phase 1: Design (data model, contracts, quickstart).
