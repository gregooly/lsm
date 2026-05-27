using LaundryMS.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Data;

/// <summary>
/// SQL-translatable filters for logistics job types. Do not use <c>LaundryMS.Web.Models.LogisticsJobTypeHelper</c> inside EF <c>Where</c> — it is not translatable.
/// </summary>
public static class LogisticsJobQueryExtensions
{
    public static IQueryable<LogisticsJob> WhereCollectionJobType(this IQueryable<LogisticsJob> source) =>
        source.Where(j =>
            (j.JobType ?? "").ToLower() == "collection"
            || (j.JobType ?? "").ToLower() == "pickup"
            || (j.JobType ?? "").ToLower() == "collect");

    public static IQueryable<LogisticsJob> WhereDeliveryJobType(this IQueryable<LogisticsJob> source) =>
        source.Where(j =>
            (j.JobType ?? "").ToLower() == "delivery"
            || (j.JobType ?? "").ToLower() == "dispatch");
}
