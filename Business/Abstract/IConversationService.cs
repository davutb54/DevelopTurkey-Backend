using Core.Utilities.Results;
using Entities.DTOs;

namespace Business.Abstract;

public interface IConversationService
{
    IDataResult<ConversationDetailDto> StartDirect(int callerUserId, int callerInstitutionId, StartDirectConversationDto dto);
    IDataResult<ConversationDetailDto> StartGroup(int callerUserId, int callerInstitutionId, StartGroupConversationDto dto);
    IDataResult<ConversationDetailDto> StartSupport(int callerUserId, int callerInstitutionId, StartSupportConversationDto dto);
    IDataResult<List<ConversationSummaryDto>> GetMyConversations(int callerUserId, int callerInstitutionId);
    IDataResult<List<ConversationSummaryDto>> GetPool(int staffUserId, int institutionId);
    IDataResult<ConversationDetailDto> GetDetail(int conversationId, int callerUserId);
    IResult AddParticipant(int conversationId, int callerUserId, AddParticipantDto dto);
    IResult RemoveParticipant(int conversationId, int callerUserId, int targetUserId);
    IResult Claim(int conversationId, int staffUserId);
    IResult CloseConversation(int conversationId, int callerUserId);
    IResult DeleteConversation(int conversationId, int callerUserId);
    IResult UpdateTitle(int conversationId, int callerUserId, string newTitle);

    /// <summary>StartGroup/StartSupport/AddParticipant sonrası eklenen katılımcı ID listesini döner (SignalR push için).</summary>
    List<int> GetParticipantUserIds(int conversationId);
}
