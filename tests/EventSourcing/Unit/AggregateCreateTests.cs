using Aviant.Core.EventSourcing.Aggregates;
using Aviant.Core.EventSourcing.DomainEvents;
using FluentAssertions;
using Xunit;

namespace Aviant.Tests.EventSourcing.Unit;

public sealed class AggregateCreateTests
{
    [Fact]
    public void Create_ShouldRehydrateStateAndClearTransientEvents()
    {
        var aggregateId = new TestAggregateId(Guid.NewGuid());
        var source = TestAggregate.CreateForTest(aggregateId);
        source.ChangeName("first");
        source.ChangeName("second");

        var aggregate = TestAggregate.Create(source.Events);

        aggregate.Name.Should().Be("second");
        aggregate.Version.Should().Be(2);
        aggregate.Events.Should().BeEmpty();
    }

    private sealed class TestAggregateId : IAggregateId
    {
        public TestAggregateId(Guid key) => Key = key;

        public Guid Key { get; }

        public byte[] Serialize() => Key.ToByteArray();
    }

    private sealed class TestAggregate : Aggregate<TestAggregate, TestAggregateId>
    {
        private TestAggregate(TestAggregateId id)
            : base(id)
        { }

        private TestAggregate()
        { }

        public static TestAggregate CreateForTest(TestAggregateId id) => new(id);

        public void ChangeName(string name) => AddEvent(new NameChanged(this, name));

        protected override void Apply(IDomainEvent<TestAggregateId> @event)
        {
            if (@event is NameChanged changed)
            {
                Id = changed.AggregateId;
                Name = changed.Name;
            }
        }

        public string? Name { get; private set; }
    }

    private sealed record NameChanged : DomainEvent<TestAggregate, TestAggregateId>
    {
        public NameChanged(TestAggregate aggregate, string name)
            : base(aggregate)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
