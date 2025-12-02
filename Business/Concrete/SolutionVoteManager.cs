using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class SolutionVoteManager : ISolutionVoteService
{
    private readonly ISolutionVoteDal _solutionVoteDal;
    private readonly ILogDal _logDal;

    public SolutionVoteManager(ISolutionVoteDal solutionVoteDal, ILogDal logDal)
    {
        _solutionVoteDal = solutionVoteDal;
        _logDal = logDal;
    }

    public IDataResult<int> GetSolutionVoteCount(int solutionId)
    {
        var votes = _solutionVoteDal.GetAll(v => v.SolutionId == solutionId);

        var upvotes = votes.Count(v => v.IsUpvote);
        var downvotes = votes.Count(v => !v.IsUpvote);

        return new SuccessDataResult<int>(upvotes - downvotes);
    }

    public IResult Vote(int userId, int solutionId, bool isUpvote)
    {
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
            
            _logDal.Add(new Log
            {
                CreationDate = DateTime.Now,
                Message = $"Kullanıcı {userId} çözüm {solutionId} için {(isUpvote ? "upvote" : "downvote")} verdi.",
                Type = "Vote,Info"
            });
            return new SuccessResult("Oy verildi.");
        }

        if (existingVote.IsUpvote == isUpvote)
        {
            _solutionVoteDal.Delete(existingVote);

            _logDal.Add(new Log
            {
                CreationDate = DateTime.Now,
                Message = $"Kullanıcı {userId} çözüm {solutionId} için {(isUpvote ? "upvote" : "downvote")} oyunu geri aldı.",
                Type = "Vote,Info"
            });
            return new SuccessResult("Oy geri alındı.");
        }

        existingVote.IsUpvote = isUpvote;
        existingVote.VoteDate = DateTime.Now;
        _solutionVoteDal.Update(existingVote);

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Kullanıcı {userId} çözüm {solutionId} için oyunu {(isUpvote ? "upvote" : "downvote")} olarak güncelledi.",
            Type = "Vote,Info"
        });
        return new SuccessResult("Oy güncellendi.");
    }
}