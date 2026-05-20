using Core.Utilities.Results;

namespace Business.Abstract;

public interface IMentionService
{
    /// <summary>
    /// Metin içerisindeki @mention'ları analiz eder ve ilgili kullanıcılara bildirim gönderir.
    /// </summary>
    /// <param name="content">Analiz edilecek metin içeriği</param>
    /// <param name="senderId">Etiketlemeyi yapan kullanıcı ID'si</param>
    /// <param name="institutionId">Kurum ID'si (Sadece bu kurumdaki kullanıcılar etiketlenebilir)</param>
    /// <param name="referenceLink">Bildirime tıklandığında gidilecek link</param>
    /// <param name="sourceTitle">Etiketlemenin yapıldığı içeriğin başlığı (Opsiyonel)</param>
    /// <returns></returns>
    IResult ProcessMentions(string content, int senderId, int institutionId, string referenceLink, string sourceTitle = "");
}
