using Airtable.EFCore.Infrastructure;
using AirtableApiClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

    private static Fields GetFields(IUpdateEntry entry, IEnumerable<IProperty> properties)
    {
        var result = new Fields();
        foreach (var item in properties)
        {
            var name = item.GetColumnName() ?? item.Name;
            var value = entry.GetCurrentValue(item);

            if (value is AirtableAttachment attachment)
                value = new[] { attachment };

            if (item.GetValueConverter() is ValueConverter converter)
            {
                value = converter.ConvertToProvider(value);
            }

            result.AddField(name, value);
        }

        return result;
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
                    .Where(i => !i.IsPrimaryKey());

            switch (item.EntityState)
            {
                case Microsoft.EntityFrameworkCore.EntityState.Detached:
                    break;
                case Microsoft.EntityFrameworkCore.EntityState.Unchanged:
                    break;
                case Microsoft.EntityFrameworkCore.EntityState.Deleted:
                    tasks.Add(_airtableBase.DeleteRecord(tableName, recordId ?? throw new InvalidOperationException("Record id is null")));
                    break;
                case Microsoft.EntityFrameworkCore.EntityState.Modified:
                    tasks.Add(Update(tableName, GetFields(item, setProps.Where(i => item.IsModified(i))), recordId));
                    break;
                case Microsoft.EntityFrameworkCore.EntityState.Added:
                    tasks.Add(Create(tableName, keyProp, item, GetFields(item, setProps)));
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

    private async Task Update(string tableName, Fields fields, string? recordId)
    {
        var result = await _airtableBase.UpdateRecord(tableName, fields, recordId);

        if (!result.Success) throw result.AirtableApiError;
    }

    private async Task Create(string tableName, IProperty primaryKey, IUpdateEntry updateEntry, Fields fields)
    {
        var result = await _airtableBase.CreateRecord(tableName, fields);

        if (!result.Success) throw result.AirtableApiError;

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

    public Task DeleteRecord(string tableName, string recordId)
        => _airtable.DeleteRecord(tableName, recordId);
    public Task<AirtableRetrieveRecordResponse> GetRecord(string tableName, string recordId)
        => _airtable.RetrieveRecord(tableName, recordId);
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

    Task DeleteRecord(string tableName, string recordId);

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

    Task<AirtableRetrieveRecordResponse> GetRecord(string tableName, string recordId);
}