using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Airtable.EFCore.Infrastructure;

internal sealed class AirtableOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;
    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public string BaseId { get; init; }
    public string ApiKey { get; init; }

    public AirtableOptionsExtension()
    {
    }

    public AirtableOptionsExtension(AirtableOptionsExtension original)
    {
        BaseId = original.BaseId;
        ApiKey = original.ApiKey;
    }

    public void ApplyServices(IServiceCollection services)
    {
        services.AddEntityFrameworkAirtableDatabase();
    }

    public void Validate(IDbContextOptions options)
    {
    }

    public AirtableOptionsExtension WithBaseId(string baseId)
    {
        return new AirtableOptionsExtension(this) { BaseId = baseId };
    }
    public AirtableOptionsExtension WithApiKey(string apiKey)
    {
        return new AirtableOptionsExtension(this) { ApiKey = apiKey };
    }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        private string? _logFragment;

        public ExtensionInfo(IDbContextOptionsExtension extension)
            : base(extension)
        {
        }

        private new AirtableOptionsExtension Extension
            => (AirtableOptionsExtension)base.Extension;

        public override bool IsDatabaseProvider => true;
        public override string LogFragment
        {
            get
            {
                if (_logFragment == null)
                {
                    var builder = new StringBuilder();

                    builder.Append("ServiceEndPoint=").Append(Extension.BaseId);

                    _logFragment = builder.ToString();
                }

                return _logFragment;
            }
        }

        public override int GetServiceProviderHashCode()
        {
            return Extension.GetHashCode();
        }

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
        {
            return other.Extension is AirtableOptionsExtension ext && Extension.Equals(ext);
        }
    }
}
