using System.Linq.Expressions;
using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Concrete.EntityFramework;

public class EfOfficialResponseDal : EfEntityRepositoryBase<OfficialResponse, DevelopTurkeyContext>, IOfficialResponseDal
{
    public List<OfficialResponseDto> GetDetails(Expression<Func<OfficialResponseDto, bool>>? filter = null)
    {
        using DevelopTurkeyContext context = new DevelopTurkeyContext();
        var query = from r in context.OfficialResponses
                    join u in context.Users on r.AuthorUserId equals u.Id
                    select new OfficialResponseDto
                    {
                        Id = r.Id,
                        ProblemId = r.ProblemId,
                        AuthorUserId = r.AuthorUserId,
                        AuthorUsername = u.UserName,
                        AuthorImageUrl = u.ProfileImageUrl,
                        Body = r.Body,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    };
        return filter == null ? query.ToList() : query.Where(filter).ToList();
    }
}
