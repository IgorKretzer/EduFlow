namespace EduFlow.Infrastructure.Ops;

public sealed class OpsSettings
{
    public const string SectionName = "Ops";

    public string WorkersHealthUrl { get; set; } = "http://localhost:5055/health/live";
    public string RabbitManagementUrl { get; set; } = "http://127.0.0.1:15672";
    public string FrontendUrl { get; set; } = "http://localhost:3000";
    public string ApiSwaggerUrl { get; set; } = "http://localhost:8080/swagger";
}
