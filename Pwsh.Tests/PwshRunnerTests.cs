using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MELT;
using Moq;
using Reductech.EDR.Core.Entities;
using Xunit;
using Entity = Reductech.EDR.Core.Entity;

namespace Reductech.EDR.Connectors.Pwsh.Tests
{

public class PwshRunnerTests
{
    [Fact]
    public void ProcessData_WhenSenderIsNotPSDataCollection_Throws()
    {
        var sender = new PSDataCollection<object>();

        Assert.Throws<ArgumentException>(
            () =>
                PwshRunner.ProcessData<PSObject>(sender, 0, _ => { })
        );
    }

    [Fact]
    public void ProcessData_WhenSenderIsPSDataCollection_RemovesItemFromCollection()
    {
        var sender   = new PSDataCollection<string>();
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
        var str    = "";
        var mock   = new Mock<Action<object>>();

        sender.Add(str);

        PwshRunner.ProcessData<object>(sender, 0, mock.Object);

        mock.Verify(m => m.Invoke(str));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void RunScript_ReadsDataFromOutputStream()
    {
        var logger = TestLoggerFactory.Create().CreateLogger("Test");
        var script = @"Write-Output 'one'; Write-Output 2";

        var result = await PwshRunner.RunScript(script, logger).ToListAsync();

        Assert.Equal(new List<PSObject> { "one", 2 }, result);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void RunScript_LogsErrorsAndWarnings()
    {
        var lf = TestLoggerFactory.Create();

        var script =
            @"Write-Output 'one'; Write-Error 'error'; Write-Output 'two'; Write-Warning 'warning'";

        _ = await PwshRunner.RunScript(script, lf.CreateLogger("Test")).ToListAsync();

        Assert.Equal(2, lf.Sink.LogEntries.Count());
        Assert.Contains(lf.Sink.LogEntries, o => o.Message != null && o.Message.Equals("error"));
        Assert.Contains(lf.Sink.LogEntries, o => o.Message != null && o.Message.Equals("warning"));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void RunScript_ReadsDataFromInputStream()
    {
        var lf       = TestLoggerFactory.Create();
        var script   = @"$Input | ForEach { Write-Output $_ }";
        var expected = new[] { 32, 120, 71, 89, 20 };

        var input = new PSDataCollection<PSObject>(5);

        _ = Task.Run(
            () =>
            {
                foreach (var num in expected)
                {
                    input.Add(num);
                    Thread.Sleep(num);
                }

                input.Complete();
            }
        );

        var result = await PwshRunner.RunScript(script, lf.CreateLogger("Test"), null, input)
            .ToListAsync();

        var actual = result.Select(o => (int)o.BaseObject).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void RunScript_CorrectlyPassesVariablesToScript()
    {
        var lf = TestLoggerFactory.Create();

        var script = @"1..6 | % { Write-Output (Get-Variable -Name ""Var$_"").Value }";

        var entity = Entity.Create(
            ("Var1", "value1"),
            ("Var2", 2),
            ("Var3", 3.3),
            ("Var4", true),
            ("Var5", Core.SCLType.Enum),
            ("Var6", new DateTime(2020, 12, 12))
        );

        var result = await PwshRunner.RunScript(script, lf.CreateLogger("Test"), entity, null)
            .ToListAsync();

        for (var i = 0; i < entity.Dictionary.Count; i++)
            Assert.Equal(
                entity.Dictionary[$"Var{i + 1}"].BestValue.ObjectValue,
                result[i].BaseObject
            );
    }

    [Fact]
    public void EntityFromPSObject_WhenPSObjectIsNull_Throws()
    {
        var err = Assert.Throws<NullReferenceException>(() => PwshRunner.EntityFromPSObject(null!));
        Assert.Matches("cannot be null", err.Message);
    }

    [Fact]
    public void EntityFromPSObject_WhenBaseObjectIsPSO_ReturnsMultiValueEntity()
    {
        var pso = new PSObject();

        pso.Properties.Add(new PSNoteProperty("prop1", "value1"));
        pso.Properties.Add(new PSNoteProperty("prop2", "value2"));

        var entity = PwshRunner.EntityFromPSObject(pso);

        Assert.NotNull(entity);

        var val1 = entity.TryGetValue("prop1").Map(x => x.GetPrimitiveString());
        var val2 = entity.TryGetValue("prop2").Map(x => x.GetPrimitiveString());

        Assert.Equal(2,        entity.Count());
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

        var val = entity.TryGetValue(Entity.PrimitiveKey).Value.ObjectValue;

        Assert.Equal(expected, val);
    }

    [Fact]
    public void EntityFromPSObject_WhenBaseObjectIsHashtable_ReturnsEntity()
    {
        var pso = new PSObject(new Hashtable { { "prop1", "value1" }, { "prop2", 2 } });

        var entity = PwshRunner.EntityFromPSObject(pso);

        Assert.NotNull(entity);

        var val1 = entity.TryGetValue("prop1").Map(x => x.GetPrimitiveString());
        var val2 = entity.TryGetValue("prop2").Map(x => x.ObjectValue);

        Assert.Equal(2,        entity.Count());
        Assert.Equal("value1", val1);
        Assert.Equal(2,        val2);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void EntityFromPSObject_WhenBaseObjectIsHashtable_ReturnsEntity_Integration()
    {
        var lf     = TestLoggerFactory.Create();
        var script = @"@{prop1 = 'value1'; prop2 = 2} | Write-Output";

        var result = await PwshRunner.GetEntityEnumerable(script, lf.CreateLogger("Test"))
            .ToListAsync();

        Assert.Single(result);

        var val1 = result[0].TryGetValue("prop1").Map(x => x.GetPrimitiveString());
        var val2 = result[0].TryGetValue("prop2").Map(x => x.ObjectValue);

        Assert.Equal("value1", val1);
        Assert.Equal(2,        val2);
    }

    [Fact]
    public void EntityFromPSObject_WhenBaseObjectIsArray_ReturnsEntity()
    {
        var arr = new object[] { "value1", 2 };
        var pso = new PSObject(arr);

        var entity = PwshRunner.EntityFromPSObject(pso);

        Assert.NotNull(entity);

        var actual =
            entity.Dictionary[Entity.PrimitiveKey].BestValue.ObjectValue as
                ImmutableList<EntityValue>;

        Assert.Equal(arr[0], actual![0].ObjectValue);
        Assert.Equal(arr[1], actual![1].ObjectValue);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void EntityFromPSObject_WhenBaseObjectIsArray_ReturnsEntity_Integration()
    {
        var lf     = TestLoggerFactory.Create();
        var script = @"Write-Output -NoEnumerate @('value1', 2)";

        var result = await PwshRunner.GetEntityEnumerable(script, lf.CreateLogger("Test"))
            .ToListAsync();

        Assert.Single(result);

        var actual = result[0].Dictionary[Entity.PrimitiveKey].BestValue.ObjectValue as
            ImmutableList<EntityValue>;

        Assert.Equal("value1", actual![0].ObjectValue);
        Assert.Equal(2,        actual![1].ObjectValue);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData(123)]
    [InlineData(1.5)]
    [InlineData(true)]
    public void PSObjectFromEntity_WhenEntityIsAPrimitive_ReturnsPsObject(object expected)
    {
        var entity = Entity.Create((Entity.PrimitiveKey, expected));

        var actual = PwshRunner.PSObjectFromEntity(entity);

        Assert.IsType<PSObject>(actual);
        Assert.Equal(expected, actual.BaseObject);
    }

    [Fact]
    public void PSObjectFromEntity_WhenEntityIsNotAPrimitive_SetsPSObjectProperties()
    {
        var entity = Entity.Create(
            ("string", "value1"),
            ("int", 2),
            ("double", 3.3),
            ("bool", true),
            ("enum", Core.SCLType.Enum),
            ("date", new DateTime(2020, 12, 12))
        );

        var actual = PwshRunner.PSObjectFromEntity(entity);

        Assert.IsType<PSObject>(actual);

        foreach (var prop in entity)
            Assert.Equal(prop.BestValue.ObjectValue, actual.Properties[prop.Name].Value);
    }

    [Fact]
    public void PSObjectFromEntity_WhenEntityValueIsEntity_ConvertsToPSObject()
    {
        var key    = "entity";
        var entity = Entity.Create((key, Entity.Create(("key", "value"))));

        var actual = PwshRunner.PSObjectFromEntity(entity);

        Assert.IsType<PSObject>(actual);
        Assert.IsType<PSObject>(actual.Properties[key].Value);
        Assert.Equal("value", ((PSObject)actual.Properties[key].Value).Properties["key"].Value);
    }

    [Fact]
    public void PSObjectFromEntity_WhenEntityValueIsEntityValueList_ConvertsToObjectList()
    {
        var expected = new object[] { 1, "item2" };
        var entity   = Entity.Create(("list", expected));

        var actual = PwshRunner.PSObjectFromEntity(entity);

        Assert.IsType<PSObject>(actual);
        Assert.Equal(expected, actual.Properties["list"].Value);
    }
}

}
