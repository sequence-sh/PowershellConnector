using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using FluentAssertions;
using Moq;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.TestHarness;
using Xunit;

namespace Reductech.EDR.Connectors.Pwsh.Tests
{
    public class PwshRunnerTests
    {
        [Fact]
        public void ProcessData_WhenSenderIsNotPSDataCollection_Throws()
        {
            var sender = new PSDataCollection<object>();

            Assert.Throws<ArgumentException>(() =>
                PwshRunner.ProcessData<PSObject>(sender, 0, o => o.ToString()));
        }

        [Fact]
        public void ProcessData_WhenSenderIsPSDataCollection_RemovesItemFromCollection()
        {
            var sender = new PSDataCollection<string>();
            var expected = "string0";
            sender.Add(expected);
            sender.Add("string1");

            PwshRunner.ProcessData<string>(sender, 0, o => { });

            Assert.Single(sender);
            Assert.DoesNotContain(sender, o => expected.Equals(o));
        }

        [Fact]
        public void ProcessData_WhenSenderIsPSDataCollection_InvokesAction()
        {
            var sender = new PSDataCollection<object>();
            var str = "";
            sender.Add(str);
            var mock = new Mock<Action<object>>();

            PwshRunner.ProcessData<object>(sender, 0, mock.Object);

            mock.Verify(m => m.Invoke(str));
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async void RunScript_ReadsDataFromOutputStream()
        {
            var logger = new TestLogger();
            var script = @"Write-Output 'one'; Write-Output 2";

            var result = await PwshRunner.RunScript(script, logger).ToListAsync();

            Assert.Equal(new List<PSObject> { "one", 2 }, result);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async void RunScript_LogsErrorsAndWarnings()
        {
            var logger = new TestLogger();
            var script = @"Write-Output 'one'; Write-Error 'error'; Write-Output 'two'; Write-Warning 'warning'";

            _ = await PwshRunner.RunScript(script, logger).ToListAsync();

            Assert.Equal(2, logger.LoggedValues.Count);
            Assert.Contains(logger.LoggedValues, o => o.Equals("error"));
            Assert.Contains(logger.LoggedValues, o => o.Equals("warning"));
        }

        [Fact]
        public void EntityFromPSObject_WhenBaseObjectIsPSO_ReturnsMultiValueEntity()
        {
            var pso = new PSObject();
            pso.Properties.Add(new PSNoteProperty("prop1", "value1"));
            pso.Properties.Add(new PSNoteProperty("prop2", "value2"));

            var entity = PwshRunner.EntityFromPSObject(pso);

            Assert.NotNull(entity);

            entity.TryGetValue("prop1", out var val1);
            entity.TryGetValue("prop2", out var val2);

            Assert.Equal(2, entity.Count());
            Assert.Equal("value1", val1!.ToString());
            Assert.Equal("value2", val2!.ToString());
        }

        [Theory]
        [InlineData("hello")]
        [InlineData(123)]
        [InlineData(1.5)]
        [InlineData(true)]
        public void EntityFromPSObject_WhenBaseObjectIsNotPSO_ReturnsSingleValueEntity(object expected)
        {
            var pso = new PSObject(expected);

            var entity = PwshRunner.EntityFromPSObject(pso);

            Assert.NotNull(entity);

            entity.TryGetValue(Entity.PrimitiveKey, out var val);

            Assert.NotNull(val);

            val!.Value.AsT1.Value.Switch(
                s => expected.Should().BeOfType<string>().Which.Equals(s),
                i => expected.Should().BeOfType<int>().Which.Equals(i),
                d => expected.Should().BeOfType<double>().Which.Equals(d),
                b => expected.Should().BeOfType<bool>().Which.Equals(b),
                e => expected.Should().BeOfType<Enumeration>().Which.Equals(e),
                dt => expected.Should().BeOfType<DateTime>().Which.Equals(dt),
                ent => expected.Should().BeOfType<Entity>().Which.Equals(ent)
            );
        }

        [Fact]
        public void EntityFromPSObject_WhenBaseObjectIsHashtable_ReturnsEntity()
        {
            var pso = new PSObject(new Hashtable
            {
                {"prop1", "value1"},
                {"prop2", 2}
            });

            var entity = PwshRunner.EntityFromPSObject(pso);

            Assert.NotNull(entity);

            entity.TryGetValue("prop1", out var val1);
            entity.TryGetValue("prop2", out var val2);

            Assert.Equal(2, entity.Count());
            Assert.Equal("value1", val1!.ToString());
            Assert.Equal(2, val2!.Value.AsT1.Value.AsT1);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async void EntityFromPSObject_WhenBaseObjectIsHashtable_ReturnsEntity_Integration()
        {
            var logger = new TestLogger();
            var script = @"@{prop1 = 'value1'; prop2 = 2} | Write-Output";

            var result = await PwshRunner.GetEntityEnumerable(script, logger).ToListAsync();

            Assert.Single(result);

            result[0].TryGetValue("prop1", out var val1);
            result[0].TryGetValue("prop2", out var val2);

            Assert.Equal("value1", val1!.ToString());
            Assert.Equal(2, val2!.Value.AsT1.Value.AsT1);
        }

        [Fact]
        public void EntityFromPSObject_WhenBaseObjectIsArray_ReturnsEntity()
        {
            var arr = new object[] { "value1", 2 };
            var pso = new PSObject(arr);

            var entity = PwshRunner.EntityFromPSObject(pso);

            Assert.NotNull(entity);

            entity.TryGetValue(Entity.PrimitiveKey, out var val);

            Assert.NotNull(val);

            var actual = val!.Value.AsT2.ToArray();

            Assert.Equal(arr[0], actual[0].Value.AsT0);
            Assert.Equal(arr[1], actual[1].Value.AsT1);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async void EntityFromPSObject_WhenBaseObjectIsArray_ReturnsEntity_Integration()
        {
            var logger = new TestLogger();
            var script = @"Write-Output -NoEnumerate @('value1', 2)";

            var result = await PwshRunner.GetEntityEnumerable(script, logger).ToListAsync();

            Assert.Single(result);

            result[0].TryGetValue(Entity.PrimitiveKey, out var val);

            Assert.NotNull(val);

            var arr = val!.Value.AsT2.ToArray();

            Assert.Equal("value1", arr[0].Value);
            Assert.Equal(2, arr[1].Value);
        }

    }
}
