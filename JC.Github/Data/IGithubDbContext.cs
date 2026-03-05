using JC.Github.Models;
using Microsoft.EntityFrameworkCore;

namespace JC.Github.Data;

public interface IGithubDbContext
{
    DbSet<ReportedIssue> ReportedIssues { get; set; }
}