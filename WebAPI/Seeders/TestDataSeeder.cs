using Core.Entities.Concrete;
using Core.Utilities.Security.Hashing;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;

namespace WebAPI.Seeders;

/// <summary>
/// Geliştirme ortamı için gerçekçi Türkçe test verileri ekler.
/// Idempotent: Problems tablosunda veri varsa çalışmaz.
/// </summary>
public static class TestDataSeeder
{
    public static void Seed(DevelopTurkeyContext context, IConfiguration config)
    {
        // Sadece Development ortamında çalış
        var env = config["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        if (!env.Equals("Development", StringComparison.OrdinalIgnoreCase)) return;

        // Idempotency: test verisi zaten yüklüyse çıkış
        if (context.Problems.Any()) return;

        // ─────────────────────────────────────────────────────────────
        // 1. KURUMLAR
        // ─────────────────────────────────────────────────────────────
        var institutions = new List<Institution>
        {
            new() { Name = "Ankara Büyükşehir Belediyesi", Subtitle = "Resmi Kurum Ağı",
                    Domain = "ankara.bel.tr", PrimaryColor = "#c0392b", Status = true },
            new() { Name = "Türkiye Teknoloji Ağı", Subtitle = "Yazılım & Teknoloji Topluluğu",
                    Domain = "teknoloji.org.tr", PrimaryColor = "#2980b9", Status = true },
            new() { Name = "Eğitim Kurumları Birliği", Subtitle = "Eğitim ve Öğretim Ağı",
                    Domain = "egitim.org.tr", PrimaryColor = "#27ae60", Status = true },
        };

        // Institution 1 (Id=1) BootstrapAdmin tarafından kullanılıyor, onu güncelle ya da yeni ekle
        if (!context.Institutions.Any(i => i.Domain == "ankara.bel.tr"))
        {
            context.Institutions.AddRange(institutions);
            context.SaveChanges();
        }

        var instAnkara  = context.Institutions.First(i => i.Domain == "ankara.bel.tr");
        var instTekno   = context.Institutions.First(i => i.Domain == "teknoloji.org.tr");
        var instEgitim  = context.Institutions.First(i => i.Domain == "egitim.org.tr");

        // ─────────────────────────────────────────────────────────────
        // 2. KULLANICILAR (hepsi şifre: Test1234!)
        // ─────────────────────────────────────────────────────────────
        HashingHelper.CreatePasswordHash("Test1234!", out var ph, out var ps);

        User MakeUser(string username, string name, string surname, string email,
                      int institutionId, int city, int gender = 0)
        {
            HashingHelper.CreatePasswordHash("Test1234!", out var h, out var s);
            return new User
            {
                UserName = username, Name = name, Surname = surname, Email = email,
                PasswordHash = h, PasswordSalt = s,
                CityCode = city, Gender = gender,
                IsEmailVerified = true, IsProfilePublic = true,
                ShowProblems = true, ShowSolutions = true,
                RegisterDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(10, 180)),
                InstitutionId = institutionId,
            };
        }

        var users = new List<User>
        {
            // Ankara Belediyesi kullanıcıları
            MakeUser("ahmet.yilmaz",   "Ahmet",    "Yılmaz",   "ahmet.yilmaz@ankara.bel.tr",   instAnkara.Id, 6,  0),
            MakeUser("fatma.kaya",     "Fatma",    "Kaya",     "fatma.kaya@ankara.bel.tr",      instAnkara.Id, 6,  1),
            MakeUser("mehmet.demir",   "Mehmet",   "Demir",    "mehmet.demir@ankara.bel.tr",    instAnkara.Id, 6,  0),
            MakeUser("zeynep.celik",   "Zeynep",   "Çelik",    "zeynep.celik@ankara.bel.tr",    instAnkara.Id, 6,  1),
            MakeUser("mustafa.sahin",  "Mustafa",  "Şahin",    "mustafa.sahin@ankara.bel.tr",   instAnkara.Id, 6,  0),

            // Teknoloji Ağı kullanıcıları
            MakeUser("ali.ozturk",     "Ali",      "Öztürk",   "ali.ozturk@teknoloji.org.tr",   instTekno.Id,  34, 0),
            MakeUser("ayse.arslan",    "Ayşe",     "Arslan",   "ayse.arslan@teknoloji.org.tr",  instTekno.Id,  34, 1),
            MakeUser("emre.kilic",     "Emre",     "Kılıç",    "emre.kilic@teknoloji.org.tr",   instTekno.Id,  34, 0),
            MakeUser("selin.kurt",     "Selin",    "Kurt",     "selin.kurt@teknoloji.org.tr",   instTekno.Id,  34, 1),
            MakeUser("can.aydın",      "Can",      "Aydın",    "can.aydin@teknoloji.org.tr",    instTekno.Id,  35, 0),

            // Eğitim Birliği kullanıcıları
            MakeUser("hasan.yıldız",   "Hasan",    "Yıldız",   "hasan.yildiz@egitim.org.tr",    instEgitim.Id, 6,  0),
            MakeUser("merve.guler",    "Merve",    "Güler",    "merve.guler@egitim.org.tr",     instEgitim.Id, 6,  1),
            MakeUser("ibrahim.cinar",  "İbrahim",  "Çınar",    "ibrahim.cinar@egitim.org.tr",   instEgitim.Id, 16, 0),
            MakeUser("neslihan.ak",    "Neslihan", "Ak",       "neslihan.ak@egitim.org.tr",     instEgitim.Id, 16, 1),
            MakeUser("sercan.bas",     "Sercan",   "Baş",      "sercan.bas@egitim.org.tr",      instEgitim.Id, 26, 0),
        };

        context.Users.AddRange(users);
        context.SaveChanges();

        // Kısayollar
        var u = users; // u[0]..u[14]

        // ─────────────────────────────────────────────────────────────
        // 3. KATEGORİLER (Topics)
        // ─────────────────────────────────────────────────────────────
        var topicsAnkara = new[]
        {
            new Topic { Name = "Altyapı & Yol",         ImageName = "altyapi",     Status = true, InstitutionId = instAnkara.Id },
            new Topic { Name = "Çevre & Temizlik",       ImageName = "cevre",       Status = true, InstitutionId = instAnkara.Id },
            new Topic { Name = "Ulaşım & Trafik",        ImageName = "ulasim",      Status = true, InstitutionId = instAnkara.Id },
            new Topic { Name = "Park & Yeşil Alan",      ImageName = "park",        Status = true, InstitutionId = instAnkara.Id },
            new Topic { Name = "Su & Kanalizasyon",      ImageName = "su",          Status = true, InstitutionId = instAnkara.Id },
        };

        var topicsTekno = new[]
        {
            new Topic { Name = "Yazılım Geliştirme",    ImageName = "yazilim",      Status = true, InstitutionId = instTekno.Id },
            new Topic { Name = "Siber Güvenlik",         ImageName = "guvenlik",    Status = true, InstitutionId = instTekno.Id },
            new Topic { Name = "Yapay Zeka & ML",        ImageName = "ai",          Status = true, InstitutionId = instTekno.Id },
            new Topic { Name = "Açık Kaynak",            ImageName = "opensource",  Status = true, InstitutionId = instTekno.Id },
        };

        var topicsEgitim = new[]
        {
            new Topic { Name = "Müfredat & Ders",       ImageName = "mufredat",     Status = true, InstitutionId = instEgitim.Id },
            new Topic { Name = "Dijital Araçlar",        ImageName = "dijital",     Status = true, InstitutionId = instEgitim.Id },
            new Topic { Name = "Öğretmen Gelişimi",      ImageName = "ogretmen",    Status = true, InstitutionId = instEgitim.Id },
            new Topic { Name = "Okul Altyapısı",         ImageName = "okul",        Status = true, InstitutionId = instEgitim.Id },
        };

        context.Topics.AddRange(topicsAnkara);
        context.Topics.AddRange(topicsTekno);
        context.Topics.AddRange(topicsEgitim);
        context.SaveChanges();

        var ta = topicsAnkara; // ta[0..4]
        var tt = topicsTekno;  // tt[0..3]
        var te = topicsEgitim; // te[0..3]

        // ─────────────────────────────────────────────────────────────
        // 4. SORUNLAR (Problems)
        // ─────────────────────────────────────────────────────────────
        var now = DateTime.UtcNow;

        Problem P(int senderId, string title, string desc, int city, string? addr,
                  double? lat, double? lon, int institutionId, bool highlighted = false,
                  bool resolved = false, int views = 0)
            => new()
            {
                SenderId = senderId, Title = title, Description = desc,
                CityCode = city, Address = addr, Latitude = lat, Longitude = lon,
                InstitutionId = institutionId, IsHighlighted = highlighted,
                IsResolved = resolved, ViewCount = views,
                SendDate = now.AddDays(-Random.Shared.Next(1, 90)),
            };

        var problems = new List<Problem>
        {
            // Ankara Belediyesi sorunları (institutionId = instAnkara.Id)
            P(u[0].Id, "Çankaya Bulvarı kaldırım çöküntüsü",
              "Çankaya Bulvarı üzerinde yaklaşık 50 metrelik kaldırım tamamen çökmüş durumda. "
              + "Yağmur yağdığında burası büyük bir su birikintisi haline geliyor ve "
              + "yayalar tehlike altında. Uzun süredir bildirilmesine rağmen müdahale yapılmıyor.",
              6, "Çankaya Bulvarı, Çankaya, Ankara",
              39.9208, 32.8541, instAnkara.Id, highlighted: true, views: 342),

            P(u[1].Id, "Dikmen Vadisi parkındaki çöpler toplanmıyor",
              "Dikmen Vadisi parkında çöp konteynerleri haftadır boşaltılmıyor. "
              + "Yaz sıcağında koku ve hijyen sorunu ciddi boyutlara ulaştı. "
              + "Çocuklar burada oynuyor, ailelerin ciddi şikayeti var.",
              6, "Dikmen Vadisi Parkı, Ankara",
              39.8980, 32.8420, instAnkara.Id, views: 218),

            P(u[2].Id, "Kızılay meydanı trafik ışıkları arızalı",
              "Kızılay Meydanı'nda ana kavşaktaki trafik ışıklarının bir kısmı 3 gündür "
              + "arızalı. Sabah ve akşam saatlerinde büyük trafik tıkanıklığı oluşuyor. "
              + "Kavşakta iki küçük trafik kazası meydana geldi.",
              6, "Kızılay Meydanı, Çankaya, Ankara",
              39.9200, 32.8543, instAnkara.Id, highlighted: true, views: 587),

            P(u[3].Id, "Bahçelievler mahallesinde su kesintisi",
              "3 günden fazladır mahallede su gelmiyor. Belediyeye defalarca aradık, "
              + "sadece 'çalışma yapılıyor' deniyor ama hiç ekip görmedik. "
              + "Çocuklar ve yaşlılar olan bir mahallede bu durum kabul edilemez.",
              6, "Bahçelievler Mahallesi, Çankaya, Ankara",
              39.9112, 32.8267, instAnkara.Id, views: 401),

            P(u[4].Id, "Altındağ sokaklarında kaldırım taşları kopuk ve tehlikeli",
              "Altındağ ilçesinin birçok sokağında kaldırım taşları yerinden çıkmış, "
              + "oynak ve tehlikeli hale gelmiş. Yaşlı vatandaşlar ve çocuklar "
              + "düşme riskiyle karşı karşıya. Fotoğraflarla belgeledim.",
              6, "Altındağ İlçesi, Ankara",
              39.9428, 32.8739, instAnkara.Id, views: 165),

            P(u[0].Id, "Mamak'ta kanalizasyon taşkını",
              "Mamak ilçesinde son yağışlar sonrası kanalizasyon sistemi taşarak "
              + "sokakları kapladı. Pislik görüntüsü ve koku kabul edilemez boyutta. "
              + "Çocuklar okula gidemiyor çünkü yollar geçilemiyor.",
              6, "Mamak İlçesi, Ankara",
              39.9356, 32.9242, instAnkara.Id, views: 523),

            P(u[1].Id, "Sincan metrosunda havalandırma sorunu",
              "Sincan metro hattındaki vagonlarda havalandırma sistemi çalışmıyor. "
              + "Yaz sıcağında yolcular bunalıyor, özellikle yaşlılar ve çocuklar "
              + "için tehlikeli bir ortam oluşuyor. İstanbul'daki metro kalitesiyle "
              + "karşılaştırıldığında rezalet.",
              6, "Sincan Metro İstasyonu, Ankara",
              39.9735, 32.5831, instAnkara.Id, views: 289),

            P(u[2].Id, "Etimesgut yeşil alan bakımı yapılmıyor",
              "Etimesgut'taki belediye parkı tamamen ihmal edilmiş durumda. "
              + "Çimler biçilmiyor, çiçekler solmuş, banklar kırık. "
              + "Park düzenli olarak çocuklarla geliyoruz ama artık gelmeye "
              + "utanır olduk.",
              6, "Etimesgut Belediye Parkı, Ankara",
              39.9547, 32.6833, instAnkara.Id, resolved: true, views: 198),

            P(u[3].Id, "Keçiören'de sokak lambaları yanmıyor",
              "Keçiören Bağlum Mahallesi'nde bir aydan fazladır sokak lambalarının "
              + "büyük çoğunluğu yanmıyor. Gece karanlıkta yürümek zorunda kalıyoruz. "
              + "Mahalle sakinleri güvensizlik yaşıyor, hırsızlık vakaları arttı.",
              6, "Bağlum Mahallesi, Keçiören, Ankara",
              40.0023, 32.8431, instAnkara.Id, views: 312),

            P(u[4].Id, "Polatlı köy yollarında asfalt bozukluğu",
              "Polatlı ilçesine bağlı Seyran Köyü yolu tamamen bozulmuş durumda. "
              + "Köprü üzerindeki delikler araçlara zarar veriyor. Köye ulaşım "
              + "çok güçleşti, tarım araçları geçemiyor.",
              6, "Seyran Köyü, Polatlı, Ankara",
              39.5843, 32.1285, instAnkara.Id, views: 143),

            // Teknoloji Ağı sorunları (institutionId = instTekno.Id)
            P(u[5].Id, ".NET 8 ile Entity Framework migration hatası",
              "EF Core 8'e yükseltme sonrası `dotnet ef migrations add` komutu "
              + "çalışıyor gibi görünüyor ama generated migration boş çıkıyor. "
              + "Tüm entity değişikliklerini yaptım, `OnModelCreating` de güncel. "
              + "Proje .NET 8 preview 5 kullanıyor.",
              34, null, null, null, instTekno.Id, views: 456),

            P(u[6].Id, "React 18 concurrent mode ile state tutarsızlığı",
              "React 18'e geçtikten sonra `useEffect` içindeki setState çağrıları "
              + "bazı durumlarda iki kez tetikleniyor. Strict Mode kapatınca sorun "
              + "kayboluyor ama bu doğru çözüm değil. Bağlantı hata raporunu da ekledim.",
              34, null, null, null, instTekno.Id, highlighted: true, views: 734),

            P(u[7].Id, "Docker container'da timezone sorunu",
              "Alpine tabanlı Docker image'da container ayağa kalktığında zaman dilimi "
              + "UTC oluyor, Türkiye saatini ayarlayamıyorum. ENV TZ=Europe/Istanbul "
              + "denedim çalışmadı. .NET uygulaması DateTime.Now ile yanlış zaman alıyor.",
              34, null, null, null, instTekno.Id, views: 312),

            P(u[8].Id, "GitHub Actions CI/CD pipeline Redis bağlantı hatası",
              "CI ortamında Redis servisini `services:` olarak tanımladım ama "
              + "uygulama `Connection refused` hatası alıyor. Lokal ortamda "
              + "aynı docker-compose ile mükemmel çalışıyor. Nerede hata yapıyorum?",
              34, null, null, null, instTekno.Id, views: 289),

            P(u[9].Id, "JWT token yenileme (refresh) güvenli nasıl yapılır?",
              "Access token süresi dolduğunda kullanıcıyı logout etmeden token "
              + "yenilemek istiyorum. Refresh token'ı nerede saklıyım — localStorage "
              + "mi, HttpOnly cookie mi? CSRF ve XSS açısından en güvenli yaklaşım nedir?",
              35, null, null, null, instTekno.Id, views: 821),

            P(u[5].Id, "Python ML modelini production'a almak için en iyi yöntem?",
              "Scikit-learn modelini eğittim, 85% accuracy var. Bunu bir REST API "
              + "olarak yayınlamak istiyorum. FastAPI mi kullanayım, Flask mi? "
              + "Model güncelleme (model versioning) nasıl yönetirim? MLflow denedim "
              + "ama kurulum karmaşık geldi.",
              34, null, null, null, instTekno.Id, views: 367),

            P(u[6].Id, "PostgreSQL full-text search Türkçe karakter desteği",
              "PostgreSQL'de `tsvector` ile Türkçe full-text arama yapmak istiyorum. "
              + "Türkçe karakterler (ğ, ş, ı, ö, ü, ç) doğru normalize edilmiyor, "
              + "örneğin 'şehir' aramasında 'sehir' çıkmıyor. Turkish locale kurulu ama "
              + "stemmer yok.",
              34, null, null, null, instTekno.Id, views: 198),

            P(u[7].Id, "Kubernetes pod'lar arası güvenli iletişim nasıl kurulur?",
              "Farklı namespace'lerdeki pod'ların birbiriyle güvenli iletişim kurması "
              + "gerekiyor. NetworkPolicy mi kullanayım, mTLS mi? Istio service mesh "
              + "kurulumu çok karmaşık geldi. Küçük bir cluster için ne önerirsiniz?",
              34, null, null, null, instTekno.Id, views: 245),

            // Eğitim Birliği sorunları (institutionId = instEgitim.Id)
            P(u[10].Id, "Uzaktan eğitim platformu sürekli çöküyor",
              "Okulumuzun kullandığı uzaktan eğitim platformu (EBA) özellikle sınav "
              + "günlerinde çöküyor. 500 öğrenci aynı anda bağlanınca sistem cevap "
              + "vermeyi durduruyor. İki sınavı ertelemek zorunda kaldık.",
              6, null, null, null, instEgitim.Id, highlighted: true, views: 634),

            P(u[11].Id, "Matematik öğretmeni açığı — yıldır doldurulamıyor",
              "Okulumuzda 3 yıldır matematik öğretmeni açığı var. Ücretli öğretmenler "
              + "sürekli değişiyor, müfredat tutarsız işleniyor. 10. sınıf öğrencileri "
              + "üniversite sınavına hazırlanamıyor.",
              6, null, null, null, instEgitim.Id, views: 482),

            P(u[12].Id, "Okul kafeteryasında sağlıksız yiyecekler",
              "Kafeteryada satılan ürünlerin büyük çoğunluğu yüksek şekerli, işlenmiş "
              + "gıdalar. Sağlıklı alternatifler yok. Veliler olarak defalarca şikayet "
              + "ettik ama değişen bir şey olmadı. Beslenme uzmanı raporu istedik, gelmedi.",
              6, null, null, null, instEgitim.Id, views: 356),

            P(u[13].Id, "Akıllı tahta sistemleri çalışmıyor",
              "Bakanlık tarafından temin edilen akıllı tahta sistemlerinin %60'ı arızalı. "
              + "Teknik destek isteğimiz haftalar öncesine kadar cevaplandırılmıyor. "
              + "Öğretmenler powerpoint açamıyor, ders verimliliği düştü.",
              16, null, null, null, instEgitim.Id, views: 289),

            P(u[14].Id, "Sınıf mevcutları 50'yi geçiyor",
              "Lisenin bazı sınıflarında öğrenci sayısı 50'yi aşıyor. Fiziki alan yetersiz, "
              + "öğrenciler birbirlerinin üstüne oturuyor. Pedagojik açıdan bu durumun "
              + "yarattığı sorunları da ayrıca rapor ettim. Ek derslik yapılması gerekiyor.",
              16, null, null, null, instEgitim.Id, views: 421),

            P(u[10].Id, "Okul kütüphanesi yıllardır kapali",
              "İlçedeki ortaokul kütüphanesi 3 yıldır kapalı. Kitaplar depoda bekleniyor, "
              + "öğrenciler kaynak bulamıyor. Belediye ile ortak proje başlatılacaktı ama "
              + "hiçbir somut adım atılmadı.",
              6, null, null, null, instEgitim.Id, views: 178),

            P(u[11].Id, "Okul servis ücretleri denetimsiz artıyor",
              "Okul servisi ücretleri bu yıl %85 arttı. Veliler olarak asgari ücretin "
              + "çok üzerinde ödeme yapıyoruz. Taşıma ihaleleri şeffaf değil, denetim "
              + "mekanizması işlemiyor.",
              6, null, null, null, instEgitim.Id, views: 267),
        };

        context.Problems.AddRange(problems);
        context.SaveChanges();

        // ─────────────────────────────────────────────────────────────
        // 5. PROBLEM-KATEGORİ İLİŞKİLERİ
        // ─────────────────────────────────────────────────────────────
        var problemTopics = new List<ProblemTopic>
        {
            // Ankara sorunları
            new() { ProblemId = problems[0].Id, TopicId = ta[0].Id },  // Altyapı & Yol
            new() { ProblemId = problems[1].Id, TopicId = ta[1].Id },  // Çevre & Temizlik
            new() { ProblemId = problems[2].Id, TopicId = ta[2].Id },  // Ulaşım & Trafik
            new() { ProblemId = problems[3].Id, TopicId = ta[4].Id },  // Su & Kanalizasyon
            new() { ProblemId = problems[4].Id, TopicId = ta[0].Id },
            new() { ProblemId = problems[5].Id, TopicId = ta[4].Id },
            new() { ProblemId = problems[6].Id, TopicId = ta[2].Id },
            new() { ProblemId = problems[7].Id, TopicId = ta[3].Id },  // Park & Yeşil Alan
            new() { ProblemId = problems[8].Id, TopicId = ta[1].Id },
            new() { ProblemId = problems[9].Id, TopicId = ta[0].Id },
            // Tekno sorunları
            new() { ProblemId = problems[10].Id, TopicId = tt[0].Id }, // Yazılım
            new() { ProblemId = problems[11].Id, TopicId = tt[0].Id },
            new() { ProblemId = problems[12].Id, TopicId = tt[0].Id },
            new() { ProblemId = problems[13].Id, TopicId = tt[0].Id },
            new() { ProblemId = problems[14].Id, TopicId = tt[1].Id }, // Siber Güvenlik
            new() { ProblemId = problems[15].Id, TopicId = tt[2].Id }, // AI & ML
            new() { ProblemId = problems[16].Id, TopicId = tt[0].Id },
            new() { ProblemId = problems[17].Id, TopicId = tt[1].Id },
            // Eğitim sorunları
            new() { ProblemId = problems[18].Id, TopicId = te[1].Id }, // Dijital Araçlar
            new() { ProblemId = problems[19].Id, TopicId = te[2].Id }, // Öğretmen Gelişimi
            new() { ProblemId = problems[20].Id, TopicId = te[0].Id }, // Müfredat
            new() { ProblemId = problems[21].Id, TopicId = te[3].Id }, // Okul Altyapısı
            new() { ProblemId = problems[22].Id, TopicId = te[0].Id },
            new() { ProblemId = problems[23].Id, TopicId = te[3].Id },
            new() { ProblemId = problems[24].Id, TopicId = te[0].Id },
        };

        context.ProblemTopics.AddRange(problemTopics);
        context.SaveChanges();

        // ─────────────────────────────────────────────────────────────
        // 6. PROBLEM UPVOTE'LAR
        // ─────────────────────────────────────────────────────────────
        var upvotes = new List<ProblemUpvote>();
        var upvotePairs = new (int ui, int pi)[]
        {
            (0,0),(1,0),(2,0),(3,0),(4,0),               // problem 0
            (0,1),(2,1),(4,1),                            // problem 1
            (0,2),(1,2),(2,2),(3,2),(4,2),(5,2),(6,2),   // problem 2
            (1,3),(2,3),(3,3),
            (0,4),(1,4),
            (0,5),(1,5),(2,5),(3,5),(4,5),               // problem 5
            (5,10),(6,10),(7,10),(8,10),(9,10),          // tekno 0
            (5,11),(6,11),(7,11),(8,11),
            (5,12),(6,12),
            (5,14),(6,14),(7,14),(8,14),(9,14),          // jwt
            (10,18),(11,18),(12,18),(13,18),(14,18),     // eğitim 0
            (10,19),(11,19),(12,19),
            (10,22),(11,22),
        };
        foreach (var (ui, pi) in upvotePairs)
            upvotes.Add(new ProblemUpvote { UserId = u[ui].Id, ProblemId = problems[pi].Id,
                CreatedAt = now.AddDays(-Random.Shared.Next(0, 30)) });

        context.ProblemUpvotes.AddRange(upvotes);
        context.SaveChanges();

        // ─────────────────────────────────────────────────────────────
        // 7. ÇÖZÜMLER (Solutions)
        // ─────────────────────────────────────────────────────────────
        Solution S(int senderId, int problemId, string title, string desc,
                   int institutionId, int approvalStatus = 0, bool highlighted = false)
            => new()
            {
                SenderId = senderId, ProblemId = problemId,
                Title = title, Description = desc,
                InstitutionId = institutionId,
                ExpertApprovalStatus = approvalStatus, IsHighlighted = highlighted,
                SendDate = now.AddDays(-Random.Shared.Next(1, 60)),
            };

        var solutions = new List<Solution>
        {
            // Problem 0: Kaldırım çöküntüsü
            S(u[2].Id, problems[0].Id,
              "Acil beton dökümü ve güvenlik bariyeri",
              "Çöküntü bölgesine önce geçici güvenlik bariyerleri kurulmalı, "
              + "ardından zemin stabilizasyonu yapılarak B20 sınıfı beton ile "
              + "onarım gerçekleştirilmeli. Maliyeti yaklaşık 15.000 TL. "
              + "İlçe belediyesi altyapı müdürlüğüne formel başvuru yapılabilir.",
              instAnkara.Id, approvalStatus: 1, highlighted: true),

            S(u[3].Id, problems[0].Id,
              "Geotekstil ile zemin iyileştirmesi",
              "Kaldırım altındaki toprağın su tutması sonucu çöktüğü anlaşılıyor. "
              + "Geotekstil filtre + kırma taş dolgu + vibrasyon sıkıştırması ile "
              + "kalıcı çözüm sağlanabilir. Kısa vadede poliüretan enjeksiyon da denenebilir.",
              instAnkara.Id, approvalStatus: 0),

            // Problem 1: Çöp sorunu
            S(u[4].Id, problems[1].Id,
              "Akıllı çöp konteyneri sistemi önerisi",
              "Sensörlü akıllı konteynerler doluluk bilgisini otomatik olarak "
              + "belediyeye iletir. Ankara'da pilot uygulama başlatılabilir. "
              + "Başlangıç için 20 adet konteyner kurulumu + uygulama geliştirme "
              + "yaklaşık 250.000 TL bütçe gerektirir.",
              instAnkara.Id, approvalStatus: 0),

            // Problem 2: Trafik ışıkları
            S(u[1].Id, problems[2].Id,
              "Yedek parça stoğu ve uzaktan monitörleme sistemi",
              "EGO Genel Müdürlüğü'nün trafik ışık bakım ekibine haber verilmeli. "
              + "Acil müdahale için EGO'nun 24 saat hizmet hattı: 0312 xxx xx xx. "
              + "Uzun vadede ışıkların IoT altyapısına geçişi süreci hızlandırır.",
              instAnkara.Id, approvalStatus: 1, highlighted: true),

            // Problem 3: Su kesintisi
            S(u[0].Id, problems[3].Id,
              "ASKİ acil ihbar ve talepte bulunun",
              "ASKİ'nin 24 saat açık ihbar hattı 185 numarasından bildirim yapılabilir. "
              + "Aynı zamanda ASKİ web sitesindeki e-ihbar formu doldurulabilir. "
              + "3 günü aşan kesintiler için ilçe kaymakamlığına da başvurabilirsiniz.",
              instAnkara.Id, approvalStatus: 2),

            S(u[4].Id, problems[3].Id,
              "Mahalledeki mevcut boru hattının yenilenmesi",
              "50+ yıllık dökme demir borular düzenli kesintiye neden oluyor. "
              + "Polietilen boru ile yenileme çalışması uzun vadede hem "
              + "kesintileri hem de su kayıplarını azaltır. ASKİ yatırım "
              + "planına alınması için dilekçe imza kampanyası başlatılabilir.",
              instAnkara.Id, approvalStatus: 0),

            // Problem 5: Kanalizasyon
            S(u[2].Id, problems[5].Id,
              "Yağmur suyu ve kanalizasyon hattı ayrıştırılması",
              "Yoğun yağışlarda kanalizasyon taşmasının temel nedeni birleşik sistem. "
              + "AB fonları desteğiyle yağmur suyu ve kanalizasyon hatlarının "
              + "ayrıştırılması projesi hazırlanabilir. Benzer proje Bursa'da başarıyla uygulandı.",
              instAnkara.Id, approvalStatus: 0),

            // Problem 10: .NET migration
            S(u[6].Id, problems[10].Id,
              "`dotnet ef migrations add` öncesi snapshot temizleme",
              "Bu sorun genellikle `__EFMigrationsHistory` ile `ModelSnapshot.cs` "
              + "arasındaki tutarsızlıktan kaynaklanır. `bin` ve `obj` klasörlerini "
              + "silip `dotnet ef database drop --force` ardından tekrar migration "
              + "deneyiniz. Ayrıca `IEntityTypeConfiguration` kullanıyorsanız "
              + "`ApplyConfigurationsFromAssembly` yöntemini tercih edin.",
              instTekno.Id, approvalStatus: 1, highlighted: true),

            S(u[7].Id, problems[10].Id,
              "DbContext factory'yi kontrol edin",
              "`IDesignTimeDbContextFactory<TContext>` implement etmeyi deneyin. "
              + "Migration tasarım zamanı farklı bir connection string / ortam "
              + "konfigürasyonu gerektiriyor olabilir. "
              + "```csharp\npublic class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> { ... }\n```",
              instTekno.Id, approvalStatus: 0),

            // Problem 11: React 18 strict mode
            S(u[5].Id, problems[11].Id,
              "useEffect çift çalışması Strict Mode davranışı — bu bir bug değil",
              "React 18 Strict Mode geliştirme ortamında effect'leri kasıtlı olarak "
              + "iki kez çalıştırır. Bu, side-effect temizliği (cleanup) yazıp "
              + "yazmadığınızı test etmek içindir. Production build'de tek çalışır. "
              + "Cleanup fonksiyonu eklemeniz yeterli:\n```js\nuseEffect(() => {\n  const sub = subscribe();\n  return () => sub.unsubscribe();\n}, []);\n```",
              instTekno.Id, approvalStatus: 1, highlighted: true),

            // Problem 12: Docker timezone
            S(u[8].Id, problems[12].Id,
              "Alpine'da tzdata paketi kurulumu gerekli",
              "Alpine base image'da timezone database mevcut değil. Dockerfile'a "
              + "şunu ekleyin:\n```dockerfile\nRUN apk add --no-cache tzdata\nENV TZ=Europe/Istanbul\n```\n"
              + ".NET tarafında da `TimeZoneInfo.FindSystemTimeZoneById(\"Europe/Istanbul\")` "
              + "kullanarak UTC'den dönüşüm yapabilirsiniz.",
              instTekno.Id, approvalStatus: 1),

            // Problem 13: GitHub Actions Redis
            S(u[9].Id, problems[13].Id,
              "Redis servis container hostname'i `localhost` değil `redis`",
              "GitHub Actions'da `services:` altındaki container'a uygulama "
              + "`localhost` ile değil, servis adıyla bağlanır. "
              + "Connection string'inizi şöyle güncelleyin:\n"
              + "```yaml\nredis://redis:6379\n```\n"
              + "Ya da environment variable ile geçin: `REDIS_URL: redis://redis:6379`",
              instTekno.Id, approvalStatus: 1, highlighted: true),

            // Problem 14: JWT
            S(u[5].Id, problems[14].Id,
              "HttpOnly cookie + SameSite=Strict ile güvenli refresh token",
              "En güvenli yaklaşım: access token (kısa ömürlü, 15 dk) memory'de, "
              + "refresh token HttpOnly SameSite=Strict cookie'de. "
              + "CSRF: SameSite cookie + double-submit cookie pattern. "
              + "XSS: HttpOnly cookie JS tarafından okunamaz. "
              + "Refresh endpoint'i /auth/refresh olsun, sadece cookie okunsun. "
              + "localStorage KESİNLİKLE kullanmayın.",
              instTekno.Id, approvalStatus: 1, highlighted: true),

            S(u[6].Id, problems[14].Id,
              "Token Rotation ile güvenlik artırımı",
              "Refresh token kullanıldığında eski invalidate edilip yeni üretilmeli "
              + "(Refresh Token Rotation). Bu sayede çalınan bir refresh token "
              + "tespit edilebilir. Ayrıca refresh token'a IP + User-Agent fingerprint "
              + "bağlayabilirsiniz. Redis üzerinde token blacklist tutmanız önerilir.",
              instTekno.Id, approvalStatus: 0),

            // Problem 18: Uzaktan eğitim platformu
            S(u[12].Id, problems[18].Id,
              "CDN ve load balancer entegrasyonu ile ölçekleme",
              "Sınav günlerinde peak traffic için Cloudflare CDN statik içerikleri "
              + "önbelleğe almalı, uygulama sunucusu horizontal scaling ile "
              + "desteklenmeli. Ayrıca sınav zamanlaması kademelendirilerek "
              + "aynı anda bağlananların sayısı azaltılabilir.",
              instEgitim.Id, approvalStatus: 0),

            S(u[13].Id, problems[18].Id,
              "Yedek platform: Google Classroom ve Meet kombinasyonu",
              "Platform çöktüğünde yedek seçenek olarak Google Workspace for Education "
              + "hazır tutulabilir. Ücretsiz ve ölçeklenebilir. Öğretmenlere 2 saatlik "
              + "eğitim yeterli. Fatura edilebilir bir yedek yerine ücretsiz alternatif "
              + "tercih edilmeli.",
              instEgitim.Id, approvalStatus: 1),

            // Problem 19: Öğretmen açığı
            S(u[10].Id, problems[19].Id,
              "Emekli matematik öğretmenleri havuzu oluşturulması",
              "MEB'in emekli öğretmen havuzundan gönüllü destek talep edilebilir. "
              + "Aynı zamanda üniversitelerin matematik bölümü son sınıf öğrencileri "
              + "staj kapsamında katkı sağlayabilir. Okul-Üniversite ortaklığı kurulması "
              + "için İl Millî Eğitim Müdürlüğü'ne resmi başvuru yapılmalı.",
              instEgitim.Id, approvalStatus: 1, highlighted: true),

            // Problem 21: Akıllı tahta
            S(u[11].Id, problems[21].Id,
              "MEBBİS üzerinden teknik destek talebi açın",
              "Arızalı akıllı tahta için MEBBİS sistemi üzerinden e-destek talebi "
              + "açılabilir. Talep numarasını alıp İl Millî Eğitim Müdürlüğü'ne "
              + "ileterek takip edilmelidir. Taleplerin 15 iş günü içinde "
              + "yanıtlanması yasal zorunluluk.",
              instEgitim.Id, approvalStatus: 0),
        };

        context.Solutions.AddRange(solutions);
        context.SaveChanges();

        // ─────────────────────────────────────────────────────────────
        // 8. ÇÖZÜM OY'LARI
        // ─────────────────────────────────────────────────────────────
        var solutionVotes = new List<SolutionVote>();
        var votePairs = new (int ui, int si, bool up)[]
        {
            (1,0,true),(2,0,true),(3,0,true),(4,0,true),
            (0,3,true),(2,3,true),(4,3,true),(1,3,false),
            (6,7,true),(7,7,true),(8,7,true),(9,7,true),
            (5,9,true),(6,9,true),(7,9,true),
            (5,11,true),(6,11,true),(7,11,true),(8,11,true),
            (5,12,true),(6,12,true),(8,12,true),
            (5,13,true),(7,13,true),(8,13,true),(9,13,true),
            (6,14,true),(7,14,true),(8,14,true),(9,14,true),
            (10,16,true),(11,16,true),(12,16,true),(13,16,true),
            (10,17,true),(11,17,true),(12,17,true),
        };
        foreach (var (ui, si, up) in votePairs)
            solutionVotes.Add(new SolutionVote
            {
                UserId = u[ui].Id, SolutionId = solutions[si].Id,
                IsUpvote = up, VoteDate = now.AddDays(-Random.Shared.Next(0, 20)),
            });

        context.SolutionVotes.AddRange(solutionVotes);
        context.SaveChanges();

        // ─────────────────────────────────────────────────────────────
        // 9. YORUMLAR (Comments)
        // ─────────────────────────────────────────────────────────────
        Comment C(int senderId, int solutionId, string text, int? parentId = null)
            => new() { SenderId = senderId, SolutionId = solutionId, Text = text,
                       ParentCommentId = parentId,
                       SendDate = now.AddDays(-Random.Shared.Next(0, 30)) };

        var comments = new List<Comment>
        {
            // Çözüm 0: Kaldırım beton çözümü
            C(u[1].Id, solutions[0].Id, "Bu çözümü belediye yetkilileriyle paylaştım. Altyapı müdürlüğü inceleme yapacakmış, umut verici."),
            C(u[4].Id, solutions[0].Id, "Maliyeti 15.000 TL değil, benim aldığım tekliflere göre 40.000-50.000 TL arasında. Daha gerçekçi bir hesaplama gerekli."),
            C(u[2].Id, solutions[0].Id, "40-50 bin TL'lik tahmini doğru olabilir, fiyatlar çok arttı. Bölünmüş ihale ile daha uygun olabilir.", 1), // reply

            // Çözüm 3: Trafik ışıkları
            C(u[0].Id, solutions[3].Id, "EGO hattını aradım, 2 saat beklettiler, sonuçta 'kayıt alındı' dediler. Bakalım ne kadar sürer."),
            C(u[3].Id, solutions[3].Id, "Ben de aradım. Dediler ki ekip en geç 48 saat içinde gidecekmiş. Umarım tutarlar sözü."),
            C(u[1].Id, solutions[3].Id, "Güncelleme: sabah 10'da EGO ekibi geldi, saat 14:00'e kadar 3 ışık değiştirildi. Teşekkürler!", 3), // reply

            // Çözüm 7: .NET EF migration
            C(u[8].Id, solutions[7].Id, "bin ve obj silmek çalıştı! Global tools güncel değildi de. `dotnet tool update --global dotnet-ef` de yapmayı unutmayın."),
            C(u[9].Id, solutions[7].Id, "Ayrıca Package Manager Console'da `Update-Database` yerine terminal ile `dotnet ef database update` kullanın, farklı environment okuyor."),
            C(u[6].Id, solutions[7].Id, "Güzel ekleme. PMC ile CLI davranışı farklı olabiliyor, bunu çok kişi atlıyor.", 7), // reply

            // Çözüm 9: React Strict Mode
            C(u[7].Id, solutions[9].Id, "Mükemmel açıklama! Benim sorunum tam buymuş. Cleanup ekleyince çift çalışma görünür oldu ama production'da sorun yok."),
            C(u[8].Id, solutions[9].Id, "Dikkat: eğer useEffect içinde başka bir async call yapıyorsanız, cleanup'ta abort controller eklemeyi de unutmayın."),
            C(u[5].Id, solutions[9].Id, "```js\nconst controller = new AbortController();\nfetch(url, { signal: controller.signal });\nreturn () => controller.abort();\n```", 10), // reply

            // Çözüm 11: Alpine timezone
            C(u[6].Id, solutions[11].Id, "Mükemmel! Sadece bunu eklemek yeterli. Ben yıllarca bu sorunla uğraştım."),
            C(u[5].Id, solutions[11].Id, "Ek not: .NET NodaTime kütüphanesiyle timezone işlemlerini çok daha güvenli yönetebilirsiniz."),

            // Çözüm 13: Redis GitHub Actions
            C(u[5].Id, solutions[13].Id, "Tam olarak bu sorunla 3 gün uğraştım. Hostname sorunuymuş meğerse, localhost'la test edince geçiyor sandım CI'da da öyle çalışır diye."),
            C(u[7].Id, solutions[13].Id, "Sadece Redis değil, bütün service container'lar için bu geçerli. PostgreSQL, MySQL vs. hepsi servis adıyla bağlanılmalı."),

            // Çözüm 14: JWT
            C(u[8].Id, solutions[14].Id, "localStorage kullanıyordum, hemen değiştireceğim. Peki mobil uygulama için en iyi yöntem ne? Secure Storage kullanıyor muyuz?"),
            C(u[5].Id, solutions[14].Id, "Mobil'de platform Keychain/Keystore (iOS Keychain, Android Keystore) kullanın. HttpOnly cookie mobil için çalışmaz.", 15), // reply
            C(u[6].Id, solutions[14].Id, "Token Rotation için örnek bir middleware yazdım, GitHub'a koydim: github.com/example/jwt-rotation-example"),

            // Çözüm 16: Google Classroom
            C(u[14].Id, solutions[16].Id, "Google Workspace kurulumu yaptık, öğretmenlerin %80'i 1 günde öğrendi. Sınav günü platform çökünce Google Meet'e geçtik, sorunsuz oldu."),
            C(u[13].Id, solutions[16].Id, "Sınav güvenliği için Google Forms yetersiz. Proctoring özelliği için Moodle + Proctorio eklentisi düşünülebilir."),

            // Çözüm 17: Emekli öğretmen havuzu
            C(u[14].Id, solutions[17].Id, "Bu fikri mükemmel buldum! Hemen İl Müdürlüğü'ne yazılı başvuru yaptık. Bir emekli matematik öğretmeni de gönüllü oldu!"),
            C(u[12].Id, solutions[17].Id, "Üniversite-okul ortaklığı çok değerli. Biz de Hacettepe Matematik Bölümü'yle görüştük, olumlu yanıt aldık."),
        };

        context.Comments.AddRange(comments);
        context.SaveChanges();

        // ─────────────────────────────────────────────────────────────
        // 10. RAPORLAR (Reports)
        // ─────────────────────────────────────────────────────────────
        var reports = new List<Report>
        {
            new() { ReporterUserId = u[3].Id, TargetType = "Problem", TargetId = problems[9].Id,
                    Reason = "Yanlış kategoride, içerik alakasız görünüyor.",
                    ReportDate = now.AddDays(-5), IsResolved = false },
            new() { ReporterUserId = u[7].Id, TargetType = "Solution", TargetId = solutions[4].Id,
                    Reason = "Çözüm yanlış bilgi içeriyor, yanıltıcı olabilir.",
                    ReportDate = now.AddDays(-3), IsResolved = false },
            new() { ReporterUserId = u[1].Id, TargetType = "Comment", TargetId = comments[1].Id,
                    Reason = "Yorum hakaret içeriyor.",
                    ReportDate = now.AddDays(-7), IsResolved = true },
            new() { ReporterUserId = u[9].Id, TargetType = "Problem", TargetId = problems[22].Id,
                    Reason = "Tekrarlanan içerik, daha önce açılmış bir sorunun kopyası.",
                    ReportDate = now.AddDays(-2), IsResolved = false },
            new() { ReporterUserId = u[4].Id, TargetType = "User", TargetId = u[11].Id,
                    Reason = "Spam içerik paylaşıyor, birden fazla hesap gibi davranıyor.",
                    ReportDate = now.AddDays(-1), IsResolved = false },
        };

        context.Reports.AddRange(reports);
        context.SaveChanges();

        // ─────────────────────────────────────────────────────────────
        // 11. GERİ BİLDİRİMLER (Feedbacks)
        // ─────────────────────────────────────────────────────────────
        var feedbacks = new List<Feedback>
        {
            new() { UserId = u[0].Id, Title = "Harita görünümü eklenebilir mi?",
                    Message = "Sorunları bir harita üzerinde görebilmek çok kullanışlı olur. "
                            + "Özellikle şehir bazlı problemleri bölgesel olarak görselleştirmek, "
                            + "yoğun bölgeleri tespit etmek açısından değerli.",
                    IsRead = false, SendDate = now.AddDays(-8) },
            new() { UserId = u[5].Id, Title = "Kod bloğu formatlaması çok iyi",
                    Message = "Yazılım topluluğunda markdown desteği ve özellikle kod bloğu "
                            + "gösterimi gerçekten harika. Syntax highlighting eklenebilirse mükemmel olur.",
                    IsRead = true,  SendDate = now.AddDays(-15) },
            new() { UserId = u[10].Id, Title = "Bildirim sistemi geç çalışıyor",
                    Message = "Çözümüme yorum yapıldığında bildirim 10-15 dakika sonra geliyor. "
                            + "Anlık bildirim alabilmek için push notification özelliği eklenebilir mi?",
                    IsRead = false, SendDate = now.AddDays(-3) },
            new() { UserId = u[2].Id, Title = "Sorun çözüldü olarak işaretleme süreci karmaşık",
                    Message = "Bir sorun çözüldüğünde bunu işaretlemek için çok fazla tıklama gerekiyor. "
                            + "Sorun sayfasında daha belirgin bir 'Çözüldü' butonu olsaydı çok daha kullanışlı olurdu.",
                    IsRead = true,  SendDate = now.AddDays(-20) },
            new() { UserId = u[7].Id, Title = "Mobil uygulama ne zaman çıkıyor?",
                    Message = "Web arayüzü çok iyi ama mobil uygulama yok mu? Anlık bildirimler "
                            + "ve kolay içerik paylaşımı için mobil uygulama şart. "
                            + "React Native ile geliştirmeyi düşünüyor musunuz?",
                    IsRead = false, SendDate = now.AddDays(-6) },
            new() { UserId = u[11].Id, Title = "Etiket/tag sistemi eksik",
                    Message = "Sorunlara özel etiketler eklenebilse arama çok daha kolay olur. "
                            + "Örneğin 'acil', 'bütçe-gerektiriyor', 'gönüllü-destekli' gibi etiketler.",
                    IsRead = false, SendDate = now.AddDays(-1) },
            new() { UserId = u[3].Id, Title = "Paylaşım butonu çalışmıyor (iOS Safari)",
                    Message = "Safari tarayıcısında paylaşım butonu tıklandığında hiçbir şey olmuyor. "
                            + "Chrome'da sorun yok. iOS 16.4 kullanıyorum.",
                    IsRead = true, SendDate = now.AddDays(-11) },
            new() { UserId = u[14].Id, Title = "Platform çok kullanışlı, teşekkürler",
                    Message = "Eğitim sektöründeki sorunları artık daha sistematik paylaşabiliyoruz. "
                            + "Özellikle çözüm önerilerinin uzman onayına tabi olması "
                            + "içeriğin kalitesini artırıyor. Başarılar!",
                    IsRead = true, SendDate = now.AddDays(-25) },
        };

        context.Feedbacks.AddRange(feedbacks);
        context.SaveChanges();
    }
}
