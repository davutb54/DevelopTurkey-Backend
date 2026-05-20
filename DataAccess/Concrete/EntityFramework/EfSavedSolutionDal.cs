using Core.DataAccess.EntityFramework;
using Core.Entities.Concrete;
using DataAccess.Abstract;

namespace DataAccess.Concrete.EntityFramework;

public class EfSavedSolutionDal : EfEntityRepositoryBase<SavedSolution, DevelopTurkeyContext>, ISavedSolutionDal
{
    public List<Entities.DTOs.SolutionDetailDto> GetSavedSolutionDetails(int userId)
    {
        using (var context = new DevelopTurkeyContext())
        {
            var result = from ss in context.SavedSolutions
                         where ss.UserId == userId
                         join sol in context.Solutions on ss.SolutionId equals sol.Id
                         join u in context.Users on sol.SenderId equals u.Id
                         join p in context.Problems on sol.ProblemId equals p.Id
                         where !sol.IsDeleted
                         select new Entities.DTOs.SolutionDetailDto
                         {
                             Id = sol.Id,
                             SenderId = sol.SenderId,
                             ProblemId = sol.ProblemId,
                             Title = sol.Title,
                             Description = sol.Description,
                             SenderUsername = u.UserName,
                             SenderIsExpert = u.IsExpert,
                             SenderIsOfficial = u.IsOfficial,
                             SenderImageUrl = u.ProfileImageUrl,
                             ProblemName = p.Title,
                             IsHighlighted = sol.IsHighlighted,
                             IsReported = sol.IsReported,
                             IsDeleted = sol.IsDeleted,
                             SendDate = sol.SendDate,
                             ExpertApprovalStatus = sol.ExpertApprovalStatus,
                             InstitutionId = sol.InstitutionId,
                             VoteCount = context.SolutionVotes.Count(v => v.SolutionId == sol.Id && v.IsUpvote == true) - 
                                         context.SolutionVotes.Count(v => v.SolutionId == sol.Id && v.IsUpvote == false)
                         };
            return result.ToList();
        }
    }
}
