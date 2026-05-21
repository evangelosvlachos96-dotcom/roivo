using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Roivo.Application.Abstractions;
using Roivo.Application.Features.Invoices.Queries.GetBusinessInvoiceSummary;
using Roivo.Application.Features.Invoices.Queries.ListBusinessInvoicesPaged;

namespace Roivo.Infrastructure.Persistence.Repositories;

/// <summary>
/// Read-side queries over synced AADE data. The detailed listing and recent-
/// activity feed combine <c>Invoices</c> (incoming, per-document) and
/// <c>IncomeBookEntries</c> (outgoing, aggregated). EF Core cannot translate a
/// UNION over already-projected queries on Npgsql, so those two queries use raw
/// parameterized SQL (see CODING_STANDARDS.md "Raw SQL in repositories").
/// </summary>
public sealed class InvoiceQueryRepository : IInvoiceQueryRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly ITenantContext _tenantContext;

    public InvoiceQueryRepository(
        IDbContextFactory<ApplicationDbContext> factory,
        ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(tenantContext);
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public async Task<InvoiceSummaryData> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
    {
        // Three independent reads, each with its own DbContext (the lifecycle rule
        // forbids concurrent operations on a shared context). Totals are simple
        // single-table aggregations and stay in LINQ; the recent feed unions both
        // tables and uses raw SQL.
        var incomingTask = LoadIncomingTotalsAsync(businessId, ct);
        var outgoingTask = LoadOutgoingTotalsAsync(businessId, ct);
        var recentTask = LoadRecentAsync(businessId, ct);

        await Task.WhenAll(incomingTask, outgoingTask, recentTask).ConfigureAwait(false);

        var (incomingGross, incomingCount) = await incomingTask.ConfigureAwait(false);
        var (outgoingGross, aggregateCount, invoiceCount) = await outgoingTask.ConfigureAwait(false);
        var recent = await recentTask.ConfigureAwait(false);

        return new InvoiceSummaryData(
            IncomingGrossTotal: incomingGross,
            IncomingCount: incomingCount,
            OutgoingGrossTotal: outgoingGross,
            OutgoingAggregateCount: aggregateCount,
            OutgoingInvoiceCount: invoiceCount,
            Recent: recent);
    }

    private async Task<(decimal Gross, int Count)> LoadIncomingTotalsAsync(Guid businessId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var agg = await db.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId)
            .GroupBy(_ => 1)
            .Select(g => new { Gross = g.Sum(x => x.GrossAmount), Count = g.Count() })
            .FirstOrDefaultAsync(ct);

        return (agg?.Gross ?? 0m, agg?.Count ?? 0);
    }

    private async Task<(decimal Gross, int AggregateCount, int InvoiceCount)> LoadOutgoingTotalsAsync(Guid businessId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var agg = await db.IncomeBookEntries
            .AsNoTracking()
            .Where(e => e.BusinessId == businessId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Gross = g.Sum(x => x.GrossValue),
                AggregateCount = g.Count(),
                InvoiceCount = g.Sum(x => x.InvoiceCount),
            })
            .FirstOrDefaultAsync(ct);

        return (agg?.Gross ?? 0m, agg?.AggregateCount ?? 0, agg?.InvoiceCount ?? 0);
    }

    // UNION ALL of the 10 most recent rows across both tables. Tenant filtering is
    // explicit — EF's global query filter does not apply to raw SQL. issue_date
    // resolves to timestamptz (date ∪ timestamptz) so the reader gets UTC values.
    private const string RecentSql = @"
