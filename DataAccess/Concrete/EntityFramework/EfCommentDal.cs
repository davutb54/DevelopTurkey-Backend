using System.Linq.Expressions;
using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Concrete.EntityFramework;

public class EfCommentDal : EfEntityRepositoryBase<Comment, DevelopTurkeyContext>, ICommentDal
{
	public List<CommentDetailDto> GetCommentDetails(Expression<Func<CommentDetailDto, bool>>? filter = null)
	{
		using var context = new DevelopTurkeyContext();
		var result = from comment in context.Comments
					 join user in context.Users on comment.SenderId equals user.Id
					 select new CommentDetailDto
					 {
						 Id = comment.Id,
						 ParentCommentId = comment.ParentCommentId,
						 SenderId = comment.SenderId,
						 SenderUsername = user.UserName,
						 Text = comment.Text,
						 SolutionId = comment.SolutionId,
						 SenderIsExpert = user.IsExpert,
                         SenderIsOfficial = user.IsOfficial,
                         SendDate = comment.SendDate
					 };
		return filter == null ? result.ToList() : result.Where(filter).ToList();
	}
}