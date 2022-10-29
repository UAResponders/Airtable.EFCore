using Airtable.EFCore.Infrastructure;
using Airtable.EFCore.Metadata.Conventions;
using AirtableApiClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace Airtable.EFCore.Storage.Internal;

internal sealed class AirtableDatabase : Database
{
    private readonly IAirtableClient _airtableBase;

    public AirtableDatabase(DatabaseDependencies dependencies, IAirtableClient airtableBase) : base(dependencies)
    {
        _airtableBase = airtableBase;
    }

    public override int SaveChanges(IList<IUpdateEntry> entries)
    {
        throw new NotImplementedException();
    }

    public override async Task<int> SaveChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();

        foreach (var item in entries)
        {
            var tableName = item.EntityType.GetTableName() ?? item.EntityType.Name;
            var keyProp = item.EntityType.FindPrimaryKey()!.Properties!.FirstOrDefault()!;
            var recordId = item.GetCurrentValue(keyProp)?.ToString();

            var setProps =
                item.EntityType
                    .GetProperties()
                    .Where(i => !i.IsPrimaryKey())
                    .Where(i => item.IsModified(i))
                    .ToDictionary(i => i.GetColumnName() ?? i.Name, i => item.GetCurrentValue(i));

            switch (item.EntityState)
            {
                case Microsoft.EntityFrameworkCore.EntityState.Detached:
                    break;
                case Microsoft.EntityFrameworkCore.EntityState.Unchanged:
                    break;
                case Microsoft.EntityFrameworkCore.EntityState.Deleted:
                    tasks.Add(_airtableBase.DeleteRecord(tableName, recordId));
                    break;
                case Microsoft.EntityFrameworkCore.EntityState.Modified:
                    tasks.Add(_airtableBase.UpdateRecord(tableName, new Fields { FieldsCollection = setProps }, recordId));
                    break;
                case Microsoft.EntityFrameworkCore.EntityState.Added:
                    tasks.Add(Create(tableName, keyProp, item, new Fields { FieldsCollection = setProps }));
                    break;
                default:
                    break;
            }
        }

        foreach (var item in tasks)
        {
            await item;
        }

        return tasks.Count;
    }

    private async Task Create(string tableName, IProperty primaryKey, IUpdateEntry updateEntry, Fields fields)
    {
        var result = await _airtableBase.CreateRecord(tableName, fields);

        updateEntry.SetStoreGeneratedValue(primaryKey, result.Record.Id);
    }
}

public sealed class AirtableBaseWrapper : IAirtableClient
{
    private readonly AirtableBase _airtable;

    public AirtableBaseWrapper(IDbContextOptions dbContextOptions)
    {
        var options = dbContextOptions.FindExtension<AirtableOptionsExtension>() ?? throw new InvalidOperationException("Options don't have AirtableOptionsExtension");

        _airtable = new AirtableBase(options.ApiKey, options.BaseId);
    }

    public Task<AirtableCreateUpdateReplaceRecordResponse> CreateRecord(string tableName, Fields fields)
        => _airtable.CreateRecord(tableName, fields);

    public Task DeleteRecord(string tableName, string? recordId) 
        => _airtable.DeleteRecord(tableName, recordId);

    public Task<AirtableListRecordsResponse?> ListRecords(
        string tableName,
        string? offset = null,
        IEnumerable<string>? fields = null,
        string? filterByFormula = null,
        int? maxRecords = null,
        int? pageSize = null,
        IEnumerable<Sort>? sort = null,
        string? view = null,
        string? cellFormat = null,
        string? timeZone = null,
        string? userLocale = null,
        bool returnFieldsByFieldId = false) => _airtable.ListRecords(tableName, offset, fields, filterByFormula, maxRecords, pageSize, sort, view, cellFormat, timeZone, userLocale, returnFieldsByFieldId);
    
    public Task<AirtableCreateUpdateReplaceRecordResponse> UpdateRecord(string tableName, Fields fields, string? recordId) 
        => _airtable.UpdateRecord(tableName, fields, recordId);
}

public interface IAirtableClient
{
    Task<AirtableCreateUpdateReplaceRecordResponse> CreateRecord(string tableName, Fields fields);
    Task DeleteRecord(string tableName, string? recordId);
    Task<AirtableListRecordsResponse?> ListRecords(
        string tableName,
        string? offset = null,
        IEnumerable<string>? fields = null,
        string? filterByFormula = null,
        int? maxRecords = null,
        int? pageSize = null,
        IEnumerable<Sort>? sort = null,
        string? view = null,
        string? cellFormat = null,
        string? timeZone = null,
        string? userLocale = null,
        bool returnFieldsByFieldId = false);
    Task<AirtableCreateUpdateReplaceRecordResponse> UpdateRecord(string tableName, Fields fields, string? recordId);
}