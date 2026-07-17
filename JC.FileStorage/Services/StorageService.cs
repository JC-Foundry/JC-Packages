using System.Text;
using JC.Core.Enums;
using JC.Core.Extensions;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using JC.FileStorage.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JC.FileStorage.Services;

public class StorageService
{
    private readonly IUserInfo? _userInfo;
    private readonly ILogger<StorageService> _logger;
    private readonly FilePathProvider _pathProvider;
    private readonly FolderRegistry _folderRegistry;
    private IRepositoryManager _repos { get; set; }

    public StorageService(IRepositoryManager repos,
        IServiceProvider serviceProvider,
        ILogger<StorageService> logger,
        FilePathProvider pathProvider,
        FolderRegistry folderRegistry)
    {
        _userInfo = serviceProvider.GetService<IUserInfo>();
        _logger = logger;
        _pathProvider = pathProvider;
        _folderRegistry = folderRegistry;
        _repos = repos;
    }
    
    public void ChangeContext(Type contextType)
        => _repos = _repos.For(contextType);

    private void ValidateTenant(FolderModel folder, string? tenantId)
    {
        if(string.IsNullOrEmpty(tenantId) && !string.Equals(folder.Tenant, FolderModel.NullTenantName, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The folder must belong to the default tenant.");
        
        if(!string.IsNullOrEmpty(tenantId) && !string.Equals(folder.Tenant, tenantId))
            throw new ArgumentException("The folder must belong to the same tenant as the file.");
    }


    #region Get File

    private async Task<SavedFile?> GetSavedFile(FolderModel folder, string fileName, string? tenantId)
    {
        var query = _repos.GetRepository<SavedFile>()
            .AsQueryable().FilterDeleted(DeletedQueryType.OnlyActive);
        
        //Unable to do role check (JC.Identity) for cross-tenant query
        if(!string.Equals(_userInfo?.TenantId, tenantId))
            query = query.IgnoreQueryFilters()
                .Where(f => f.TenantId == tenantId);
        
        //FileName is stored without its extension, so the lookup has to be keyed the same way
        var name = SavedFile.NormaliseFileName(fileName).ToLower();
        return await query.FirstOrDefaultAsync(f => f.FolderName.ToLower() == folder.Name.ToLower() 
                                                    && f.FileName.ToLower() == name);
    }

    private string GetSavedFilePath(FolderModel folder, SavedFile dbFile)
    {
        var path = _pathProvider.GetPath(folder);
        return _pathProvider.GetFilePath(path, dbFile.Id, dbFile.Extension);
    }
    
    
    public async Task<GetFileTextResponse> GetSavedFileText(FolderModel folder, string fileName)
        => await GetSavedFileTextForTenant(_userInfo?.TenantId, folder, fileName);
    
    public async Task<GetFileByteResponse> GetSavedFileBytes(FolderModel folder, string fileName)
        => await GetSavedFileBytesForTenant(_userInfo?.TenantId, folder, fileName);
    
    public async Task<GetFileByteResponse> GetSavedFileBytesForTenant(string? tenantId, FolderModel folder, string fileName)
    {
        ValidateTenant(folder, tenantId);
        
        var dbFile = await GetSavedFile(folder, fileName, tenantId);
        if(dbFile == null)
            return new GetFileByteResponse("File not found.");
        
        var filePath = GetSavedFilePath(folder, dbFile);
        if(!File.Exists(filePath))
            return new GetFileByteResponse("File not found.");

        try
        {
            var bytes = await File.ReadAllBytesAsync(filePath);
            return new GetFileByteResponse(dbFile, bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file {FileId} from folder {FolderName}", dbFile.Id, folder.Name);
            return new GetFileByteResponse("Error reading file.");
        }
    }
    
    public async Task<GetFileTextResponse> GetSavedFileTextForTenant(string? tenantId, FolderModel folder, string fileName)
    {
        ValidateTenant(folder, tenantId);
        
        var dbFile = await GetSavedFile(folder, fileName, tenantId);
        if(dbFile == null)
            return new GetFileTextResponse("File not found.");
        
        var filePath = GetSavedFilePath(folder, dbFile);
        if(!File.Exists(filePath))
            return new GetFileTextResponse("File not found.");
        
        try
        {
            var text = await File.ReadAllTextAsync(filePath);
            return new GetFileTextResponse(dbFile, text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file {FileId} from folder {FolderName}", dbFile.Id, folder.Name);
            return new GetFileTextResponse("Error reading file.");
        }
    }
    
    #endregion
    
    
    
    #region Save File

    public async Task<bool> TrySaveFile(FolderModel folder, string fileName, string fileText, string ext,
        bool blockOverwrite = true)
        => await TrySaveFileForTenant(_userInfo?.TenantId, folder, fileName, fileText, ext, blockOverwrite);
    
    public async Task<bool> TrySaveFile(FolderModel folder, string fileName, byte[] content, string ext,
        bool blockOverwrite = true)
        => await TrySaveFileForTenant(_userInfo?.TenantId, folder, fileName, content, ext, blockOverwrite);
    
    
    public async Task<bool> TrySaveFileForTenant(string? tenantId, FolderModel folder, string fileName, string fileText, 
        string ext, bool blockOverwrite = true)
    {
        var bytes = Encoding.UTF8.GetBytes(fileText);
        return await TrySaveFileForTenant(tenantId, folder, fileName, bytes, ext, blockOverwrite);
    }
    
    public async Task<bool> TrySaveFileForTenant(string? tenantId, FolderModel folder, string fileName, byte[] content,
        string ext, bool blockOverwrite = true)
    {
        ValidateTenant(folder, tenantId);
        
        var adding = false;
        var savedFile = await GetSavedFile(folder, fileName, tenantId);
        if (savedFile == null)
        {
            adding = true;
            savedFile = new SavedFile
            {
                TenantId = folder.TenantId
            };
            savedFile.SetFolderName(folder);
        }
        else if (blockOverwrite)
            return false;

        //Set on both paths - an overwrite that changes the extension must update the row to match
        //the file that gets written
        var previousExt = savedFile.Extension;
        savedFile.SetFileName(fileName, ext);

        //Enforced here, not in a caller, so no entry point can store a file the folder forbids.
        //Uses the stored extension so it matches what actually lands on disk
        var validation = _folderRegistry.ValidateFile(folder, savedFile.Extension, content.Length);
        if(!validation.Result)
        {
            _logger.LogWarning("File validation failed. Type: {ErrorType}, Message: {ErrorMessage}", 
                validation.Error, validation.ErrorMessage);
            return false;
        }

        var path = _pathProvider.GetPath(folder);
        //Built from the stored extension, not the parameter, so the row and the file always agree
        var filePath = _pathProvider.GetFilePath(path, savedFile.Id, savedFile.Extension);

        await _repos.BeginTransactionAsync();
        try
        {
            //Save the file to the db (saveNow=true - failure rolls back before file is saved)
            //If error writing to file, then this transaction will also be rolled back
            if(adding)
                await _repos.GetRepository<SavedFile>()
                    .AddAsync(savedFile);
            else
                await _repos.GetRepository<SavedFile>()
                    .UpdateAsync(savedFile);
            
            //Create or overwrite the file
            await using var stream = File.Create(filePath);
            await stream.WriteAsync(content);
            await stream.FlushAsync();
            
            //Commit the transaction
            await _repos.CommitTransactionAsync();

            //An overwrite that changed the extension wrote to a new path, stranding the old file
            if(!adding && !string.Equals(previousExt, savedFile.Extension, StringComparison.OrdinalIgnoreCase))
                DeleteStaleFile(path, savedFile.Id, previousExt);

            return true;
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Error saving file {FileId} to folder {FolderName}", savedFile.Id, folder.Name);
            return false;
        }
    }

    private void DeleteStaleFile(string path, string id, string ext)
    {
        //The save has already committed, so failing to clean up must not fail the call
        try
        {
            var stalePath = _pathProvider.GetFilePath(path, id, ext);
            if(_pathProvider.CheckFileExists(stalePath))
                File.Delete(stalePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to delete the replaced file {Id}{Ext} in {Path}", id, ext, path);
        }
    }

    #endregion


    #region Delete File

    public async Task<bool> TryDeleteFile(FolderModel folder, string fileName)
        => await TryDeleteFileForTenant(_userInfo?.TenantId, folder, fileName);

    public async Task<bool> TryDeleteFileForTenant(string? tenantId, FolderModel folder, string fileName)
    {
        ValidateTenant(folder, tenantId);

        var savedFile = await GetSavedFile(folder, fileName, tenantId);
        if(savedFile == null)
            return false;

        var filePath = GetSavedFilePath(folder, savedFile);
        await _repos.BeginTransactionAsync();
        try
        {
            //The record is only soft deleted, so an audit can still show the file was here and who
            //removed it. There is no restore path - JC.Core's cleanup job reaps the record in time
            await _repos.GetRepository<SavedFile>()
                .SoftDeleteAsync(savedFile);

            //The file itself goes permanently. A failure here (a reader holding a lock, say) rolls
            //the record back, leaving both sides unchanged so the caller can retry
            File.Delete(filePath);

            //Commit the transaction
            await _repos.CommitTransactionAsync();
            return true;
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Error deleting file {FileId} from folder {FolderName}", savedFile.Id, folder.Name);
            return false;
        }
    }

    #endregion
    
}