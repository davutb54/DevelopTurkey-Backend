using Business.Abstract;
using Business.Models;
using Core.Utilities.Context;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class SolutionVoteManager : ISolutionVoteService
{
    private readonly ISolutionVoteDal _solutionVoteDal;
    private readonly ILogService _logService;
    private readonly IClientContext _clientContext;
    private readonly IWorkflowEventBus _eventBus;

    public SolutionVoteManager(ISolutionVoteDal solutionVoteDal, ILogService logService, IClientContext clientContext, IWorkflowEventBus eventBus)
    {
        _solutionVoteDal = solutionVoteDal;
        _logService = logService;
        _clientContext = clientContext;
        _eventBus = eventBus;
    }

    public IDataResult<int> GetSolutionVoteCount(int solutionId)
    {
        var votes = _solutionVoteDal.GetAll(v => v.SolutionId == solutionId);

        var upvotes = votes.Count(v => v.IsUpvote);
        var downvotes = votes.Count(v => !v.IsUpvote);

        return new SuccessDataResult<int>(upvotes - downvotes);
    }

    public IResult Vote(int solutionId, bool isUpvote)
    {
        var userId = _clientContext.GetUserId() ?? 0;
        var existingVote = _solutionVoteDal.Get(v => v.UserId == userId && v.SolutionId == solutionId);

        if (existingVote == null)
        {
            var newVote = new SolutionVote
            {
                UserId = userId,
                SolutionId = solutionId,
                IsUpvote = isUpvote,
                VoteDate = DateTime.Now
            };
            _solutionVoteDal.Add(newVote);

            _ = _eventBus.PublishAsync(isUpvote ? "solution.upvoted" : "solution.downvoted", new RuleContext
            {
                SystemUserId = userId,
                Metadata = new Dictionary<string, object?>
                {
                    ["SolutionId"] = solutionId,
                    ["IsUpvote"] = isUpvote
                }
            });

            return new SuccessResult("Oy verildi.");
        }

        if (existingVote.IsUpvote == isUpvote)
        {
            _solutionVoteDal.Delete(existingVote);

            _ = _eventBus.PublishAsync("solution.vote_retracted", new RuleContext
            {
                SystemUserId = userId,
                Metadata = new Dictionary<string, object?>
                {
                    ["SolutionId"] = solutionId,
                    ["WasUpvote"] = isUpvote
                }
            });

            return new SuccessResult("Oy geri alındı.");
        }

        existingVote.IsUpvote = isUpvote;
        existingVote.VoteDate = DateTime.Now;
        _solutionVoteDal.Update(existingVote);

        _ = _eventBus.PublishAsync("solution.vote_changed", new RuleContext
        {
            SystemUserId = userId,
            Metadata = new Dictionary<string, object?>
            {
                ["SolutionId"] = solutionId,
                ["IsUpvote"] = isUpvote
            }
        });

        return new SuccessResult("Oy güncellendi.");
    }
}