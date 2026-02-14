using System.Linq.Expressions;
using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Concrete.EntityFramework;

public class EfSolutionDal : EfEntityRepositoryBase<Solution, DevelopTurkeyContext>, ISolutionDal
{
    public List<SolutionDetailDto> GetSolutions(Expression<Func<SolutionDetailDto, bool>>? filter = null)
    {
        using DevelopTurkeyContext context = new DevelopTurkeyContext();
        var result = from s in context.Solutions
                     join u in context.Users on s.SenderId equals u.Id
                     join p in context.Problems on s.ProblemId equals p.Id
                     where s.IsDeleted == false
                     select new SolutionDetailDto
                     {
                         Id = s.Id,
                         Title = s.Title,
                         Description = s.Description,
                         ProblemId = s.ProblemId,
                         SenderId = s.SenderId,
                         SenderUsername = u.UserName,
                         IsHighlighted = s.IsHighlighted,
                         IsReported = s.IsReported,
                         IsDeleted = s.IsDeleted,
                         ProblemName = p.Title,
                         SenderIsOfficial = u.IsOfficial,
                         SenderIsExpert = u.IsExpert,
                         SendDate = s.SendDate,
                         VoteCount = context.SolutionVotes.Count(v => v.SolutionId == s.Id && v.IsUpvote) - context.SolutionVotes.Count(v => v.SolutionId == s.Id && !v.IsUpvote)
                     };
        return filter == null ? result.ToList() : result.Where(filter).ToList();
    }
}