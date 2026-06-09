using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class MessageManager : IMessageService
{
    private readonly IMessageDal _messageDal;
    private readonly IConversationParticipantDal _participantDal;
    private readonly IUserDal _userDal;

    public MessageManager(
        IMessageDal messageDal,
        IConversationParticipantDal participantDal,
        IUserDal userDal)
    {
        _messageDal     = messageDal;
        _participantDal = participantDal;
        _userDal        = userDal;
    }

    public IDataResult<MessageDto> Send(int conversationId, int callerUserId, SendMessageDto dto)
    {
        if (!IsParticipant(conversationId, callerUserId))
            return new ErrorDataResult<MessageDto>(null!, "Bu konuşmada mesaj gönderme yetkiniz yok.");

        if (string.IsNullOrWhiteSpace(dto.Body))
            return new ErrorDataResult<MessageDto>(null!, "Mesaj boş olamaz.");

        if (dto.Body.Length > 4000)
            return new ErrorDataResult<MessageDto>(null!, "Mesaj 4000 karakteri geçemez.");

        var msg = new Message
        {
            ConversationId = conversationId,
            SenderUserId   = callerUserId,
            Body           = dto.Body.Trim(),
        };
        _messageDal.Add(msg);

        return new SuccessDataResult<MessageDto>(ToDto(msg), "Mesaj gönderildi.");
    }

    public IDataResult<MessagePageDto> GetHistory(int conversationId, int callerUserId, int pageSize, int? beforeMessageId)
    {
        if (!IsParticipant(conversationId, callerUserId))
            return new ErrorDataResult<MessagePageDto>(null!, "Bu konuşmaya erişim yetkiniz yok.");

        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var query = _messageDal.GetAll(m => m.ConversationId == conversationId && !m.IsDeleted);

        if (beforeMessageId.HasValue)
            query = query.Where(m => m.Id < beforeMessageId.Value).ToList();

        var items = query
            .OrderByDescending(m => m.Id)
            .Take(pageSize + 1)
            .ToList();

        bool hasMore = items.Count > pageSize;
        if (hasMore) items = items.Take(pageSize).ToList();

        items.Reverse();

        return new SuccessDataResult<MessagePageDto>(new MessagePageDto
        {
            Items   = items.Select(ToDto).ToList(),
            HasMore = hasMore,
        });
    }

    public IResult MarkRead(int conversationId, int callerUserId, MarkReadDto dto)
    {
        var participant = _participantDal.Get(p => p.ConversationId == conversationId && p.UserId == callerUserId);
        if (participant == null)
            return new ErrorResult("Bu konuşmada değilsiniz.");

        participant.LastReadMessageId = dto.LastReadMessageId;
        _participantDal.Update(participant);
        return new SuccessResult("Okundu olarak işaretlendi.");
    }

    public IResult DeleteMessage(int messageId, int callerUserId)
    {
        var msg = _messageDal.Get(m => m.Id == messageId);
        if (msg == null)
            return new ErrorResult("Mesaj bulunamadı.");

        if (msg.SenderUserId != callerUserId)
            return new ErrorResult("Sadece kendi mesajınızı silebilirsiniz.");

        msg.IsDeleted = true;
        _messageDal.Update(msg);
        return new SuccessResult("Mesaj silindi.");
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private bool IsParticipant(int conversationId, int userId)
        => _participantDal.Get(p => p.ConversationId == conversationId && p.UserId == userId) != null;

    private MessageDto ToDto(Message m)
    {
        var user = _userDal.Get(u => u.Id == m.SenderUserId);
        return new MessageDto
        {
            Id             = m.Id,
            ConversationId = m.ConversationId,
            SenderUserId   = m.SenderUserId,
            SenderUsername = user?.UserName ?? string.Empty,
            Body           = m.IsDeleted ? "[Bu mesaj silindi]" : m.Body,
            CreatedAt      = m.CreatedAt,
            IsDeleted      = m.IsDeleted,
        };
    }
}