WITH combined AS (
    SELECT
        ""IssueDate"" AS issue_date,
        'Incoming' AS direction,
        COALESCE(""CounterpartyName"", ""CounterpartyAfm"") AS display,
        ""InvoiceType"" AS doc_type,
        ""GrossAmount"" AS gross_amount,
        CASE WHEN ""CancelledByMark"" IS NOT NULL THEN TRUE ELSE FALSE END AS is_cancelled,
        NULL::int AS aggregate_count
    FROM ""Invoices""
    WHERE ""BusinessId"" = @businessId AND ""TenantId"" = @tenantId

    UNION ALL

    SELECT
        ""IssueDate"" AS issue_date,
        'Outgoing' AS direction,
        ""CounterpartyAfm"" AS display,
        ""DocumentTypeCode"" AS doc_type,
        ""GrossValue"" AS gross_amount,
        FALSE AS is_cancelled,
        ""InvoiceCount"" AS aggregate_count
    FROM ""IncomeBookEntries""
    WHERE ""BusinessId"" = @businessId AND ""TenantId"" = @tenantId
)
SELECT * FROM combined
ORDER BY issue_date DESC
LIMIT 10";

    private async Task<IReadOnlyList<RecentInvoiceRow>> LoadRecentAsync(Guid businessId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        var rows = new List<RecentInvoiceRow>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = RecentSql;
        cmd.Parameters.Add(new NpgsqlParameter("@businessId", businessId));
        cmd.Parameters.Add(new NpgsqlParameter("@tenantId", _tenantContext.CurrentTenantId));

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new RecentInvoiceRow(
                IssueDate: reader.GetDateTime(0),
                Direction: reader.GetString(1),
                CounterpartyDisplay: reader.GetString(2),
                DocumentTypeCode: reader.GetString(3),
                GrossAmount: reader.GetDecimal(4),
                IsCancelled: reader.GetBoolean(5),
                AggregateCount: reader.IsDBNull(6) ? null : reader.GetInt32(6)));
        }

        return rows;
    }

    public async Task<(IReadOnlyList<InvoiceRow> Rows, int TotalCount)> ListPagedAsync(
        ListBusinessInvoicesPagedQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var db = await _factory.CreateDbContextAsync(ct);

        var sortColumn = SanitizeSortColumn(query.SortColumn);
        var sortDirection = query.SortDescending ? "DESC" : "ASC";

        var (incomingWhere, incomingParams) = BuildIncomingFilters(query);
        var (outgoingWhere, outgoingParams) = BuildOutgoingFilters(query);

        var fetchIncoming = query.Direction != InvoiceDirectionFilter.Outgoing;
        // Book entries are aggregates and can never be cancelled, so an
        // "only cancelled" filter excludes the outgoing side entirely.
        var fetchOutgoing = query.Direction != InvoiceDirectionFilter.Incoming
                            && query.CancelledStatus != CancelledFilter.OnlyCancelled;

        if (!fetchIncoming && !fetchOutgoing)
            return (Array.Empty<InvoiceRow>(), 0);

        var pageOffset = (query.Page - 1) * query.PageSize;

        var sql = new StringBuilder();

        if (fetchIncoming)
        {
            sql.AppendLine($@"
            SELECT
                ""IssueDate"" AS issue_date,
                'Incoming' AS direction,
                ""AadeMark"" AS mark,
                ""CounterpartyAfm"" AS counterparty_afm,
                ""CounterpartyName"" AS counterparty_name,
                ""InvoiceType"" AS document_type_code,
                ""NetAmount"" AS net_amount,
                ""VatAmount"" AS vat_amount,
                ""GrossAmount"" AS gross_amount,
                CASE WHEN ""CancelledByMark"" IS NOT NULL THEN TRUE ELSE FALSE END AS is_cancelled,
                NULL::int AS aggregate_count
            FROM ""Invoices""
            WHERE ""BusinessId"" = @businessId AND ""TenantId"" = @tenantId
            {(incomingWhere.Length > 0 ? "AND " + incomingWhere : "")}");
        }

        if (fetchIncoming && fetchOutgoing) sql.AppendLine("UNION ALL");

        if (fetchOutgoing)
        {
            sql.AppendLine($@"
            SELECT
                ""IssueDate"" AS issue_date,
                'Outgoing' AS direction,
                NULL::text AS mark,
                ""CounterpartyAfm"" AS counterparty_afm,
                NULL::text AS counterparty_name,
                ""DocumentTypeCode"" AS document_type_code,
                ""NetValue"" AS net_amount,
                ""VatAmount"" AS vat_amount,
                ""GrossValue"" AS gross_amount,
                FALSE AS is_cancelled,
                ""InvoiceCount"" AS aggregate_count
            FROM ""IncomeBookEntries""
            WHERE ""BusinessId"" = @businessId AND ""TenantId"" = @tenantId
            {(outgoingWhere.Length > 0 ? "AND " + outgoingWhere : "")}");
        }

        var dataSql = $@"
        WITH combined AS ({sql})
        SELECT *
        FROM combined
        ORDER BY {MapSortColumnToSqlAlias(sortColumn)} {sortDirection}
        LIMIT @pageSize OFFSET @offset";

        var countSql = $@"
        WITH combined AS ({sql})
        SELECT COUNT(*) FROM combined";

        var sharedParams = new List<NpgsqlParameter>
        {
            new("@businessId", query.BusinessId),
            new("@tenantId", _tenantContext.CurrentTenantId),
        };
        if (fetchIncoming) sharedParams.AddRange(incomingParams);
        if (fetchOutgoing) sharedParams.AddRange(outgoingParams);

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        int totalCount;
        await using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = countSql;
            countCmd.Parameters.AddRange(sharedParams.Select(CloneParameter).ToArray());
            totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct) ?? 0);
        }

        if (totalCount == 0)
            return (Array.Empty<InvoiceRow>(), 0);

        var dataParams = sharedParams.Select(CloneParameter).ToList();
        dataParams.Add(new NpgsqlParameter("@pageSize", query.PageSize));
        dataParams.Add(new NpgsqlParameter("@offset", pageOffset));

        var rows = new List<InvoiceRow>();
        await using (var dataCmd = conn.CreateCommand())
        {
            dataCmd.CommandText = dataSql;
            dataCmd.Parameters.AddRange(dataParams.ToArray());

            await using var reader = await dataCmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                rows.Add(new InvoiceRow(
                    IssueDate: reader.GetDateTime(0),
                    Direction: reader.GetString(1),
                    Mark: reader.IsDBNull(2) ? null : reader.GetString(2),
                    CounterpartyAfm: reader.GetString(3),
                    CounterpartyName: reader.IsDBNull(4) ? null : reader.GetString(4),
                    DocumentTypeCode: reader.GetString(5),
                    NetAmount: reader.GetDecimal(6),
                    VatAmount: reader.GetDecimal(7),
                    GrossAmount: reader.GetDecimal(8),
                    IsCancelled: reader.GetBoolean(9),
                    AggregateCount: reader.IsDBNull(10) ? null : reader.GetInt32(10)));
            }
        }

        return (rows, totalCount);
    }

    private static string SanitizeSortColumn(string requested)
    {
        // Allowlist: sort columns are enum-like, never free user input. Anything
        // off the list silently falls back to IssueDate.
        var allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "IssueDate", "issue_date" },
            { "GrossAmount", "gross_amount" },
            { "NetAmount", "net_amount" },
            { "CounterpartyAfm", "counterparty_afm" },
        };
        return allowed.ContainsKey(requested) ? requested : "IssueDate";
    }

    private static string MapSortColumnToSqlAlias(string apiColumn)
        => apiColumn switch
        {
            "IssueDate" => "issue_date",
            "GrossAmount" => "gross_amount",
            "NetAmount" => "net_amount",
            "CounterpartyAfm" => "counterparty_afm",
            _ => "issue_date",
        };

    private static (string WhereClause, List<NpgsqlParameter> Parameters) BuildIncomingFilters(ListBusinessInvoicesPagedQuery query)
    {
        var clauses = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        if (query.FromDate.HasValue)
        {
            clauses.Add(@"""IssueDate"" >= @fromDate");
            parameters.Add(new NpgsqlParameter("@fromDate", DateTime.SpecifyKind(query.FromDate.Value, DateTimeKind.Utc)));
        }
        if (query.ToDate.HasValue)
        {
            clauses.Add(@"""IssueDate"" <= @toDate");
            parameters.Add(new NpgsqlParameter("@toDate", DateTime.SpecifyKind(query.ToDate.Value, DateTimeKind.Utc)));
        }
        if (!string.IsNullOrWhiteSpace(query.CounterpartyAfmSearch))
        {
            clauses.Add(@"""CounterpartyAfm"" ILIKE @counterpartyAfm");
            parameters.Add(new NpgsqlParameter("@counterpartyAfm", $"%{query.CounterpartyAfmSearch.Trim()}%"));
        }
        if (query.CancelledStatus == CancelledFilter.HideCancelled)
            clauses.Add(@"""CancelledByMark"" IS NULL");
        if (query.CancelledStatus == CancelledFilter.OnlyCancelled)
            clauses.Add(@"""CancelledByMark"" IS NOT NULL");

        return (string.Join(" AND ", clauses), parameters);
    }

    private static (string WhereClause, List<NpgsqlParameter> Parameters) BuildOutgoingFilters(ListBusinessInvoicesPagedQuery query)
    {
        var clauses = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        if (query.FromDate.HasValue)
        {
            clauses.Add(@"""IssueDate"" >= @fromDateOut");
            parameters.Add(new NpgsqlParameter("@fromDateOut", DateTime.SpecifyKind(query.FromDate.Value, DateTimeKind.Utc)));
        }
        if (query.ToDate.HasValue)
        {
            clauses.Add(@"""IssueDate"" <= @toDateOut");
            parameters.Add(new NpgsqlParameter("@toDateOut", DateTime.SpecifyKind(query.ToDate.Value, DateTimeKind.Utc)));
        }
        if (!string.IsNullOrWhiteSpace(query.CounterpartyAfmSearch))
        {
            clauses.Add(@"""CounterpartyAfm"" ILIKE @counterpartyAfmOut");
            parameters.Add(new NpgsqlParameter("@counterpartyAfmOut", $"%{query.CounterpartyAfmSearch.Trim()}%"));
        }

        return (string.Join(" AND ", clauses), parameters);
    }

    // The same NpgsqlParameter instance cannot belong to two commands, so each
    // command (count + data) gets its own clones.
    private static NpgsqlParameter CloneParameter(NpgsqlParameter source)
        => new(source.ParameterName, source.Value);
}
