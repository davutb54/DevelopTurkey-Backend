using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using System.Linq.Expressions;
using Core.Entities.Constants;

namespace DataAccess.Concrete.EntityFramework;

public class EfProblemDal : EfEntityRepositoryBase<Problem, DevelopTurkeyContext>, IProblemDal
{
	public List<ProblemDetailDto> GetProblemsDetails(Expression<Func<ProblemDetailDto, bool>>? filter = null)
	{
		using DevelopTurkeyContext context = new DevelopTurkeyContext();
		var result = from p in context.Problems
					 join t in context.Topics
						 on p.TopicId equals t.Id
					 join u in context.Users on p.SenderId equals u.Id
                     where p.IsDeleted == false
					 select new ProblemDetailDto
					 {
						 Id = p.Id,
						 Title = p.Title,
						 Description = p.Description,
						 CityCode = p.CityCode,
						 TopicName = t.Name,
						 IsHighlighted = p.IsHighlighted,
						 IsReported = p.IsReported,
						 IsDeleted = p.IsDeleted,
						 TopicId = p.TopicId,
						 SenderId = p.SenderId,
                         ImageUrl = p.ImageUrl,
                         SenderUsername = u.UserName,
						 CityName = ConstantData.GetCity(p.CityCode).Text,
						 SenderIsExpert = u.IsExpert,
						 SendDate = p.SendDate,
                         ViewCount = p.ViewCount,
                         SolutionCount = context.Solutions.Count(s => s.ProblemId == p.Id),
                         SenderIsOfficial = u.IsOfficial,
                         SenderImageUrl = u.ProfileImageUrl,
                         IsResolvedByExpert = context.Solutions.Any(s => s.ExpertApprovalStatus == 1),
                         IsResolved = p.IsResolved,
                         InstitutionId = p.InstitutionId,
                     };
		return filter == null ? result.ToList() : result.Where(filter).ToList();
	}

	public ProblemDetailDto GetProblemDetail(Expression<Func<ProblemDetailDto, bool>> filter)
	{
		using DevelopTurkeyContext context = new DevelopTurkeyContext();
		var result = from p in context.Problems
			join t in context.Topics
				on p.TopicId equals t.Id
			join u in context.Users on p.SenderId equals u.Id
			where p.IsDeleted == false
			select new ProblemDetailDto
			{
				Id = p.Id,
				Title = p.Title,
				Description = p.Description,
				CityCode = p.CityCode,
				TopicName = t.Name,
				IsHighlighted = p.IsHighlighted,
				IsReported = p.IsReported,
				IsDeleted = p.IsDeleted,
                ImageUrl = p.ImageUrl,
                TopicId = p.TopicId,
				SenderId = p.SenderId,
				SenderUsername = u.UserName,
				CityName = ConstantData.GetCity(p.CityCode).Text,
                ViewCount = p.ViewCount,
                SolutionCount = context.Solutions.Count(s => s.ProblemId == p.Id),
                SenderIsExpert = u.IsExpert,
                SendDate = p.SendDate,
                SenderIsOfficial = u.IsOfficial,
                SenderImageUrl = u.ProfileImageUrl,
                IsResolvedByExpert = context.Solutions.Any(s => s.ExpertApprovalStatus == 1),
                IsResolved = p.IsResolved,
                InstitutionId = p.InstitutionId,
            };
		return result.Where(filter).SingleOrDefault();
	}

    public List<ProblemDetailDto> GetListByFilter(ProblemFilterDto filter)
    {
        using (var context = new DevelopTurkeyContext())
        {
            var query = from p in context.Problems
                        join u in context.Users on p.SenderId equals u.Id
                        join t in context.Topics on p.TopicId equals t.Id
                        where p.IsDeleted == false
                        select new { p, u, t };

            

            if (filter.CityCode.HasValue)
            {
                query = query.Where(x => x.p.CityCode == filter.CityCode.Value);
            }

            if (filter.TopicId.HasValue)
            {
                query = query.Where(x => x.p.TopicId == filter.TopicId.Value);
            }

            if (!string.IsNullOrEmpty(filter.SearchText))
            {
                string text = filter.SearchText.ToLower();
                query = query.Where(x => x.p.Title.ToLower().Contains(text) ||
                                         x.p.Description.ToLower().Contains(text));
            }

            
            var result = query.Select(x => new ProblemDetailDto
            {
                Id = x.p.Id,
                Title = x.p.Title,
                Description = x.p.Description,
                CityCode = x.p.CityCode,
                CityName = ConstantData.GetCity(x.p.CityCode).Text,
                TopicId = x.p.TopicId,
                TopicName = x.t.Name,
                SenderId = x.p.SenderId,
                SenderUsername = x.u.UserName,
                SenderIsExpert = x.u.IsExpert,
                SenderIsOfficial = x.u.IsOfficial,
                ImageUrl = x.p.ImageUrl,
                IsHighlighted = x.p.IsHighlighted,
                IsReported = x.p.IsReported,
                IsDeleted = x.p.IsDeleted,
                SendDate = x.p.SendDate,
                ViewCount = x.p.ViewCount,
                SolutionCount = context.Solutions.Count(s => s.ProblemId == x.p.Id),
                SenderImageUrl = x.u.ProfileImageUrl,
                IsResolvedByExpert = context.Solutions.Any(s => s.ExpertApprovalStatus == 1),
                IsResolved = x.p.IsResolved,
                InstitutionId = x.p.InstitutionId,
            });

            return result.OrderByDescending(p => p.ViewCount).ToList();
        }
    }

}