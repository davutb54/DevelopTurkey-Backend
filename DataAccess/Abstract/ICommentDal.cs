using System.Linq.Expressions;
using Core.DataAccess;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Abstract;

public interface ICommentDal : IEntityRepository<Comment>
{
	List<CommentDetailDto> GetCommentDetails(Expression<Func<CommentDetailDto, bool>>? filter = null);
}