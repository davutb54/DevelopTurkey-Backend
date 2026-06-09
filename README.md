# DevelopTurkey Backend

ASP.NET Core 8 Web API — DevelopTurkey platformunun sunucu tarafı.

## Teknoloji Yığını

- **.NET 8** / ASP.NET Core Web API
- **Entity Framework Core 9** — SQL Server (LocalDB geliştirme, full instance production)
- **MassTransit 8** — In-memory kuyruk (RabbitMQ geçişe hazır)
- **SignalR** — Gerçek zamanlı bildirim ve sohbet
- **JWT Bearer + Google OAuth** — Kimlik doğrulama
- **Sqids 3** — Sayısal ID gizleme
- **Roslyn** — İzole C# script çalıştırma (ayrı process)
- **xUnit + FluentAssertions + Moq** — Test

## Proje Yapısı

```
DevelopTurkey-Backend/
├── Core/                   # Cross-cutting: JWT, sonuç tipleri, yetki arayüzleri
├── Entities/               # EF Core entity sınıfları ve DTO'lar
├── DataAccess/             # Repository soyutlamaları ve EF implementasyonları
│   └── Concrete/EntityFramework/
│       └── DevelopTurkeyContext.cs   # DbContext + global query filter'lar
├── Business/
│   ├── Concrete/Actions/   # 17 adet IWorkflowActionHandler
│   └── Concrete/Consumers/ # MassTransit consumer'ları
├── WebAPI/
│   ├── Controllers/        # Tüm API controller'ları
│   ├── Filters/            # RequireCapabilityAttribute
│   ├── Seeders/            # Capability, Feature, BootstrapAdmin seeder'ları
│   ├── HostedServices/     # CapabilitySnapshotInitializer, MediaCleanupHostedService
│   └── Program.cs          # DI kaydı, middleware, seeder sırası
├── CSharpSandbox/          # İzole Roslyn çalıştırıcısı (net8.0 Exe)
└── Tests/
    └── DevelopTurkey.Workflow.Tests/   # 136 test
```

## Kurulum

### Gereksinimler

- .NET 8 SDK
- SQL Server LocalDB (Development) veya SQL Server (Production)

### Çalıştırma

```powershell
cd DevelopTurkey-Backend/WebAPI

$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

Swagger UI: `https://localhost:7001/swagger`

### Migration

```powershell
dotnet ef migrations add <MigrationAdi> `
    --project ../DataAccess `
    --startup-project .

dotnet ef database update `
    --project ../DataAccess `
    --startup-project .
```

### Testler

```powershell
dotnet test Tests/DevelopTurkey.Workflow.Tests/
# 136/136 passed
```

## Yetkilendirme

Platform `[Authorize(Roles="...")]` yerine **capability tabanlı** yetkilendirme kullanır.

Uygulama açılışında `CapabilitySnapshotInitializer` tüm aktif capability'leri RAM'e yükler. Her istekte `ICapabilityResolver` O(1) ile kontrol yapar — veritabanına gitmez.

```csharp
// Controller seviyesinde
[RequireCapability("moderation.problem_delete")]
public async Task<IActionResult> DeleteProblem(int id) { ... }

// İş mantığı seviyesinde
await _capabilityPolicy.RequireAsync("user.problem_delete_own");
```

### Capability Kategorileri (124 kod)

| Prefix | Kapsam |
|--------|--------|
| `admin.*` | Platform ve kurum yönetimi |
| `moderation.*` | İçerik moderasyonu |
| `expert.*` | Uzman işlemleri |
| `official.*` | Resmi yanıt ve kurum temsili |
| `user.*` | Standart kullanıcı işlemleri |
| `chat.*` | Sohbet sistemi |
| `page.admin.*` | Admin panel sayfa görünürlükleri |

## Workflow Motoru

Görsel kanvasta oluşturulan kurallar durable pipeline olarak çalışır:

```
WorkflowOrchestrator
  └─ NodeRunConsumer
       └─ ActionRunConsumer
            └─ WorkflowActionDispatcher
                 └─ IWorkflowActionHandler.ExecuteAsync()
```

**17 hazır action:** `SendEmail`, `SendNotification`, `SendBulkNotification`, `BanUser`, `UnbanUser`, `WarnUser`, `ResolveProblem`, `HighlightProblem`, `DeleteProblem`, `ReportProblem`, `ApproveSolution`, `RejectSolution`, `HighlightSolution`, `DeleteSolution`, `DeleteComment`, `LogEvent`, `Webhook`

## C# Sandbox

Kullanıcı scriptleri ana process'ten izole, ayrı bir `CSharpSandbox.dll` process'inde çalışır.

```
RuleExecutionManager
  → dotnet exec sandbox/CSharpSandbox.dll
      stdin:  { "Code": "...", "Context": {...} }
      stdout: { "Success": true, "Result": "..." }
  → 30 sn timeout → process.Kill(entireProcessTree: true)
```

## Kill Switch

| Seviye | Davranış |
|--------|----------|
| `Soft` | Yeni workflow başlatma durdurulur |
| `Hard` | Tüm yeni node işlemleri durdurulur |
| `Emergency` | Tüm işlemler anında durdurulur |

Öncelik: **DB → Ortam değişkeni (`KILLSWITCH__DEFAULTSTATE`) → appsettings → Off**

## Çok Kiracılı Mimari

`TenantResolutionMiddleware` her istekte tenant'ı çözümler; `DevelopTurkeyContext`'teki global query filter'lar veri izolasyonunu sağlar. Global adminler `admin.cross_institution_read` capability'siyle tüm tenant verilerini okuyabilir.

## Ortam Değişkenleri

| Değişken | Açıklama |
|----------|----------|
| `ASPNETCORE_ENVIRONMENT` | `Development` / `Production` |
| `KILLSWITCH__DEFAULTSTATE` | `none` / `soft` / `hard` / `emergency` |
| `CSharpSandbox__DllPath` | Sandbox DLL yolu (varsayılan: `{baseDir}/sandbox/CSharpSandbox.dll`) |
| `Sqids__Alphabet` | ID encode alfabesi |
| `Sqids__MinLength` | Minimum encode uzunluğu |
| `Cors__AllowedOrigins__0` | İzin verilen CORS origin |
