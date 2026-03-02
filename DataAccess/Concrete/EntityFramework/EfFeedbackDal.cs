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
    }
}
