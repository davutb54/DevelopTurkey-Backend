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
    private readonly ICommentDal _commentDal;
    private readonly ILogDal _logDal;

    public CommentManager(ICommentDal commentDal, ILogDal logDal)
    {
        _commentDal = commentDal;
        _logDal = logDal;
    }


    public IDataResult<Comment?> GetById(int id)
    {
        return new SuccessDataResult<Comment?>(_commentDal.Get(comment => comment.Id == id));
    }

    public IDataResult<List<CommentDetailDto>> GetAll()
    {
        return new SuccessDataResult<List<CommentDetailDto>>(_commentDal.GetCommentDetails());
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
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = Messages.CommentAdded + $" - SolutionId: {comment.SolutionId}",
            Type = "Comment,Add,Info"
        });
        return new SuccessResult(Messages.CommentAdded);
    }

    public IResult Update(Comment comment)
    {
        _commentDal.Update(comment);
        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = Messages.CommentUpdated + $" - CommentId: {comment.Id}",
            Type = "Comment,Update,Info"
        });
        return new SuccessResult(Messages.CommentUpdated);
    }

    public IResult Delete(int id)
    {
        var comment = _commentDal.Get(comment => comment.Id == id);
        if (comment == null)
        {
            return new ErrorResult("Yorum Bulunamadı");
        }
        comment.IsDeleted = true;
        comment.DeleteDate = DateTime.Now;
        _commentDal.Update(comment);

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = Messages.CommentDeleted + $" - CommentId: {id}",
            Type = "Comment,Delete,Info"
        });
        return new SuccessResult(Messages.CommentDeleted);
    }
}