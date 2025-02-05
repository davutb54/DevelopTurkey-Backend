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
						 SenderUsername = u.UserName,
						 CityName = ConstantData.GetCity(p.CityCode).Text,
						 SenderIsExpert = u.IsExpert,
						 SendDate = p.SendDate
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
				TopicId = p.TopicId,
				SenderId = p.SenderId,
				SenderUsername = u.UserName,
				CityName = ConstantData.GetCity(p.CityCode).Text
			};
		return result.Where(filter).SingleOrDefault();
	}
}