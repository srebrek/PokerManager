using System.Diagnostics;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Api.Bootstrapper;

internal static class TelemetryExtensions
{
    public static WebApplicationBuilder AddDatabaseAndMessagingTelemetry(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddNpgsqlInstrumentation()
                .AddMeter("Wolverine*"))
            .WithTracing(tracing => tracing
                .AddNpgsql()
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource("Wolverine")
                .AddProcessor(new InfrastructureSqlFilterProcessor()));

        return builder;
    }

    private sealed class InfrastructureSqlFilterProcessor : BaseProcessor<Activity>
    {
        private static readonly string[] s_wolverineInternalOps =
        [
            "wolverine_node_assignments",
            "wolverine_node_activity",
            "wolverine_inbox",
            "wolverine_outbox",
            "wolverine_dead_letters",
            "wolverine_scheduled_messages",
        ];

        public override void OnEnd(Activity activity)
        {
            if (IsWolverineInternalOp(activity))
            {
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
                return;
            }

            if (activity.OperationName == "postgresql"
                || activity.OperationName == "db-server"
                || activity.Kind == ActivityKind.Client)
            {
                string? dbQuery = activity.GetTagItem("db.query.text") as string
                               ?? activity.GetTagItem("db.statement") as string;

                if (dbQuery is not null && (dbQuery.Contains("wolverine", StringComparison.OrdinalIgnoreCase)
                    || dbQuery.Contains("pg_try_advisory", StringComparison.OrdinalIgnoreCase)
                    || dbQuery.Contains("select 1", StringComparison.OrdinalIgnoreCase)
                    || dbQuery.Contains("data_protection_keys", StringComparison.OrdinalIgnoreCase)))
                {
                    activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
                }
            }
        }

        private static bool IsWolverineInternalOp(Activity activity)
        {
            string name = activity.DisplayName;
            return s_wolverineInternalOps.Any(op => name.Contains(op, StringComparison.OrdinalIgnoreCase));
        }
    }
}
