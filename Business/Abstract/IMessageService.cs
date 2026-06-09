using Core.Utilities.Results;
using Entities.DTOs;

namespace Business.Abstract;

public interface IMessageService
{
    IDataResult<MessageDto> Send(int conversationId, int callerUserId, SendMessageDto dto);
    IDataResult<MessagePageDto> GetHistory(int conversationId, int callerUserId, int pageSize, int? beforeMessageId);
    IResult MarkRead(int conversationId, int callerUserId, MarkReadDto dto);
    IResult DeleteMessage(int messageId, int callerUserId);
}
