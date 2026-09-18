using Aviant.Application.Orchestration;
using Aviant.Application.UseCases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Aviant.Presentation.AspNetCore.Controllers;

/// <summary>
///     A controller that sends commands and queries through the orchestrator itself.
/// </summary>
[ApiController]
public abstract class OrchestratorController : ControllerBase
{
    /// <summary>
    ///     The request's orchestrator.
    /// </summary>
    protected IOrchestrator Orchestrator => HttpContext.RequestServices.GetRequiredService<IOrchestrator>();
}

/// <summary>
///     A controller that runs one use case and presents its output: the controller is the use case's
///     <typeparamref name="TUseCaseOutput" />, and each output method sets <see cref="ViewModel" />.
/// </summary>
/// <example>
///     <code>
///     public sealed class CreateTodoListController(CreateTodoListUseCase useCase)
///         : UseCaseController&lt;CreateTodoListUseCase, ICreateTodoListOutput&gt;(useCase), ICreateTodoListOutput
///     {
///         [HttpPost]
///         public async Task&lt;IActionResult&gt; Create(CreateTodoListInput input, CancellationToken cancellationToken)
///         {
///             UseCase.SetOutput(this);
///             await UseCase.ExecuteAsync(input, cancellationToken);
///             return ViewModel;
///         }
///
///         void ICreateTodoListOutput.Created(int id) => ViewModel = Ok(id);
///     }
///     </code>
/// </example>
[ApiController]
public abstract class UseCaseController<TUseCase, TUseCaseOutput> : ControllerBase, IUseCaseOutput
    where TUseCase : class, IUseCase<TUseCaseOutput>
    where TUseCaseOutput : class, IUseCaseOutput
{
    protected UseCaseController(TUseCase useCase) => UseCase = useCase;

    /// <summary>
    ///     The use case this controller runs.
    /// </summary>
    protected TUseCase UseCase { get; }

    /// <summary>
    ///     What the action returns; 204 until an output method sets it.
    /// </summary>
    protected IActionResult ViewModel { get; set; } = new NoContentResult();

    void IUseCaseOutput.BadRequest(object? @object) => ViewModel = BadRequest(@object);
}
