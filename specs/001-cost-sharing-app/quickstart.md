# Quick Start Guide: Cost-Sharing Application

**Date**: 2026-01-05  
**Purpose**: Get developers up and running quickly with the cost-sharing application

## Prerequisites

- **.NET SDK**: 8.0 or later ([Download](https://dotnet.microsoft.com/download))
- **Node.js**: 18.x or later ([Download](https://nodejs.org/))
- **Git**: Latest version
- **Google Cloud Account**: For Google Drive API access
- **SendGrid Account**: For email functionality (free tier available)
- **Twilio Account**: For SMS functionality (free trial available)
- **IDE**: Visual Studio 2022, VS Code, or Rider

## Repository Structure

```
cost-sharing/
├── backend/                      # .NET Core Web API
│   ├── src/
│   │   ├── CostSharing.API/     # API controllers & middleware
│   │   ├── CostSharing.Core/    # Business logic & domain
│   │   ├── CostSharing.Infrastructure/  # External integrations
│   │   └── CostSharing.Shared/  # DTOs & contracts
│   └── tests/                    # Backend tests
├── frontend/                     # React SPA
│   ├── src/                      # React components & services
│   └── tests/                    # Frontend tests
├── specs/                        # Feature specifications
└── .github/                      # CI/CD workflows
```

## 1. Clone and Setup

```bash
# Clone the repository
git clone https://github.com/your-org/cost-sharing.git
cd cost-sharing

# Checkout the feature branch
git checkout 001-cost-sharing-app
```

## 2. Backend Setup

### Install Dependencies

```bash
cd backend/src/CostSharing.API
dotnet restore
```

### Configure Environment Variables

Create `appsettings.Development.json` in `backend/src/CostSharing.API/`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "GoogleDrive": "See Google Drive Setup below"
  },
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-here-replace-in-production",
    "Issuer": "costsharing-api",
    "Audience": "costsharing-app",
    "ExpirationMinutes": 60
  },
  "SendGrid": {
    "ApiKey": "YOUR_SENDGRID_API_KEY",
    "InvitationTemplateId": "d-xxxxx",
    "FromEmail": "noreply@localhost",
    "FromName": "CostSharing Dev"
  },
  "Twilio": {
    "AccountSid": "YOUR_TWILIO_ACCOUNT_SID",
    "AuthToken": "YOUR_TWILIO_AUTH_TOKEN",
    "PhoneNumber": "+1234567890"
  },
  "GoogleDrive": {
    "ApplicationName": "CostSharing",
    "CredentialsPath": "path/to/service-account-credentials.json"
  },
  "AllowedOrigins": ["http://localhost:3000"]
}
```

**⚠️ Important**: Never commit this file with real credentials. Add it to `.gitignore`.

### Google Drive API Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project: "CostSharing Dev"
3. Enable Google Drive API
4. Create Service Account credentials:
   - Navigate to "APIs & Services" → "Credentials"
   - Click "Create Credentials" → "Service Account"
   - Download JSON key file
   - Save as `backend/src/CostSharing.API/google-credentials.json`
5. Share a Google Drive folder with the service account email

### SendGrid Setup

1. Sign up at [SendGrid](https://sendgrid.com/) (free tier: 100 emails/day)
2. Create API Key: Settings → API Keys → Create API Key
3. Create email template: Email API → Dynamic Templates
4. Copy template ID and API key to `appsettings.Development.json`

### Twilio Setup

1. Sign up at [Twilio](https://www.twilio.com/) (free trial includes SMS credits)
2. Get Account SID and Auth Token from dashboard
3. Get a Twilio phone number
4. Add credentials to `appsettings.Development.json`

### Run Backend

```bash
cd backend/src/CostSharing.API
dotnet run

# Backend should start on https://localhost:5001
```

### Verify Backend

```bash
# Health check
curl https://localhost:5001/health

# Should return: {"status": "healthy"}
```

## 3. Frontend Setup

### Install Dependencies

```bash
cd frontend
npm install
```

### Configure Environment

Create `.env.development` in `frontend/`:

```env
REACT_APP_API_URL=https://localhost:5001/v1
REACT_APP_ENV=development
```

### Run Frontend

```bash
npm start

# Frontend should start on http://localhost:3000
```

### Verify Frontend

Open browser to `http://localhost:3000` - you should see the login page.

## 4. Running Tests

### Backend Tests

```bash
# Unit tests
cd backend/tests/CostSharing.UnitTests
dotnet test

# Integration tests
cd backend/tests/CostSharing.IntegrationTests
dotnet test

# All tests
cd backend
dotnet test
```

### Frontend Tests

```bash
cd frontend

# Unit tests
npm test

# Coverage report
npm run test:coverage

# E2E tests (requires both backend and frontend running)
npm run test:e2e
```

## 5. Code Quality Checks

### Backend Linting

```bash
cd backend

# Run Roslyn analyzers
dotnet build /p:TreatWarningsAsErrors=true

# Format code
dotnet format
```

### Frontend Linting

```bash
cd frontend

# ESLint check
npm run lint

# ESLint fix
npm run lint:fix

# Prettier format
npm run format
```

## 6. Database (Google Drive Files)

### File Structure

The application creates the following structure in Google Drive:

```
costsharing/
├── users/
│   └── {userId}.json
├── groups/
│   └── {groupId}.json
├── settlements/
│   └── {groupId}.json
└── metadata/
    └── user_groups_{userId}.json
```

### Seed Development Data

Run the seeding script to create test users and groups:

```bash
cd backend/src/CostSharing.API
dotnet run -- seed-data

# Creates:
# - 3 test users (test1@example.com, test2@example.com, test3@example.com)
# - 2 test groups with expenses
# - Password for all test users: "TestPass123!"
```

## 7. Common Development Tasks

### Create a New Component

```bash
cd frontend/src/components

# Create component directory
mkdir my-component
cd my-component

# Create files
touch MyComponent.tsx
touch MyComponent.test.tsx
touch MyComponent.module.css
```

### Add a New API Endpoint

1. Define contract in `specs/001-cost-sharing-app/contracts/api-spec.yaml`
2. Add DTO in `backend/src/CostSharing.Shared/DTOs/`
3. Add service method in `backend/src/CostSharing.Core/Services/`
4. Add controller endpoint in `backend/src/CostSharing.API/Controllers/`
5. Write tests in `backend/tests/CostSharing.UnitTests/`

### Update Data Model

1. Modify entity in `backend/src/CostSharing.Core/Models/`
2. Update documentation in `specs/001-cost-sharing-app/data-model.md`
3. Update validation rules if needed
4. Run tests to ensure no breakage

## 8. Debugging

### Backend Debugging (VS Code)

Add to `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/backend/src/CostSharing.API/bin/Debug/net8.0/CostSharing.API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/backend/src/CostSharing.API",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### Frontend Debugging (Chrome DevTools)

1. Install React Developer Tools extension
2. Open DevTools (F12)
3. Use "Components" and "Profiler" tabs for React debugging

### API Testing with Swagger

Navigate to `https://localhost:5001/swagger` to see interactive API documentation.

## 9. Troubleshooting

### "Google Drive API not enabled"

**Solution**: Enable Google Drive API in Google Cloud Console for your project.

### "CORS error in frontend"

**Solution**: Ensure `AllowedOrigins` in `appsettings.Development.json` includes `http://localhost:3000`.

### "SendGrid 401 Unauthorized"

**Solution**: Verify API key is correct and has "Mail Send" permissions.

### "Tests failing due to file access"

**Solution**: Ensure Google Drive service account has write permissions to test folder.

### "npm install fails"

**Solution**: 
```bash
# Clear cache and retry
npm cache clean --force
rm -rf node_modules package-lock.json
npm install
```

### "dotnet restore fails"

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear
dotnet restore --force
```

## 10. Development Workflow

### Daily Development

```bash
# 1. Pull latest changes
git pull origin 001-cost-sharing-app

# 2. Start backend
cd backend/src/CostSharing.API && dotnet run

# 3. Start frontend (new terminal)
cd frontend && npm start

# 4. Make changes and test

# 5. Run linters before commit
cd backend && dotnet format
cd frontend && npm run lint:fix

# 6. Run tests
cd backend && dotnet test
cd frontend && npm test

# 7. Commit and push
git add .
git commit -m "feat: description of changes"
git push origin 001-cost-sharing-app
```

### Code Review Checklist

- [ ] All tests pass
- [ ] Linting passes with zero warnings
- [ ] Code follows constitutional principles
- [ ] New features have tests (≥80% coverage for critical features)
- [ ] API contracts updated if endpoints changed
- [ ] Documentation updated if public APIs changed

## 11. Next Steps

- **Implement User Stories**: Follow `specs/001-cost-sharing-app/tasks.md` (will be generated by `/speckit.tasks`)
- **Review Architecture**: Read `specs/001-cost-sharing-app/research.md` for technical decisions
- **Understand Data Model**: Study `specs/001-cost-sharing-app/data-model.md`
- **API Reference**: See `specs/001-cost-sharing-app/contracts/api-spec.yaml`

## 12. Useful Commands

```bash
# Backend
dotnet build                    # Build solution
dotnet test --logger "console"  # Run tests with output
dotnet watch run                # Hot reload during development
dotnet ef database update       # Apply migrations (if using EF)

# Frontend
npm start                       # Start dev server
npm test -- --watch            # Run tests in watch mode
npm run build                  # Production build
npm run analyze                # Bundle size analysis

# Both
docker-compose up              # Start all services (if using Docker)
```

## 13. Resources

- **API Documentation**: https://localhost:5001/swagger
- **Design Specifications**: `specs/001-cost-sharing-app/spec.md`
- **Implementation Plan**: `specs/001-cost-sharing-app/plan.md`
- **Research Decisions**: `specs/001-cost-sharing-app/research.md`
- **.NET Docs**: https://docs.microsoft.com/dotnet/
- **React Docs**: https://react.dev/
- **Google Drive API**: https://developers.google.com/drive/api/guides/about-sdk

## 14. Getting Help

- **Technical Questions**: Open an issue on GitHub
- **Bugs**: Report via GitHub Issues with reproduction steps
- **Feature Requests**: Discuss in GitHub Discussions
- **Code Review**: Tag `@team` in pull requests

---

**Happy Coding! 🚀**

For detailed architecture decisions and technical approach, refer to the research document: `specs/001-cost-sharing-app/research.md`
