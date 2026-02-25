using Core.Utilities.Results;
using Entities.Concrete;
using System.Collections.Generic;

namespace Business.Abstract
{
    public interface IReportService
    {
        IDataResult<List<Report>> GetAll();
        IDataResult<List<Report>> GetPendingReports();
        IDataResult<Report> GetById(int id);
        IResult Add(Report report);
        IResult Delete(int id);
        IResult ResolveReport(int id);
    }
}