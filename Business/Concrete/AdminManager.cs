using Business.Abstract;
using Core.Utilities.Results;
using Core.CrossCuttingConcerns.Monitoring;
using DataAccess.Abstract;
using Entities.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Business.Concrete;

public class AdminManager : IAdminService
{
    private readonly IUserDal _userDal;
    private readonly IProblemDal _problemDal;
    private readonly ISolutionDal _solutionDal;
    private readonly IInstitutionDal _institutionDal;
    private readonly IMemoryCache _cache;
    private readonly ISystemMonitor _systemMonitor;

    public AdminManager(IUserDal userDal, IProblemDal problemDal, ISolutionDal solutionDal, IInstitutionDal institutionDal, IMemoryCache cache, ISystemMonitor systemMonitor)
    {
        _userDal = userDal;
        _problemDal = problemDal;
        _solutionDal = solutionDal;
        _institutionDal = institutionDal;
        _cache = cache;
        _systemMonitor = systemMonitor;
    }

    public IDataResult<AdminDashboardDto> GetDashboardStats()
    {
        string cacheKey = "AdminDashboardStats";

        if (!_cache.TryGetValue(cacheKey, out AdminDashboardDto stats))
        {
            stats = new AdminDashboardDto
            {
                TotalUsers = _userDal.Count(),
                BannedUsers = _userDal.Count(u => u.IsBanned),
                TotalProblems = _problemDal.Count(),
                ReportedProblems = _problemDal.Count(p => p.IsReported),
                TotalSolutions = _solutionDal.Count()
            };

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

            _cache.Set(cacheKey, stats, cacheEntryOptions);
        }

        return new SuccessDataResult<AdminDashboardDto>(stats, "Genel istatistikler getirildi.");
    }

    public IDataResult<DashboardAnalyticsDto> GetDashboardAnalytics()
    {
        string cacheKey = "AdminDashboardAnalytics";

        if (!_cache.TryGetValue(cacheKey, out DashboardAnalyticsDto analytics))
        {
            var users = _userDal.GetAll(); 
            int totalUsers = users.Count;
            int bannedUsers = users.Count(u => u.IsBanned);
            int activeUsers = totalUsers - bannedUsers;

            var problems = _problemDal.GetAll();
            var institutions = _institutionDal.GetAll();

            var problemsByInst = problems
                .GroupBy(p => p.InstitutionId)
                .Select(g => new InstitutionProblemCountDto
                {
                    InstitutionName = institutions.FirstOrDefault(i => i.Id == g.Key)?.Name ?? "Bilinmiyor",
                    Count = g.Count()
                })
                .ToList();

            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var recentUsers = users.Where(u => u.RegisterDate >= thirtyDaysAgo).ToList();
            
            var registrationsMap = new List<DailyUserRegistrationDto>();
            for (int i = 29; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i).Date;
                registrationsMap.Add(new DailyUserRegistrationDto
                {
                    Date = date.ToString("dd MMM yyyy"),
                    Count = recentUsers.Count(u => u.RegisterDate.Date == date)
                });
            }

            analytics = new DashboardAnalyticsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                BannedUsers = bannedUsers,
                ProblemsByInstitution = problemsByInst,
                UserRegistrationsLast30Days = registrationsMap
            };

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

            _cache.Set(cacheKey, analytics, cacheEntryOptions);
        }

        return new SuccessDataResult<DashboardAnalyticsDto>(analytics, "Gelişmiş analitik verileri getirildi.");
    }

    public IDataResult<SystemHealthDto> GetSystemHealthStatus()
    {
        var activeUsers = _systemMonitor.GetActiveUserCount(5);
        var ramUsage = Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0; // MB

        string cacheKey = "TurkeyMapDensity";
        if (!_cache.TryGetValue(cacheKey, out List<CityProblemDensityDto> cityData))
        {
            var problems = _problemDal.GetAll();
            var users = _userDal.GetAll();

            cityData = users.GroupBy(u => u.CityCode)
                .Select(g => new CityProblemDensityDto
                {
                    CityCode = g.Key,
                    UserCount = g.Count(),
                    ProblemCount = problems.Count(p => p.CityCode == g.Key)
                })
                .Where(c => c.CityCode > 0 && c.CityCode <= 81)
                .ToList();

            _cache.Set(cacheKey, cityData, TimeSpan.FromMinutes(10));
        }

        var healthDto = new SystemHealthDto
        {
            TotalRequests = _systemMonitor.TotalRequests,
            TotalErrors = _systemMonitor.TotalErrors,
            AverageResponseTimeMs = Math.Round(_systemMonitor.AverageResponseTimeMs, 2),
            ActiveUsers = activeUsers,
            RamUsageMb = Math.Round(ramUsage, 2),
            TrafficHistory = _systemMonitor.GetTrafficHistory(),
            TurkeyMapData = cityData
        };

        return new SuccessDataResult<SystemHealthDto>(healthDto, "Sistem sağlığı verileri başarıyla getirildi.");
    }
}
