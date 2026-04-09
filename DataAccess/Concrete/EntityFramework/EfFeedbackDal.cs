using Core.DataAccess.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Concrete.EntityFramework
{
    public class EfFeedbackDal:EfEntityRepositoryBase<Feedback, DevelopTurkeyContext>, IFeedbackDal
    {
        public List<FeedbackDetailDto> GetFeedbackDetails()
        {
            using var context = new DevelopTurkeyContext();
            var result = from f in context.Feedbacks
                join u in context.Users on f.UserId equals u.Id
                orderby f.SendDate descending
                select new FeedbackDetailDto
                {
                    Id = f.Id,
                    UserId = f.UserId,
                    UserName = u.UserName,
                    UserEmail = u.Email,
                    Title = f.Title,
                    Message = f.Message,
                    IsRead = f.IsRead,
                    SendDate = f.SendDate
                };
            return result.ToList();
        }

        public (List<FeedbackDetailDto> Items, int TotalCount) GetFeedbackDetailsPaged(FeedbackFilterDto filter)
        {
            using var context = new DevelopTurkeyContext();

            var query = from f in context.Feedbacks
                        join u in context.Users on f.UserId equals u.Id
                        select new FeedbackDetailDto
                        {
                            Id = f.Id,
                            UserId = f.UserId,
                            UserName = u.UserName,
                            UserEmail = u.Email,
                            Title = f.Title,
                            Message = f.Message,
                            IsRead = f.IsRead,
                            SendDate = f.SendDate
                        };

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var s = filter.SearchText.ToLower();
                query = query.Where(f =>
                    f.Title.ToLower().Contains(s) ||
                    f.Message.ToLower().Contains(s));
            }

            if (filter.IsRead.HasValue)
                query = query.Where(f => f.IsRead == filter.IsRead.Value);

            int totalCount = query.Count();
            var items = query
                .OrderByDescending(f => f.SendDate)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return (items, totalCount);
        }
    }
}
