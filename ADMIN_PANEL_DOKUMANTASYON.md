# SÖ7LE — Admin Panel Dokümantasyonu

> **Son güncelleme:** 2026-06-09  
> **Kapsam:** Frontend (`develop-turkey-frontend/src/pages/admin/`) + Backend (`DevelopTurkey-Backend/WebAPI/Controllers/`)  
> **Erişim adresi:** `/admin` (giriş yapmış kullanıcı + `admin.system_access` capability gerektirir)

---

## İçindekiler

1. [Genel Bakış](#1-genel-bakış)
2. [Erişim ve Yetkilendirme](#2-erişim-ve-yetkilendirme)
3. [Mimari Yapı](#3-mimari-yapı)
4. [Navigasyon ve Rota Yapısı](#4-navigasyon-ve-rota-yapısı)
5. [Genel Yönetim Sekmeleri](#5-genel-yönetim-sekmeleri)
   - 5.1 Canlı Radar (CommandCenter)
   - 5.2 Genel Bakış (AdminOverview)
6. [İçerik Moderasyonu](#6-içerik-moderasyonu)
   - 6.1 Sorunlar (ProblemsTab)
   - 6.2 Çözümler (SolutionsTab)
   - 6.3 Kategoriler (TopicsTab)
   - 6.4 Uzman Onayları (ExpertApprovalsTab)
7. [Kullanıcı Yönetimi](#7-kullanıcı-yönetimi)
   - 7.1 Kullanıcılar (UsersTab)
   - 7.2 Kurumlar (InstitutionsTab)
8. [Moderasyon Merkezi](#8-moderasyon-merkezi)
   - 8.1 Şikayet Merkezi (ReportsTab)
   - 8.2 Geri Bildirimler (FeedbacksTab)
   - 8.3 Sohbet Yönetimi (ChatManagementTab)
9. [Sistem Yönetimi](#9-sistem-yönetimi)
   - 9.1 Güvenlik İzleme (SecurityTab)
   - 9.2 Sistem Ayarları (SettingsTab)
   - 9.3 Duyurular (AnnouncementsTab)
   - 9.4 Sistem Logları (SystemLogsTab)
   - 9.5 Aksiyon Geçmişi (ActivityLogsTab)
   - 9.6 Kill Switch (KillSwitchTab)
10. [İçerik Yönetimi](#10-içerik-yönetimi)
    - 10.1 Sözleşmeler (AgreementsTab)
    - 10.2 E-posta Şablonları (EmailTemplatesTab)
    - 10.3 Hakkımızda (CorporateTab)
11. [Araçlar](#11-araçlar)
    - 11.1 Modül Yönetimi (FeatureManager)
    - 11.2 Workflow Builder (WorkflowBuilder)
12. [Yetki Yönetimi](#12-yetki-yönetimi)
    - 12.1 Yetki Yönetimi (CapabilityManagementTab)
    - 12.2 Yetki Şablonları (CapabilityTemplatesTab)
    - 12.3 Yetki Logları (CapabilityAuditTab)
13. [Metrikler Paneli](#13-metrikler-paneli)
    - 13.1 Genel Bakış (Overview)
    - 13.2 Yetki Metrikleri (CapabilityMetrics)
    - 13.3 Workflow Metrikleri (WorkflowMetrics)
    - 13.4 Kullanıcı Metrikleri (UserMetrics)
    - 13.5 Sistem Sağlığı (SystemHealth)
    - 13.6 Audit Log Tarayıcısı (AuditLogBrowser)
    - 13.7 Dead-Letter Kuyruğu (WorkflowDeadLetterTab)
14. [Backend Entegrasyonu](#14-backend-entegrasyonu)
15. [Capability Kodu Referansı](#15-capability-kodu-referansı)

---

## 1. Genel Bakış

Admin paneli, SÖ7LE platformunun merkezi yönetim arayüzüdür. Platform üzerindeki tüm kullanıcı, içerik, sistem ve iş akışı operasyonları bu panel aracılığıyla yönetilir.

**Ana özellikler:**

| Kategori | Özellikler |
|----------|-----------|
| İçerik moderasyonu | Sorun/çözüm onaylama, silme, öne çıkarma, kapatma |
| Kullanıcı yönetimi | Ban/unban, uyarı, kimliğe bürünme, kurum değiştirme, unvan atama |
| Yetki sistemi | Capability grant/revoke, şablon uygulama, audit log |
| Sistem izleme | Güvenlik olayları, kill switch, sistem logları |
| Metrikler | Kullanıcı büyüme, workflow performansı, yetki kullanımı |
| Yapılandırma | Feature flag yönetimi, e-posta şablonları, sözleşmeler |
| Workflow | Görsel workflow builder (ReactFlow), test-run, çalışma geçmişi |

---

## 2. Erişim ve Yetkilendirme

### 2.1 Genel Erişim Kuralı

Admin paneline `/admin` URL'inden ulaşılır. `App.tsx` içinde bu yolun `AdminLayout` ile korunduğu görülür. Kullanıcı oturum açmamışsa login sayfasına yönlendirilir.

Sidebar'daki her menü öğesi ayrıca bir **`page.*` capability** ile de korunur. Dolayısıyla `admin.system_access` olan bir kullanıcı yalnızca kendi `page.admin.*` capability'lerine sahip olduğu sayfaları görebilir.

### 2.2 Capability Tabanlı Erişim Modeli

```
admin.system_access          → Admin paneline giriş izni (minimum şart)
page.admin.<sekme>           → Belirli sekmeyi sidebar'da görme + açma izni
admin.<işlem>                → Belirli bir işlemi gerçekleştirme izni (ör. admin.user_ban)
moderation.<işlem>           → Moderasyon işlemleri (ör. moderation.problem_delete)
```

**Örnek akış:**
1. Kullanıcı `/admin/users` sayfasını açmaya çalışır
2. `AdminLayout` sidebar'ı render ederken `page.admin.users` capability'sini kontrol eder; yoksa menü öğesi görünmez
3. Route render olsa bile `UsersTab` içindeki her buton (`admin.user_ban` vb.) ayrıca `useCapability` hook'u ile kontrol edilir

### 2.3 Frontend Capability Kontrol Araçları

```tsx
// Hook kullanımı
const canBan = useCapability('admin.user_ban');
{canBan && <button>Banla</button>}

// Bileşen kullanımı
<Can capability="admin.user_ban">
  <BanButton />
</Can>

// Birden fazla — herhangi biri yeterli
<Can capability={['admin.system_access', 'moderation.content_review']}>
  <ReportsButton />
</Can>
```

### 2.4 Backend Capability Kontrolü

Controller metodları `[RequireCapability("...")]` niteliği ile korunur:

```csharp
[HttpPost("banuser")]
[RequireCapability("admin.user_ban")]
public async Task<IActionResult> BanUser([FromBody] BanUserDto dto) { ... }
```

Yetkisiz erişim denemesi → `403 Capability Denied` + `SecurityEvent` kaydı.

---

## 3. Mimari Yapı

```
develop-turkey-frontend/src/pages/admin/
├── AdminLayout.tsx              ← Ana yerleşim: sidebar + <Outlet />
├── FeatureManager.tsx           ← Feature flag yönetimi
├── InstitutionFeaturePanel.tsx  ← Kurum bazlı feature override
├── WorkflowBuilder.tsx          ← Görsel workflow editörü
│
├── dashboard/                   ← Metrikler bölümü (DashboardLayout altında)
│   ├── DashboardLayout.tsx
│   ├── Overview.tsx
│   ├── CapabilityMetrics.tsx
│   ├── WorkflowMetrics.tsx
│   ├── UserMetrics.tsx
│   ├── SystemHealth.tsx
│   └── AuditLogBrowser.tsx
│
└── tabs/                        ← Yönetim sekmeleri (AdminLayout altında)
    ├── AdminOverview.tsx
    ├── CapabilityManagementTab.tsx
    ├── CapabilityTemplatesTab.tsx
    ├── CapabilityAuditTab.tsx
    ├── UsersTab.tsx
    ├── ProblemsTab.tsx
    ├── SolutionsTab.tsx
    ├── TopicsTab.tsx
    ├── InstitutionsTab.tsx
    ├── ExpertApprovalsTab.tsx
    ├── ReportsTab.tsx
    ├── FeedbacksTab.tsx
    ├── SystemLogsTab.tsx
    ├── ActivityLogsTab.tsx
    ├── SecurityTab.tsx
    ├── SettingsTab.tsx
    ├── AgreementsTab.tsx
    ├── CorporateTab.tsx
    ├── EmailTemplatesTab.tsx
    ├── AnnouncementsTab.tsx
    ├── KillSwitchTab.tsx
    ├── ChatManagementTab.tsx
    ├── WorkflowDeadLetterTab.tsx
    └── CommandCenter.tsx
```

---

## 4. Navigasyon ve Rota Yapısı

### 4.1 Sidebar Grupları

`AdminLayout.tsx` içinde tanımlı 9 menü grubu:

| Grup | Menü Öğeleri | Capability |
|------|-------------|-----------|
| **Genel** | Canlı Radar, Genel Bakış | `page.admin.monitor`, `page.admin.overview` |
| **İçerik** | Sorunlar, Çözümler, Kategoriler, Uzman Onayları | `page.admin.problems`, `page.admin.solutions`, `page.admin.topics`, `page.admin.expert_approvals` |
| **Kullanıcılar** | Kullanıcılar, Kurumlar | `page.admin.users`, `page.admin.institutions` |
| **Moderasyon** | Şikayet Merkezi ⁽¹⁾, Gelen Kutusu ⁽¹⁾, Sohbet | `page.admin.reports`, `page.admin.feedbacks`, `page.admin.chat` |
| **Sistem** | Güvenlik İzleme, Sistem Ayarları, Duyurular, Sistem Logları, Aksiyon Geçmişi, Kill Switch | `page.admin.security` vb. |
| **İçerik Yönetimi** | Sözleşmeler, E-posta Şablonları, Hakkımızda | `page.admin.agreements` vb. |
| **Araçlar** | Modül Yönetimi, Workflow Builder | `page.admin.features`, `page.admin.workflow` |
| **Yetkiler** | Yetki Yönetimi, Yetki Şablonları, Yetki Logları | `page.admin.capabilities` vb. |
| **Metrikler** | Dashboard (nested: Genel, Yetki, Workflow, Kullanıcı, Sistem, Audit, Dead-Letter) | `page.admin.metrics` |

> ⁽¹⁾ Bekleyen işlem sayısı badge olarak gösterilir. Sayılar `reportService.getPending()` ve `feedbackService.getAllPaged()` ile yüklenir.

### 4.2 Tam Rota Tablosu

```
/admin                              → /admin/overview yönlendirme
/admin/overview                     → AdminOverview
/admin/command-center               → CommandCenter
/admin/users                        → UsersTab
/admin/topics                       → TopicsTab
/admin/institutions                 → InstitutionsTab
/admin/problems                     → ProblemsTab
/admin/solutions                    → SolutionsTab
/admin/expert-approvals             → ExpertApprovalsTab
/admin/reports                      → ReportsTab
/admin/feedbacks                    → FeedbacksTab
/admin/logs                         → SystemLogsTab
/admin/activity-logs                → ActivityLogsTab
/admin/settings                     → SettingsTab
/admin/agreements                   → AgreementsTab
/admin/corporate                    → CorporateTab
/admin/email-templates              → EmailTemplatesTab
/admin/workflow                     → WorkflowBuilder
/admin/features                     → FeatureManager
/admin/capabilities                 → CapabilityManagementTab
/admin/capability-templates         → CapabilityTemplatesTab
/admin/capability-audit             → CapabilityAuditTab
/admin/announcements                → AnnouncementsTab
/admin/kill-switch                  → KillSwitchTab
/admin/security                     → SecurityTab
/admin/chat                         → ChatManagementTab

/admin/metrics                      → /admin/metrics/overview yönlendirme
/admin/metrics/overview             → Overview
/admin/metrics/capabilities         → CapabilityMetrics
/admin/metrics/workflow             → WorkflowMetrics
/admin/metrics/users                → UserMetrics
/admin/metrics/system               → SystemHealth
/admin/metrics/audit-log            → AuditLogBrowser
/admin/metrics/dead-letters         → WorkflowDeadLetterTab
```

### 4.3 Mobil Uyumluluk

`AdminLayout.tsx` masaüstü ve mobil için iki farklı mod sunar:

- **Masaüstü (≥768px):** Sol sidebar yapışık (`sticky top-0`), tam genişlikte navigasyon
- **Mobil (<768px):** Hamburger menü butonu (`☰`), tıklandığında kenar çekmeciyi (`fixed drawer`) açar; backdrop overlay ile navigasyon tıklamasında otomatik kapanır

---

## 5. Genel Yönetim Sekmeleri

### 5.1 Canlı Radar — `CommandCenter.tsx`

**Rota:** `/admin/command-center`  
**Gerekli capability:** `page.admin.monitor`

Gerçek zamanlı sistem izleme kontrol merkezi. Canlı metrikler ve anlık sistem durumu hakkında yöneticilere özet bilgi verir.

**Gösterilenler:**
- Sistem kaynak metrikleri (CPU, RAM, Disk)
- Servis erişilebilirlik durumu
- Veritabanı bağlantı havuzu istatistikleri
- Anlık uyarılar

**API Çağrısı:** `adminService.getSystemHealthStatus()`

---

### 5.2 Genel Bakış — `AdminOverview.tsx`

**Rota:** `/admin/overview`  
**Gerekli capability:** `admin.dashboard_view`

Yönetici gösterge panosu. Platformun genel durumunu tek ekranda özetler.

#### İstatistik Kartları (6 adet)

| Kart | Açıklama |
|------|----------|
| Toplam Kullanıcı | Kayıtlı tüm kullanıcı sayısı |
| Toplam Sorun | Platforma girilen sorun sayısı |
| Toplam Çözüm | Üretilen çözüm sayısı |
| Bekleyen Şikayet | İncelenmemiş şikayet sayısı |
| Banlı Kullanıcı | Aktif ban ceza sayısı |
| Aktif Workflow | Tanımlı ve aktif iş akışı sayısı |

#### Sağlık Göstergeleri (3 adet)

| Gösterge | Veri Kaynağı |
|----------|-------------|
| Sistem Uptime | `metricsService.getOverview()` |
| WF Başarı Oranı (24s) | `metricsService.getWorkflow()` |
| Capability Snapshot Durumu | `metricsService.getOverview()` |

#### Bekleyen İşlemler Paneli

- Çözüm bekleyen şikayetler
- Uzman onayı bekleyen çözümler (liste + önizleme)
- Okunmamış geri bildirimler
- Kurum sayısı

#### Grafikler

| Grafik | Tür | Veri |
|--------|-----|------|
| Kategori dağılımı | Bar chart | Problem/konu sayısı |
| Kuruma göre sorunlar | Pie chart | Kurum bazında dağılım |
| Son 30 günlük kayıtlar | Line chart | Günlük yeni kullanıcı |

**API Çağrıları:**
```
adminService.getDashboardStats()       → AdminDashboardDto
adminService.getDashboardAnalytics()   → DashboardAnalyticsDto
metricsService.getOverview()           → OverviewMetrics
adminService.getPendingExpertSolutions()
reportService.getPending()
feedbackService.getAllPaged({page:1, pageSize:1})
```

---

## 6. İçerik Moderasyonu

### 6.1 Sorunlar — `ProblemsTab.tsx`

**Rota:** `/admin/problems`  
**Gerekli yetkiler:** `page.admin.problems`

Tüm sorunların listelendiği ve yönetildiği sekme.

#### Filtreler

| Filtre | Değerler |
|--------|----------|
| Metin arama | Başlık ve açıklamada arama |
| Durum | Öne Çıkan, Çözüldü |
| Kurum | Kurum dropdown |
| Konu | Kategori dropdown |

#### Tablo Sütunları

- ID, Başlık, Yazar, Kurum, Kategori
- Durum badge'leri: `isHighlighted`, `isResolved`, `isResolvedByExpert`
- Oluşturma tarihi
- İşlemler dropdown menüsü

#### İşlemler ve Gerekli Capability'ler

| İşlem | Capability | Açıklama |
|-------|-----------|----------|
| Çözüldü işaretle/kaldır | `moderation.problem_resolve` | Toggle |
| Öne çıkar/kaldır | `moderation.problem_highlight` | Toggle |
| Sil | `moderation.problem_delete` | Kalıcı silme |
| Kapat (gerekçeyle) | `moderation.problem_close` | Nedeniyle kapama |
| Yeniden aç | `moderation.problem_reopen` | Kapatılmış sorunu aktif et |
| Gizle | `moderation.problem_hide` | Görünürlük kaldır |

**Sayfalama:** Sayfa başı 8 kayıt

---

### 6.2 Çözümler — `SolutionsTab.tsx`

**Rota:** `/admin/solutions`  
**Gerekli yetkiler:** `page.admin.solutions`

Platform genelindeki çözümlerin moderasyonu.

#### İşlemler ve Capability'ler

| İşlem | Capability |
|-------|-----------|
| Öne çıkar | `moderation.solution_highlight` |
| Uzman çözümü onayla | `expert.solution_approve` |
| Uzman çözümü reddet | `expert.solution_reject` |
| Sil | `moderation.solution_delete` |

---

### 6.3 Kategoriler — `TopicsTab.tsx`

**Rota:** `/admin/topics`  
**Gerekli yetki:** `page.admin.topics`

Sorun kategorilerini (topic) yönetme arayüzü.

**İşlemler:** Yeni kategori oluşturma, güncelleme, silme, sıra değiştirme, ikon/resim yükleme.

---

### 6.4 Uzman Onayları — `ExpertApprovalsTab.tsx`

**Rota:** `/admin/expert-approvals`  
**Gerekli yetki:** `page.admin.expert_approvals`

Uzmanların işaretlediği çözümler inceleme kuyruğunda bekler. Bu sekme o kuyruğu yönetir.

**İş akışı:**
1. Uzman bir çözümü `ExpertApprovalStatus = Pending` olarak işaretler
2. Bu sekmede bekleyen çözümler listelenir
3. Admin Onayla (`expert.solution_approve`) veya Reddet (`expert.solution_reject`) seçer
4. Onaylanan çözüm ana listede uzman rozeti ile gösterilir

**API:** `adminService.getPendingExpertSolutions()`

---

## 7. Kullanıcı Yönetimi

### 7.1 Kullanıcılar — `UsersTab.tsx`

**Rota:** `/admin/users`  
**Gerekli yetki:** `page.admin.users`

Platformun en kapsamlı yönetim sekmelerinden biri. Kullanıcı arama, filtreleme ve birden fazla işlemi tek ekranda yönetme olanağı sunar.

#### Arama ve Filtreleme

| Filtre | Açıklama |
|--------|----------|
| Metin arama | Ad, soyad, e-posta ile arama |
| Durum filtresi | Banlı, E-posta doğrulanmamış, Şikayet edilmiş |
| Kurum filtresi | Kurum dropdown seçimi |
| Tarih filtresi | Son 7 gün, Son 30 gün |
| Sıralama | Kayıt tarihi, Ad, ID |

**Sayfalama:** Sayfa başı 10 kullanıcı  
**API:** `userService.getAllPaged({searchText, roleFilter, emailStatus, institutionId, isReported, registeredAfter, sortBy})`

#### Kullanıcı Tablo Sütunları

- ID, Ad, E-posta, Kurum
- E-posta doğrulama durumu badge'i
- Ban durumu badge'i
- Şikayet edilmiş işareti
- Kayıt tarihi
- İşlemler dropdown menüsü

#### Kullanıcı İşlemleri

| İşlem | Capability | Açıklama |
|-------|-----------|----------|
| Banla | `admin.user_ban` | Kullanıcıyı platforma erişimden engeller |
| Banı kaldır | `admin.user_unban` | Erişimi yeniden aktif eder |
| Uyarı ver | `moderation.user_warn` | Uyarı başlığı + mesaj + önem derecesi |
| Uyarı geçmişi | `admin.user_warning_read_all` | Tüm uyarıları listele + revoke |
| Kimliğe bürün | `admin.user_impersonate` | Admin şifresiyle doğrulama → sudo mod |
| Kurum değiştir | `admin.user_institution_change` | Dropdown ile yeni kurum seç |
| Unvan yönet | `admin.user_title_assign` | Unvan ata, listele, kaldır |
| Yetki paketi | `admin.capability_template_apply` | Şablon uygula veya kaldır |

#### Kimliğe Bürünme (Impersonation) Akışı

1. Admin "Kimliğe Bürün" butonuna tıklar
2. Admin şifresi giriş diyaloğu açılır
3. `adminService.impersonateUser({targetUserId, adminPassword})` çağrısı
4. Başarılıysa hedef kullanıcının session'ıyla oturum açılır
5. Oturum sonlandırılarak normale dönülür

#### Uyarı Dereceleri

| Derece | Kullanım |
|--------|----------|
| `low` | Küçük kural ihlali |
| `medium` | Tekrarlanan kural ihlali |
| `high` | Ciddi ihlal |
| `critical` | Ban öncesi son uyarı |

#### Unvan Yönetimi

| Alan | Değerler |
|------|----------|
| Label | Metin (ör. "Güvenilir Uzman") |
| Kind | `official` \| `expert` \| `custom` |
| Color | Hex renk kodu |
| Icon | Emoji veya ikon kodu |
| IsVisible | Profilden görünürlük toggle |

#### Yetki Paketi (Template) Uygulaması

`UsersTab` içindeki "Yetki Paketi" butonu, önceden tanımlanmış capability şablonlarını tek tıklamayla kullanıcıya uygular veya kaldırır.

---

### 7.2 Kurumlar — `InstitutionsTab.tsx`

**Rota:** `/admin/institutions`  
**Gerekli yetki:** `page.admin.institutions`

Platform genelindeki kurumların (tenant) listesi ve yönetimi.

**İşlemler:**
- Kurum oluşturma (ad, domain, subdomain, logo, renk)
- Kurum güncelleme
- Kullanıcıları listeleme

**Subdomain Notu:** Her kurumun bir subdomain'i olabilir. `FeatureContext` bu subdomain üzerinden kuruma ait feature ayarlarını yükler.

---

## 8. Moderasyon Merkezi

### 8.1 Şikayet Merkezi — `ReportsTab.tsx`

**Rota:** `/admin/reports`  
**Gerekli yetki:** `page.admin.reports`

Platform kullanıcılarının yaptığı şikayetlerin incelenip sonuçlandırıldığı merkez.

#### Sekme Yapısı

| Sekme | Kapsam |
|-------|--------|
| Sorun Şikayetleri | Raporlanan sorun içerikleri |
| Çözüm Şikayetleri | Raporlanan çözüm içerikleri |
| Kullanıcı Şikayetleri | Raporlanan kullanıcılar |

#### Tablo Sütunları

- Hedef içerik (ID + başlık önizleme)
- Şikayet nedeni
- Şikayet eden kullanıcı
- Şikayet tarihi
- Seçim checkbox'ı (toplu işlem için)

#### İşlemler

| İşlem | Capability | Açıklama |
|-------|-----------|----------|
| Şikayeti çöz | `moderation.report_resolve` | Şikayeti kapalı işaretle, içerik kalır |
| İçeriği sil + çöz | `moderation.problem_delete` / `moderation.solution_delete` | İçeriği sil ve şikayeti kapat |
| Kullanıcıyı banla | `admin.user_ban` | Kullanıcı şikayetlerinde |

**Toplu işlem:** Checkbox seçimi + "Seçilenleri Çöz" butonu

**API Çağrıları:**
```
reportService.getPending()            → Tüm bekleyen şikayetler
reportService.resolve(reportId)       → Şikayeti kapat
adminService.deleteProblem(id)        → İçeriği sil
solutionService.delete(id)            → Çözümü sil
```

---

### 8.2 Geri Bildirimler — `FeedbacksTab.tsx`

**Rota:** `/admin/feedbacks`  
**Gerekli yetki:** `page.admin.feedbacks`

Kullanıcıların iletişim/geri bildirim formu üzerinden gönderdiği mesajların gelen kutusu.

**Gösterilenler:** Gönderen, konu, mesaj, tarih  
**İşlemler:** Okundu işaretleme, yanıtlama, silme

---

### 8.3 Sohbet Yönetimi — `ChatManagementTab.tsx`

**Rota:** `/admin/chat`  
**Gerekli yetki:** `page.admin.chat`

Platform içi sohbet sisteminin yönetim arayüzü.

**Özellikler:**
- `Communication.EnableChat` veya `Communication.EnableSupportChat` feature flag'i kontrolü
- Aktif feature badge'leri
- Grup konuşma oluşturma formu
- `ChatPanel` bileşeni ile sohbet moderasyonu
- Konuşma silme (`chat.institution_manage`)
- Mesaj silme (admin yetkilileri için)

**Feature Flag Durumu:**
- Her iki flag da kapalıysa: "Sohbet sistemi devre dışı" mesajı gösterilir
- Her flag için ayrı bilgi notu

---

## 9. Sistem Yönetimi

### 9.1 Güvenlik İzleme — `SecurityTab.tsx`

**Rota:** `/admin/security`  
**Gerekli capability:** `admin.security_monitor`

Platform güvenlik olaylarının gerçek zamanlı izleme ekranı.

#### Olay Türleri

| Kod | Açıklama | Kayıt Noktası |
|-----|----------|---------------|
| `failed_login` | Başarısız giriş denemesi | `AuthController` |
| `rate_limited` | Rate limit aşımı | `Program.cs` OnRejected |
| `capability_denied` | Yetkisiz erişim girişimi | `RequireCapabilityAttribute` |
| `enumeration_detected` | API tarama saldırısı | `EnumerationDetectionMiddleware` (30+ 404/5dk) |

#### Önem Seviyeleri

| Seviye | Badge Rengi | Kullanım |
|--------|------------|----------|
| `low` | Gri | Bilgi amaçlı |
| `medium` | Sarı | İzleme gerektiren |
| `high` | Turuncu | Hızlı inceleme gerekli |
| `critical` | Kırmızı | Acil müdahale |

#### Filtreler

- IP adresi
- Olay türü dropdown
- Önem seviyesi dropdown
- Başlangıç/bitiş tarihi

**Sayfalama:** Sayfa başı 50 kayıt  
**API:** `securityService.getEvents({ipAddress, eventType, severity, startDate, endDate, page, pageSize})`

---

### 9.2 Sistem Ayarları — `SettingsTab.tsx`

**Rota:** `/admin/settings`  
**Gerekli yetki:** `page.admin.settings`

Platform geneli sistem yapılandırma ayarları. Bakım modu, kayıt açık/kapalı, e-posta ayarları vb.

---

### 9.3 Duyurular — `AnnouncementsTab.tsx`

**Rota:** `/admin/announcements`  
**Gerekli yetki:** `page.admin.announcements`

Tüm platforma veya belirli kurumlara toplu bildirim/duyuru gönderimi.

**Özellikler:**
- Hedef kurum seçimi (dropdown)
- Başlık + mesaj formu
- Anlık push notification veya kalıcı duyuru seçeneği

---

### 9.4 Sistem Logları — `SystemLogsTab.tsx`

**Rota:** `/admin/logs`  
**Gerekli yetki:** `page.admin.logs`

Backend servislerinin ürettiği sistem olayı logları. `WorkflowAction`, `CSharpNode`, `Notification` gibi kategorilere göre filtrelenebilir.

**Filtreler:** Kategori, tarih aralığı, metin arama  
**API:** `adminService.getLogs(filter)`

---

### 9.5 Aksiyon Geçmişi — `ActivityLogsTab.tsx`

**Rota:** `/admin/activity-logs`  
**Gerekli yetki:** `page.admin.activity_logs`

Kullanıcı bazlı aksiyon geçmişi. Kim, ne zaman, ne yaptı bilgisini sunar.

**Filtreler:** Kullanıcı ID, aksiyon türü, tarih aralığı

---

### 9.6 Kill Switch — `KillSwitchTab.tsx`

**Rota:** `/admin/kill-switch`  
**Gerekli capability:** `admin.kill_switch`

Acil durum sistem kontrol paneli. İş akışlarını ve C# script çalıştırmayı durdurur.

#### Kill Switch Modları

| Mod | Etki | Aktivasyon |
|-----|------|-----------|
| **Soft** | Yeni workflow başlatımları durdurulur; aktif çalışanlar tamamlanır | `POST /api/admin/killswitch/soft` |
| **Hard** | Aktif workflow'lar da durdurulur; C# çalıştırma engellenir | `POST /api/admin/killswitch/hard` |
| **Emergency** | Tüm pipeline işlemleri anında durdurulur | `POST /api/admin/killswitch/emergency` |
| **Deactivate** | Kill switch kaldırılır, normal işleyişe dönülür | `POST /api/admin/killswitch/deactivate` |

#### Öncelik Zinciri

`KillSwitchManager` şu öncelik sırasına göre etkin modu belirler:

```
1. Veritabanı (en yüksek öncelik)
2. Ortam değişkeni: KILLSWITCH__DEFAULTSTATE
3. appsettings.json → KillSwitch:DefaultState
4. Off (varsayılan)
```

Geçerli değerler: `"soft"` | `"hard"` | `"emergency"` | `"none"`

**Tüm modlar `[RequireCapability("admin.kill_switch")]` ile korunur.**

---

## 10. İçerik Yönetimi

### 10.1 Sözleşmeler — `AgreementsTab.tsx`

**Rota:** `/admin/agreements`  
**Gerekli yetki:** `page.admin.agreements`

Kullanım koşulları, KVKK aydınlatma metni gibi yasal belgelerin yönetimi.

**İşlemler:** Yeni sözleşme oluşturma, mevcut güncelleme, yayınlama, kullanıcılara onay zorunluluğu gönderme

---

### 10.2 E-posta Şablonları — `EmailTemplatesTab.tsx`

**Rota:** `/admin/email-templates`  
**Gerekli yetki:** `page.admin.email_templates`

Platform tarafından gönderilen otomatik e-postaların şablon editörü.

**Şablon türleri:** Hoşgeldin, E-posta doğrulama, Şifre sıfırlama, Bildirim, Uyarı bildirimi

**Editör:** HTML + placeholder değişkenleri (ör. `{{userName}}`, `{{platformName}}`)

---

### 10.3 Hakkımızda — `CorporateTab.tsx`

**Rota:** `/admin/corporate`  
**Gerekli yetki:** `page.admin.corporate`

"Hakkımızda" sayfasının içerik bölümlerini (misyon, vizyon, ekip vb.) yönetme arayüzü.

---

## 11. Araçlar

### 11.1 Modül Yönetimi — `FeatureManager.tsx`

**Rota:** `/admin/features`  
**Gerekli capability:** `admin.feature_group_manage`

Platform ve kurum bazlı feature flag'lerin merkezi yönetim ekranı.

#### Feature Scope

| Scope | Açıklama |
|-------|----------|
| `Institution` | Kurum bazında açılıp kapanabilir |
| `Global` | Tüm platform için geçerli, kurum override edemez |

#### Arayüz Bölümleri

**Sol üst — Feature Grupları:**
- Grup adı + sıra numarası
- Yeni grup ekleme formu
- Güncelleme / silme butonları

**Sağ üst — Feature Tanımları:**
- Anahtar (key), görünen ad, grup, giriş tipi, scope, varsayılan değer
- Global scope badge'i (mor) ve kurum scope badge'i (mavi)
- Düzenleme/silme butonları

**Alt sol — Kurum Ayarları (Sekme):**
- Kurum dropdown seçimi
- `InstitutionFeaturePanel` bileşeni
- Global feature'lar listeden çıkarılmış + mor bilgi notu
- Boolean: toggle, Text/Number: input, Color: renk seçici, Select: dropdown
- Bekleyen değişiklikler amber kenarlıkla vurgulanır → "Kaydet" butonu

**Alt sağ — Platform Ayarları (Sekme):**
- Tüm feature'ların varsayılan platform değerleri
- Toplu kaydetme desteği

#### Desteklenen Giriş Tipleri

| Tip | UI Bileşeni |
|-----|------------|
| `Boolean` | Toggle switch |
| `Text` | Text input |
| `Number` | Number input |
| `Color` | Color picker |
| `Select` | Dropdown (OptionsJson'dan seçenekler) |

---

### 11.2 Workflow Builder — `WorkflowBuilder.tsx`

**Rota:** `/admin/workflow`  
**Gerekli yetki:** `page.admin.workflow`

Görsel sürükle-bırak iş akışı editörü. ReactFlow tabanlı canvas üzerinde trigger, koşul ve aksiyon node'larını birbirine bağlayarak otomatik iş akışları oluşturulur.

#### Canvas Görünümleri

| Görünüm | İçerik |
|---------|--------|
| `canvas` | ReactFlow editör (varsayılan) |
| `runs` | Geçmiş çalışmalar listesi ve detay paneli |
| `logs` | İş akışı logları (capability: `workflow.view_logs`) |

#### Toolbar İşlemleri

| Buton | Capability | Açıklama |
|-------|-----------|----------|
| Kaydet | `admin.rule_create` | Workflow'u kaydet |
| Test-Run | `admin.rule_test_run` | Sandbox test çalıştırması |
| Loglar | `workflow.view_logs` | Log görünümüne geç |
| Çalışmalar | (kayıtlı kural gerekli) | Run geçmişini aç |
| ↩ Geri al | — | Ctrl+Z / Ctrl+Y |
| ↪ İleri al | — | 50 history entry cap |

#### Klavye Kısayolları

| Kısayol | İşlev |
|---------|-------|
| `Ctrl+Z` | Geri al (Undo) |
| `Ctrl+Y` / `Ctrl+Shift+Z` | İleri al (Redo) |
| `Ctrl+F` | Arama kutusuna odaklan |
| `Esc` | Aramayı temizle |

#### Node Türleri

| Tür | Açıklama |
|-----|----------|
| `triggerNode` | İş akışını başlatan olay (sorun oluşturma, oy verme vb.) |
| `conditionNode` | Koşullu dallanma (if/else) |
| `actionNode` | Gerçekleştirilecek aksiyon (ban, bildirim, webhook vb.) |
| `csharpNode` | Özel C# mantık bloğu (capability: `expert.csharp_execute`) |

#### Node Inspector Paneli

Bir node'a çift tıklandığında sağ taraftan açılan slide-over panel:

- Node etiketi düzenleme
- Vurgu rengi (color picker)
- `triggerNode`: Olay seçici
- `actionNode`: Tam schema-aware form (select/boolean/number/text widget'ları)
- "Kaydet" ile canvas'a anlık işleme

#### Arama Özelliği (Ctrl+F)

Node type, label, trigger ve action değerlerinde arama yapar:
- Eşleşen node'lar `selected:true` ile vurgulanır
- `fitView` ile eşleşen node'a otomatik zoom
- `N/M` sayacı ve ▲▼ navigasyon butonları

#### Test-Run Akışı

1. Workflow kaydedilmiş olmalı
2. `🧪 Test-Run` butonuna tıkla
3. `POST /api/dynamicrule/{id}/test-run` çağrısı
4. Backend `isDryRun: true` ile çalıştırır (gerçek etki olmaz)
5. Sonuç banner'ı canvas üzerinde gösterilir

#### Workflow Çalışmalar (Runs) Sekmesi

- Son çalışmaların özet listesi
- Çalışma seçimi → `WorkflowRunDetailPanel` açılır
- Detay: Node çalışmaları, aksiyon sonuçları, durum (success/failed/partial)

---

## 12. Yetki Yönetimi

### 12.1 Yetki Yönetimi — `CapabilityManagementTab.tsx`

**Rota:** `/admin/capabilities`  
**Gerekli yetki:** `page.admin.capabilities`

Bireysel kullanıcılara capability grant/revoke işlemlerinin yapıldığı merkez.

#### Kullanıcı Arama

`userService.getAllPaged({searchText, pageSize})` ile otomatik tamamlamalı kullanıcı arama.

#### Capability Kataloğu

- `capabilityService.getAll()` ile tam katalog yüklenir
- GroupKey'e göre gruplandırma
- Metin filtresi
- Kapsam filtresi: `Page` / `Action`

#### Grant İşlemi

| Alan | Açıklama |
|------|----------|
| Capability kodu | Dropdown'dan seçim |
| Kurum ID | Scoped grant için (opsiyonel) |
| Son kullanım tarihi | Süreli yetki (opsiyonel) |
| Neden | Audit kaydı için açıklama |

**Capability:** `admin.capability_grant`

#### Revoke İşlemleri

| İşlem | Açıklama |
|-------|----------|
| Tekil revoke | Listeden tek capability kaldır |
| Toplu revoke | Checkbox seçimi + neden dialog'u → `revokeBulk` |

**Capability:** `admin.capability_revoke`

#### Toplu Kaldırma Diyaloğu

1. İstenen capability'ler checkbox ile işaretlenir
2. "Toplu Kaldır" butonuna tıklanır
3. Neden giriş alanı açılır (zorunlu)
4. `capabilityService.revokeBulk(userId, {capabilityCodes, reason})` çağrısı
5. Başarı sonrası liste yenilenir

---

### 12.2 Yetki Şablonları — `CapabilityTemplatesTab.tsx`

**Rota:** `/admin/capability-templates`  
**Gerekli yetki:** `page.admin.capability_templates`

Önceden tanımlanmış capability paketlerinin (template) yönetildiği sekme.

**Şablon Türleri:**

| Tür | Açıklama |
|-----|----------|
| Rol şablonları | `moderator`, `expert`, `official` gibi roller |
| Paket şablonları | Belirli senaryolar için hazır paketler |

**İşlemler:**

| İşlem | Açıklama |
|-------|----------|
| Şablonu görüntüle | İçerdiği capability listesi |
| Kullanıcıya uygula | Kullanıcı arama modal'ı → `applyTemplate` |
| Şablonu kaldır | Uygulama geri alınacak kullanıcı seçimi → `revokeApplied` |

**API Çağrıları:**
```
capabilityService.getTemplates()
capabilityService.applyTemplate(userId, templateId, reason)
capabilityService.revokeApplied(userId, templateId, reason)
```

---

### 12.3 Yetki Logları — `CapabilityAuditTab.tsx`

**Rota:** `/admin/capability-audit`  
**Gerekli capability:** `admin.capability_audit_read`

Tüm grant/revoke işlemlerinin kronolojik audit kaydı.

**Filtreler:** Aktör kullanıcı, hedef kullanıcı, işlem türü, capability kodu, tarih aralığı  
**API:** `metricsService.getAuditLog(filter)` → sayfalı liste  
**Detay:** Her kayıt tıklanabilir → JSON detay modalı

---

## 13. Metrikler Paneli

`/admin/metrics` altında `DashboardLayout` tarafından yönetilen iç navigasyonlu metrik sayfaları.

**Erişim için:** `page.admin.metrics`

---

### 13.1 Genel Bakış — `Overview.tsx`

**Rota:** `/admin/metrics/overview`  
**API:** `metricsService.getOverview()`

| Kart | Capability |
|------|-----------|
| Toplam/Yeni Kullanıcı, Banlı | `admin.metrics_user_view` |
| Sorun/Çözüm/Yorum sayıları | `admin.dashboard_view` |
| Aktif Workflow, WF Run (24s), Başarı Oranı | `admin.metrics_workflow_view` |
| Capability snapshot girişleri, yaş | `admin.metrics_capability_view` |
| RAM, Uptime | `admin.metrics_system_health_view` |

---

### 13.2 Yetki Metrikleri — `CapabilityMetrics.tsx`

**Rota:** `/admin/metrics/capabilities`  
**API:** `metricsService.getCapabilities(from?, to?)`  
**Capability:** `admin.metrics_capability_view`

**Gösterilenler:**
- Snapshot giriş sayısı, benzersiz kullanıcı, kullanıcı başı ortalama capability
- Grant/Revoke trendi (line chart)
- En çok grant edilen 10 capability (bar chart)
- Capability sayısı en yüksek 10 kullanıcı (tablo)
- Capability kategori dağılımı (pie chart)

---

### 13.3 Workflow Metrikleri — `WorkflowMetrics.tsx`

**Rota:** `/admin/metrics/workflow`  
**API:** `metricsService.getWorkflow(from?, to?)`  
**Capability:** `admin.metrics_workflow_view`

**Gösterilenler:**
- 30 günlük run sayısı trend (line chart)
- Başarılı/Başarısız/Kısmi sayılar
- Başarı oranı %, ortalama süre
- En çok tetikleyen trigger türleri
- Son başarısız çalışmalar + hata mesajları

---

### 13.4 Kullanıcı Metrikleri — `UserMetrics.tsx`

**Rota:** `/admin/metrics/users`  
**API:** `metricsService.getUsers(from?, to?)`  
**Capability:** `admin.metrics_user_view`

**Gösterilenler:**
- Toplam, banlı, uyarılı, e-posta doğrulanmamış sayıları
- Günlük yeni kayıt trendi, 30 gün (line chart)
- Kuruma göre kullanıcı dağılımı (bar chart)
- En aktif 10 kullanıcı (sorun + çözüm + yorum toplamı)

---

### 13.5 Sistem Sağlığı — `SystemHealth.tsx`

**Rota:** `/admin/metrics/system`  
**API:** `adminService.getSystemHealthStatus()`  
**Capability:** `admin.metrics_system_health_view`

**Gösterilenler:**
- CPU, RAM, Disk kullanımı
- Servis durumları (DB, MassTransit, SignalR)
- Veritabanı bağlantı havuzu istatistikleri

---

### 13.6 Audit Log Tarayıcısı — `AuditLogBrowser.tsx`

**Rota:** `/admin/metrics/audit-log`  
**API:** `metricsService.getAuditLog(filter)`  
**Capability:** `admin.audit_read`

**Filtreler:** Aktör, hedef kullanıcı, işlem türü, capability kodu, tarih aralığı  
**Detay:** Her kayıt tıklandığında JSON detay modalı açılır

---

### 13.7 Dead-Letter Kuyruğu — `WorkflowDeadLetterTab.tsx`

**Rota:** `/admin/metrics/dead-letters`  
**API:** `metricsService.getDeadLetters(page, pageSize)`  
**Capability:** `admin.metrics_workflow_view`

MassTransit tarafından işlenemeyen (başarısız) workflow mesajlarının listesi.

**İşlemler:**
- Mesaj detayını görüntüleme (JSON)
- Yeniden kuyruğa alma (`POST /metrics/workflow/dead-letters/{id}/requeue`)

---

## 14. Backend Entegrasyonu

### 14.1 Kullanılan Controller'lar

| Controller | Prefix | Sorumluluk |
|-----------|--------|-----------|
| `AdminController` | `/api/admin` | Dashboard, ban/unban, uyarı, impersonation, kill switch |
| `MetricsController` | `/api/metrics` | Tüm metrik endpoint'leri + dead-letter |
| `UserController` | `/api/user` | Kullanıcı CRUD + kurum değiştirme |
| `ProblemController` | `/api/problem` | Sorun CRUD + moderasyon |
| `SolutionController` | `/api/solution` | Çözüm CRUD + moderasyon |
| `CapabilitiesController` | `/api/capabilities` | Capability kataloğu |
| `UserCapabilitiesController` | `/api/users/{id}/capabilities` | Grant/revoke |
| `CapabilityTemplatesController` | `/api/capability-templates` | Template CRUD + apply/revoke |
| `KillSwitchController` | `/api/killswitch` | Kill switch aktivasyon |
| `SecurityController` | `/api/security` | Güvenlik olayı sorgulama |
| `ConversationController` | `/api/conversation` | Sohbet yönetimi |

### 14.2 Frontend Servis Katmanı

| Servis | Dosya | Kapsam |
|--------|-------|--------|
| `adminService` | `services/adminService.ts` | Dashboard, ban, uyarı, impersonation |
| `metricsService` | `services/metricsService.ts` | Tüm metrik + audit log + dead-letter |
| `capabilityService` | `services/capabilityService.ts` | Grant/revoke, template, katalog |
| `userService` | `services/userService.ts` | Kullanıcı arama/listeleme |
| `institutionService` | `services/institutionService.ts` | Kurum listesi, subdomain |
| `reportService` | `services/reportService.ts` | Şikayet listesi ve çözümleme |
| `feedbackService` | `services/feedbackService.ts` | Geri bildirim gelen kutusu |
| `featureService` | `services/featureService.ts` | Feature grup ve tanım yönetimi |
| `securityService` | `services/securityService.ts` | Güvenlik olayı sorgulama |
| `userTitleService` | `services/userTitleService.ts` | Unvan atama/kaldırma |
| `chatService` | `services/chatService.ts` | Sohbet yönetimi |

---

## 15. Capability Kodu Referansı

### Admin Erişim Capability'leri

| Kod | Açıklama |
|-----|----------|
| `admin.system_access` | Admin paneline temel erişim |
| `admin.dashboard_view` | Dashboard ve genel bakış |
| `admin.overview` | Genel bakış sayfası |

### Kullanıcı Yönetimi Capability'leri

| Kod | Açıklama |
|-----|----------|
| `admin.user_ban` | Kullanıcı banlama |
| `admin.user_unban` | Ban kaldırma |
| `admin.user_impersonate` | Kimliğe bürünme |
| `admin.user_institution_change` | Kurum değiştirme |
| `admin.user_title_assign` | Unvan atama |
| `admin.user_warning_read_all` | Tüm uyarıları görme |
| `moderation.user_warn` | Uyarı verme |
| `moderation.user_warn_revoke` | Uyarı geri alma |

### İçerik Moderasyon Capability'leri

| Kod | Açıklama |
|-----|----------|
| `moderation.content_review` | İçerik inceleme erişimi |
| `moderation.report_resolve` | Şikayet çözümleme |
| `moderation.problem_resolve` | Sorunu çözüldü işaretleme |
| `moderation.problem_highlight` | Sorunu öne çıkarma |
| `moderation.problem_delete` | Sorun silme |
| `moderation.problem_close` | Sorun kapatma |
| `moderation.problem_reopen` | Sorunu yeniden açma |
| `moderation.problem_hide` | Sorun gizleme |
| `moderation.solution_highlight` | Çözümü öne çıkarma |
| `moderation.solution_delete` | Çözüm silme |
| `expert.solution_approve` | Uzman çözümü onaylama |
| `expert.solution_reject` | Uzman çözümü reddetme |

### Yetki Yönetimi Capability'leri

| Kod | Açıklama |
|-----|----------|
| `admin.capability_grant` | Capability verme |
| `admin.capability_revoke` | Capability kaldırma |
| `admin.capability_catalog_read` | Katalog görüntüleme |
| `admin.capability_audit_read` | Audit log görüntüleme |
| `admin.capability_template_apply` | Şablon uygulama |

### Sistem Capability'leri

| Kod | Açıklama |
|-----|----------|
| `admin.kill_switch` | Kill switch kontrolü |
| `admin.security_monitor` | Güvenlik izleme |
| `admin.feature_group_manage` | Feature grup yönetimi |
| `admin.feature_definition_manage` | Feature tanım yönetimi |
| `admin.audit_read` | Audit log okuma |
| `admin.audit_export` | Audit log dışa aktarma |
| `admin.cross_institution_read` | Çapraz kurum veri okuma |
| `admin.user_institution_change` | Kullanıcı kurumu değiştirme |

### Metrik Capability'leri

| Kod | Açıklama |
|-----|----------|
| `admin.metrics_user_view` | Kullanıcı metrikleri |
| `admin.metrics_workflow_view` | Workflow metrikleri |
| `admin.metrics_capability_view` | Yetki metrikleri |
| `admin.metrics_system_health_view` | Sistem sağlık metrikleri |

### Workflow Capability'leri

| Kod | Açıklama |
|-----|----------|
| `admin.rule_create` | Workflow kaydetme |
| `admin.rule_test_run` | Test çalıştırma |
| `expert.csharp_execute` | C# node çalıştırma |
| `workflow.view_logs` | Workflow loglarını görme |

### Sayfa Erişim Capability'leri (`page.*`)

| Kod | Sayfa |
|-----|-------|
| `page.admin.monitor` | Canlı Radar |
| `page.admin.overview` | Genel Bakış |
| `page.admin.problems` | Sorunlar |
| `page.admin.solutions` | Çözümler |
| `page.admin.topics` | Kategoriler |
| `page.admin.expert_approvals` | Uzman Onayları |
| `page.admin.users` | Kullanıcılar |
| `page.admin.institutions` | Kurumlar |
| `page.admin.reports` | Şikayet Merkezi |
| `page.admin.feedbacks` | Geri Bildirimler |
| `page.admin.chat` | Sohbet Yönetimi |
| `page.admin.security` | Güvenlik İzleme |
| `page.admin.settings` | Sistem Ayarları |
| `page.admin.announcements` | Duyurular |
| `page.admin.logs` | Sistem Logları |
| `page.admin.activity_logs` | Aksiyon Geçmişi |
| `page.admin.kill_switch` | Kill Switch |
| `page.admin.agreements` | Sözleşmeler |
| `page.admin.email_templates` | E-posta Şablonları |
| `page.admin.corporate` | Hakkımızda |
| `page.admin.workflow` | Workflow Builder |
| `page.admin.features` | Modül Yönetimi |
| `page.admin.capabilities` | Yetki Yönetimi |
| `page.admin.capability_templates` | Yetki Şablonları |
| `page.admin.capability_audit` | Yetki Logları |
| `page.admin.metrics` | Metrikler Paneli |

---

*Bu dokümantasyon `CLAUDE.md` ile birlikte güncel tutulmalıdır. Her yeni sekme veya capability eklendiğinde ilgili bölümler güncellenmelidir.*
