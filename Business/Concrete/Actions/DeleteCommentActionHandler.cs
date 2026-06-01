using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;

namespace Business.Concrete.Actions;

/// <summary>delete_comment — Yorum silme action'ı.</summary>
public class DeleteCommentActionHandler : IWorkflowActionHandler
{
    private readonly ICommentService _commentService;

    public string ActionCode => "delete_comment";

    public DeleteCommentActionHandler(ICommentService commentService)
    {
        _commentService = commentService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
        => Array.Empty<string>(); // commentTarget isteğe bağlı; context_comment varsayılan

    public Task<IDataResult<object?>> ExecuteAsync(Dictionary<string, string> parameters, RuleContext context)
    {
        var commentTarget = parameters.GetValueOrDefault("commentTarget") ?? "context_comment";
        int? commentId = commentTarget == "custom"
            && int.TryParse(
                WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("customCommentId"), context),
                out var cid)
            ? cid
            : context.CommentId;

        if (commentId == null)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "delete_comment: yorum ID çözülemedi."));

        var reason = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("reason"), context);
        var result = _commentService.Delete(commentId.Value);

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { commentId, reason }, "Yorum silindi.")
            : new ErrorDataResult<object?>(null, $"Yorum silinemedi: {result.Message}"));
    }
}
