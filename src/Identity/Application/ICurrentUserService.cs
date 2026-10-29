namespace Aviant.Application.Identity;

/// <summary>
///     Who is making this request, when anybody is.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    ///     The signed-in account, or <see langword="null" /> when there is not one.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     <b>Nullable because "nobody" is a real answer and must not be able to
    ///     impersonate an identity.</b> This returned <see cref="Guid" /> and used
    ///     <see cref="Guid.Empty" /> for an anonymous caller — a value the type system
    ///     could not distinguish from a real account, so every consumer was free to write
    ///     it into a column, compare it, or substitute something for it, and the compiler
    ///     had nothing to say.
    ///     </para>
    ///     <para>
    ///     <b>What that cost downstream.</b> Fourteen audit rows in a consuming system
    ///     carry an all-zero actor: a populated column, a valid identifier, naming nobody,
    ///     so "who did this" returns an answer no account will ever match. Worse, three
    ///     places replaced the empty value with <c>Guid.NewGuid()</c> — a fresh random
    ///     identifier per call, indistinguishable from a real person — on the records for
    ///     who approved a refund, who wrote a customer-visible note and who raised a
    ///     purchase order.
    ///     </para>
    ///     <para>
    ///     <b>Null costs consumers almost nothing and finds exactly the wrong ones.</b>
    ///     Equality comparisons against an entity key still compile and still match no
    ///     row, which is the correct, closed answer. Anything taking a nullable already
    ///     works. What stops compiling is code that needs a definite identity, which is
    ///     precisely the code that should have been asked what it means to have none.
    ///     </para>
    /// </remarks>
    Guid? UserId { get; }
}
