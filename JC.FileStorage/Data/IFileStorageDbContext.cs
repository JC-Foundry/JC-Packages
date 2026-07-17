using JC.FileStorage.Models;
using Microsoft.EntityFrameworkCore;

namespace JC.FileStorage.Data;

public interface IFileStorageDbContext
{
    DbSet<SavedFile> SavedFiles { get; set; }
}