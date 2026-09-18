namespace Aviant.Application.UseCases;

/// <summary>
///     Gives a use case the dependency scope it runs in. Called once, by the factory that
///     <c>AddAviantUseCases</c> registers, right after the use case is constructed.
/// </summary>
public interface IUseCaseActivation
{
    public void Activate(IServiceProvider services);
}
