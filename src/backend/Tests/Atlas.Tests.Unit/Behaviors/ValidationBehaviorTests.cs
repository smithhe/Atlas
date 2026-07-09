using Atlas.Application.Behaviors;
using FluentValidation;
using MediatR;

namespace Atlas.Tests.Unit.Behaviors;

public sealed class ValidationBehaviorTests
{
    private sealed record SampleRequest(string Name) : IRequest<string>;

    private sealed class SampleRequestValidator : AbstractValidator<SampleRequest>
    {
        public SampleRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    [Fact]
    public async Task Handle_WhenNoValidatorsRegistered_InvokesNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(Array.Empty<IValidator<SampleRequest>>());
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        string result = await behavior.Handle(new SampleRequest(string.Empty), next, CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([new SampleRequestValidator()]);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new SampleRequest(string.Empty), next, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_InvokesNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([new SampleRequestValidator()]);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        string result = await behavior.Handle(new SampleRequest("valid"), next, CancellationToken.None);

        Assert.Equal("ok", result);
    }
}
