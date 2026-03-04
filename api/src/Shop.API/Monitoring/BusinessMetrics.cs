using Prometheus;

namespace Shop.API.Monitoring;

public static class BusinessMetrics
{
    private static readonly Counter OrdersCreated = Metrics
        .CreateCounter("shop_orders_created_total", "Total orders created",
            new CounterConfiguration { LabelNames = ["tenant_id", "status"] });

    private static readonly Counter PaymentsProcessed = Metrics
        .CreateCounter("shop_payments_processed_total", "Total payments processed",
            new CounterConfiguration { LabelNames = ["tenant_id", "provider", "status"] });

    private static readonly Gauge ActiveUsers = Metrics
        .CreateGauge("shop_active_users", "Currently active users",
            new GaugeConfiguration { LabelNames = ["tenant_id"] });

    private static readonly Counter AutoReorderTriggered = Metrics
        .CreateCounter("shop_auto_reorder_triggered_total", "Auto reorder jobs triggered",
            new CounterConfiguration { LabelNames = ["tenant_id"] });

    private static readonly Counter MesSyncOperations = Metrics
        .CreateCounter("shop_mes_sync_total", "MES sync operations",
            new CounterConfiguration { LabelNames = ["status"] });

    private static readonly Histogram ApiRequestDuration = Metrics
        .CreateHistogram("shop_api_request_duration_seconds", "API request duration",
            new HistogramConfiguration
            {
                LabelNames = ["method", "endpoint", "status_code"],
                Buckets = [0.01, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0]
            });

    public static void RecordOrderCreated(int tenantId, string status)
        => OrdersCreated.WithLabels(tenantId.ToString(), status).Inc();

    public static void RecordPayment(int tenantId, string provider, string status)
        => PaymentsProcessed.WithLabels(tenantId.ToString(), provider, status).Inc();

    public static void SetActiveUsers(int tenantId, int count)
        => ActiveUsers.WithLabels(tenantId.ToString()).Set(count);

    public static void RecordAutoReorder(int tenantId)
        => AutoReorderTriggered.WithLabels(tenantId.ToString()).Inc();

    public static void RecordMesSync(string status)
        => MesSyncOperations.WithLabels(status).Inc();

    public static IDisposable TrackApiRequest(string method, string endpoint, string statusCode)
        => ApiRequestDuration.WithLabels(method, endpoint, statusCode).NewTimer();
}
