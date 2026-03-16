using Business.Abstract;
using Business.Constants;
using Core.Utilities.Context;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class CommentManager : ICommentService
{
    private readonly ICommentDal _commentDal;
    private readonly ILogService _logService;
    private readonly IClientContext _clientContext;

    public CommentManager(ICommentDal commentDal, ILogService logService, IClientContext clientContext)
    {
        _commentDal = commentDal;
        _logService = logService;
        _clientContext = clientContext;
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
        comment.SenderId = _clientContext.GetUserId() ?? 0;
        comment.SendDate = DateTime.Now;
        _commentDal.Add(comment);
        _logService.LogInfo("Content", "Add", $"Yorum eklendi - SolutionId: {comment.SolutionId}");
        return new SuccessResult(Messages.CommentAdded);
    }

    public IResult Update(CommentUpdateDto commentUpdateDto)
    {
        var currentUserId = _clientContext.GetUserId();
        var isAdmin = _clientContext.GetRoles().Contains("Admin");

        var comment = _commentDal.Get(c => c.Id == commentUpdateDto.Id);
        if (comment == null) return new ErrorResult("Yorum Bulunamadı");

        if (!isAdmin && comment.SenderId != currentUserId)
        {
            return new ErrorResult("Bu yorumu güncelleme yetkiniz yok.");
        }
        comment.Text = commentUpdateDto.Text;

        _commentDal.Update(comment);
        _logService.LogInfo("Content", "Update", $"Yorum güncellendi - CommentId: {comment.Id}");
        return new SuccessResult(Messages.CommentUpdated);
    }

    public IResult Delete(int id)
    {
        var currentUserId = _clientContext.GetUserId();
        var isAdmin = _clientContext.GetRoles().Contains("Admin");

        var comment = _commentDal.Get(comment => comment.Id == id);
        if (comment == null) return new ErrorResult("Yorum Bulunamadı");

        if (!isAdmin && comment.SenderId != currentUserId)
        {
            return new ErrorResult("Bu yorumu silme yetkiniz yok.");
        }

        comment.IsDeleted = true;
        comment.DeleteDate = DateTime.Now;
        _commentDal.Update(comment);

        var childComments = _commentDal.GetAll(cc => cc.ParentCommentId == id && !cc.IsDeleted);
        foreach (var childCom in childComments)
        {
            childCom.IsDeleted = true;
            childCom.DeleteDate = DateTime.Now;
            _commentDal.Update(childCom);
        }

        _logService.LogWarning("Content", "Delete", $"Yorum silindi - CommentId: {id} (Alt Yanıtlarıyla Birlikte)");

        return new SuccessResult(Messages.CommentDeleted);
    }
}