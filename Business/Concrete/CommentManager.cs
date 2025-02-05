using Business.Abstract;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class CommentManager : ICommentService
{
	private readonly ICommentDal _commentDal = new EfCommentDal();

	public IDataResult<Comment?> GetById(int id)
	{
		return new SuccessDataResult<Comment?>(_commentDal.Get(comment => comment.Id == id));
	}

	public IDataResult<List<Comment>> GetAll()
	{
		return new SuccessDataResult<List<Comment>>(_commentDal.GetAll());
	}

	public IDataResult<List<CommentDetailDto>> GetByParentCommentId(int parentCommentId)
	{
		return new SuccessDataResult<List<CommentDetailDto>>(_commentDal.GetCommentDetails(comment => comment.ParentCommentId == parentCommentId));
	}

	public IDataResult<List<CommentDetailDto>> GetBySolution(int solutionId)
	{
		return new SuccessDataResult<List<CommentDetailDto>>(_commentDal.GetCommentDetails(comment => comment.SolutionId == solutionId));
	}

	public IResult Add(Comment comment)
	{
		comment.SendDate = DateTime.Now;
		_commentDal.Add(comment);
		return new SuccessResult(Messages.CommentAdded);
	}

	public IResult Update(Comment comment)
	{
		_commentDal.Update(comment);
		return new SuccessResult(Messages.CommentUpdated);
	}

	public IResult Delete(int id)
	{
		_commentDal.Delete(_commentDal.Get(comment => comment.Id == id));
		return new SuccessResult(Messages.CommentDeleted);
	}
}