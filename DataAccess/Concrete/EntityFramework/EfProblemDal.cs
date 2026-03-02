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

        var query = from p in context.Problems
                    join u in context.Users on p.SenderId equals u.Id
                    // join i in context.Institutions on p.InstitutionId equals i.Id
                    where p.IsDeleted == false
                    select new ProblemDetailDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Description = p.Description,
                        CityCode = p.CityCode,
                        IsHighlighted = p.IsHighlighted,
                        IsReported = p.IsReported,
                        IsDeleted = p.IsDeleted,
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
                        IsResolvedByExpert = context.Solutions.Any(s => s.ProblemId == p.Id && s.ExpertApprovalStatus == 1),
                        IsResolved = p.IsResolved,
                        InstitutionId = p.InstitutionId,

                        Topics = (from pt in context.ProblemTopics
                                  join t in context.Topics on pt.TopicId equals t.Id
                                  where pt.ProblemId == p.Id && t.Status == true
                                  select new TopicDto
                                  {
                                      Id = t.Id,
                                      Name = t.Name
                                  }).ToList()
                    };

        return filter == null ? query.ToList() : query.Where(filter).ToList();
    }

    public ProblemDetailDto GetProblemDetail(Expression<Func<ProblemDetailDto, bool>> filter)
    {
        using DevelopTurkeyContext context = new DevelopTurkeyContext();
        var query = from p in context.Problems
                    join u in context.Users on p.SenderId equals u.Id
                    where p.IsDeleted == false
                    select new ProblemDetailDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Description = p.Description,
                        CityCode = p.CityCode,
                        IsHighlighted = p.IsHighlighted,
                        IsReported = p.IsReported,
                        IsDeleted = p.IsDeleted,
                        ImageUrl = p.ImageUrl,
                        SenderId = p.SenderId,
                        SenderUsername = u.UserName,
                        CityName = ConstantData.GetCity(p.CityCode).Text,
                        ViewCount = p.ViewCount,
                        SolutionCount = context.Solutions.Count(s => s.ProblemId == p.Id),
                        SenderIsExpert = u.IsExpert,
                        SendDate = p.SendDate,
                        SenderIsOfficial = u.IsOfficial,
                        SenderImageUrl = u.ProfileImageUrl,
                        IsResolvedByExpert = context.Solutions.Any(s => s.ProblemId == p.Id && s.ExpertApprovalStatus == 1),
                        IsResolved = p.IsResolved,
                        InstitutionId = p.InstitutionId,

                        Topics = (from pt in context.ProblemTopics
                                  join t in context.Topics on pt.TopicId equals t.Id
                                  where pt.ProblemId == p.Id && t.Status == true
                                  select new TopicDto
                                  {
                                      Id = t.Id,
                                      Name = t.Name
                                  }).ToList()
                    };

        return query.Where(filter).SingleOrDefault();
    }

    public List<ProblemDetailDto> GetListByFilter(ProblemFilterDto filter)
    {
        using (var context = new DevelopTurkeyContext())
        {
            var query = from p in context.Problems
                        join u in context.Users on p.SenderId equals u.Id
                        where p.IsDeleted == false
                        select new { p, u };

            if (!string.IsNullOrEmpty(filter.SearchText))
            {
                string text = filter.SearchText.ToLower();
                query = query.Where(x => x.p.Title.ToLower().Contains(text) ||
                                         x.p.Description.ToLower().Contains(text));
            }

            if (filter.CityCode.HasValue && filter.CityCode.Value > 0)
            {
                query = query.Where(x => x.p.CityCode == filter.CityCode.Value);
            }

            var result = query.Select(x => new ProblemDetailDto
            {
                Id = x.p.Id,
                Title = x.p.Title,
                Description = x.p.Description,
                CityCode = x.p.CityCode,
                CityName = ConstantData.GetCity(x.p.CityCode).Text,
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
                IsResolvedByExpert = context.Solutions.Any(s => s.ProblemId == x.p.Id && s.ExpertApprovalStatus == 1),
                IsResolved = x.p.IsResolved,
                InstitutionId = x.p.InstitutionId,

                Topics = (from pt in context.ProblemTopics
                          join t in context.Topics on pt.TopicId equals t.Id
                          where pt.ProblemId == x.p.Id && t.Status == true
                          select new TopicDto
                          {
                              Id = t.Id,
                              Name = t.Name
                          }).ToList()
            });

            var list = result.OrderByDescending(p => p.ViewCount).ToList();

            if (filter.TopicId.HasValue && filter.TopicId.Value > 0)
            {
                list = list.Where(p => p.Topics.Any(t => t.Id == filter.TopicId.Value)).ToList();
            }

            return list;
        }
    }
}