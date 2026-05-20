using System.Text.RegularExpressions;
using Business.Abstract;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class MentionManager : IMentionService
{
    private readonly IUserDal _userDal;
    private readonly INotificationService _notificationService;
    private readonly IInstitutionFeatureService _featureService;

    public MentionManager(IUserDal userDal, INotificationService notificationService, IInstitutionFeatureService featureService)
    {
        _userDal = userDal;
        _notificationService = notificationService;
        _featureService = featureService;
    }

    public IResult ProcessMentions(string content, int senderId, int institutionId, string referenceLink, string sourceTitle = "")
    {
        if (string.IsNullOrWhiteSpace(content)) return new SuccessResult();

        // 1. Kurumda etiketleme özelliği açık mı?
        if (!_featureService.IsFeatureEnabled(institutionId, "Social.EnableMentions"))
        {
            return new SuccessResult("Etiketleme özelliği bu kurum için kapalı.");
        }

        // 2. Metin içerisindeki @username kalıplarını bul
        // Kurallar: @ işaretinden sonra en az 3 karakter, alphanumeric, nokta veya alt çizgi.
        // Öncesinde boşluk veya satır başı olmalı.
        var mentionRegex = new Regex(@"(?<=^|\s)@([a-zA-Z0-9._]{3,})", RegexOptions.Compiled);
        var matches = mentionRegex.Matches(content);

        if (matches.Count == 0) return new SuccessResult();

        // Benzersiz kullanıcı adlarını al
        var usernames = matches.Cast<Match>()
            .Select(m => m.Groups[1].Value.ToLower())
            .Distinct()
            .ToList();

        foreach (var username in usernames)
        {
            // 3. Kullanıcıyı kurum filtresiyle bul
            var mentionedUser = _userDal.Get(u => u.UserName.ToLower() == username && u.InstitutionId == institutionId);

            if (mentionedUser != null && mentionedUser.Id != senderId)
            {
                // 4. Kullanıcının etiketlenme bildirimleri açık mı?
                if (mentionedUser.MentionNotificationEnabled)
                {
                    try
                    {
                        var sender = _userDal.Get(u => u.Id == senderId);
                        var senderName = sender != null ? $"{sender.Name} {sender.Surname}" : "Birisi";

                        _notificationService.Add(new Notification
                        {
                            UserId = mentionedUser.Id,
                            Title = "Bir içerikte etiketlendin",
                            Message = $"{senderName} seni bir içerikte etiketledi: \"{sourceTitle}\"",
                            Type = "UserMentioned",
                            ReferenceLink = referenceLink
                        });
                    }
                    catch { /* Bildirim hatası ana akışı bozmasın */ }
                }
            }
        }

        return new SuccessResult();
    }
}
