using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using System;
using System.Collections.Generic;

namespace Business.Concrete
{
    public class ReportManager : IReportService
    {
        private readonly IReportDal _reportDal;
        private readonly IProblemService _problemService;
        private readonly ISolutionService _solutionService;
        private readonly IUserService _userService;

        public ReportManager(
            IReportDal reportDal,
            IProblemService problemService,
            ISolutionService solutionService,
            IUserService userService)
        {
            _reportDal = reportDal;
            _problemService = problemService;
            _solutionService = solutionService;
            _userService = userService;
        }

        public IResult Add(Report report)
        {
            report.ReportDate = DateTime.Now;
            report.IsResolved = false;

            _reportDal.Add(report);

            switch (report.TargetType)
            {
                case "Problem":
                    _problemService.ReportProblem(report.TargetId);
                    break;
                case "Solution":
                    _solutionService.ReportSolution(report.TargetId);
                    break;
                case "User":
                    _userService.ReportUser(report.TargetId);
                    break;
            }

            return new SuccessResult("Şikayetiniz başarıyla yönetime iletildi.");
        }

        public IResult Delete(int id)
        {
            var report = _reportDal.Get(r => r.Id == id);
            if (report == null) return new ErrorResult("Şikayet bulunamadı.");

            _reportDal.Delete(report);
            return new SuccessResult("Şikayet kaydı silindi.");
        }

        public IDataResult<List<Report>> GetAll()
        {
            return new SuccessDataResult<List<Report>>(_reportDal.GetAll(), "Tüm şikayetler listelendi.");
        }

        public IDataResult<Report> GetById(int id)
        {
            return new SuccessDataResult<Report>(_reportDal.Get(r => r.Id == id));
        }

        public IDataResult<List<Report>> GetPendingReports()
        {
            return new SuccessDataResult<List<Report>>(_reportDal.GetAll(r => r.IsResolved == false));
        }

        public IResult ResolveReport(int id)
        {
            var report = _reportDal.Get(r => r.Id == id);
            if (report == null) return new ErrorResult("Şikayet bulunamadı.");

            report.IsResolved = true;
            _reportDal.Update(report);

            switch (report.TargetType)
            {
                case "Problem":
                    _problemService.UnReportProblem(report.TargetId);
                    break;
                case "Solution":
                    _solutionService.UnReportSolution(report.TargetId);
                    break;
                case "User":
                    _userService.UnReportUser(report.TargetId);
                    break;
            }

            return new SuccessResult("Şikayet incelendi ve kapatıldı.");
        }
    }
}