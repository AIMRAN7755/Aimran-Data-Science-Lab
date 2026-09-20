using AIMRAN_Data_Science_Lab.Services;
using AimranDataScienceLab.Gateway;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Generates data visualizations via the Gateway's Python AI Engine.
/// </summary>
internal sealed class GatewayVisualizationService : IVisualizationService
{
    private readonly IGatewayManager _gateway;

    public GatewayVisualizationService(IGatewayManager gateway)
    {
        _gateway = gateway;
    }

    public async Task<VisualizationResult> GenerateChartAsync(
        VisualizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var pyRequest = new PythonVisualizationRequest
        {
            DatasetPath = request.DatasetPath,
            ChartType = request.ChartType,
            XColumn = request.XColumn,
            YColumn = request.YColumn,
            Columns = request.Columns,
            Title = request.Title,
            Options = request.Options
        };

        var result = await _gateway.PythonEngine.GenerateChartAsync(pyRequest, cancellationToken);
        return new VisualizationResult
        {
            ChartType = result.ChartType,
            FigureBase64 = result.FigureBase64,
            PythonCode = result.PythonCode
        };
    }
}
