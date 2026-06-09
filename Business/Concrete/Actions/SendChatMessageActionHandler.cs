using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Entities.DTOs;

namespace Business.Concrete.Actions;

/// <summary>send_chat_message — Mevcut bir konuşmaya mesaj gönderir.</summary>
public class SendChatMessageActionHandler : IWorkflowActionHandler
{
    private readonly IMessageService _messageService;

    public string ActionCode => "send_chat_message";

    public SendChatMessageActionHandler(IMessageService messageService)
    {
        _messageService = messageService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "conversationId", ActionCode, errors);
        WorkflowParameterResolver.RequireParam(parameters, "body",           ActionCode, errors);
        return errors;
    }

    public Task<IDataResult<object?>> ExecuteAsync(Dictionary<string, string> parameters, RuleContext context)
    {
        var convIdStr = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("conversationId"), context);
        if (!int.TryParse(convIdStr, out var conversationId))
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"send_chat_message: 'conversationId' geçerli bir tam sayı değil: '{convIdStr}'."));

        var body = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("body"), context);

        var senderUserId = WorkflowParameterResolver.ResolveUserId(parameters, context, "senderType", "customSenderId");

        var result = _messageService.Send(conversationId, senderUserId, new SendMessageDto { Body = body });

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { conversationId, senderUserId, body }, "Sohbet mesajı gönderildi.")
            : new ErrorDataResult<object?>(null, $"Mesaj gönderilemedi: {result.Message}"));
    }
}
