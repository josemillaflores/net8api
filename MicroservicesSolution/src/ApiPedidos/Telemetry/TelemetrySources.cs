using System.Diagnostics;

namespace ApiPedidos.Telemetry;

public static class TelemetrySources
{
    public static readonly ActivitySource MainActivitySource = new("ApiPedidos");
    public static readonly ActivitySource UseCaseActivitySource = new("ApiPedidos.UseCases");
    public static readonly ActivitySource RepositoryActivitySource = new("ApiPedidos.Repositories");
    public static readonly ActivitySource ExternalServiceActivitySource = new("ApiPedidos.ExternalServices");
}