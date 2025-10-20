namespace ApiConsulta.Application.UseCases.ProcesarEventoPago
{
    public interface IProcesarEventoPagoUseCase
    {
        Task<ProcesarEventoPagoResponse> ExecuteAsync(ProcesarEventoPagoCommand command, CancellationToken cancellationToken = default);
    }
}