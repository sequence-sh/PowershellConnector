﻿using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MELT;
using Moq;
using Sequence.Core.Entities;
using Sequence.Core.Enums;
using Xunit;
using Entity = Sequence.Core.Entity;

namespace Sequence.Connectors.Pwsh.Tests;

public class PwshRunnerTests
{
#region ProcessData

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

        PwshRunner.ProcessData<string>(sender, 0, _ => { });

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

        PwshRunner.ProcessData(sender, 0, mock.Object);

        mock.Verify(m => m.Invoke(str));
    }

#endregion

#region RunScript

    [Fact]
    [Trait("Category", "Integration")]
    public async void RunScript_ReadsDataFromOutputStream()
    {
        var logger = TestLoggerFactory.Create().CreateLogger("Test");
        var script = @"Write-Output 'one'; Write-Output 2";

        var result = await PwshRunner.RunScript(script, logger);

        Assert.Equal(new List<PSObject> { "one", 2 }, result);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void RunScript_LogsErrorsWarningsAndInformation()
    {
        var lf = TestLoggerFactory.Create();

        var script =
            @"Write-Output 'one'; Write-Error 'error'; Write-Output 'two'; Write-Warning 'warning'; Write-Information 'info'";

        _ = await PwshRunner.RunScript(script, lf.CreateLogger("Test"));

        Assert.Equal(3, lf.Sink.LogEntries.Count());
        Assert.Contains(lf.Sink.LogEntries, o => o.Message is "error");
        Assert.Contains(lf.Sink.LogEntries, o => o.Message is "warning");
        Assert.Contains(lf.Sink.LogEntries, o => o.Message is "info");
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

        var result = await PwshRunner.RunScript(script, lf.CreateLogger("Test"), null, input);

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
            ("Var5", TextCase.Upper),
            ("Var6", new DateTime(2020, 12, 12))
        );

        var result = await PwshRunner.RunScript(script, lf.CreateLogger("Test"), entity);

        for (var i = 0; i < entity.Headers.Length; i++)
            Assert.Equal(
                entity.TryGetValue($"Var{i + 1}").Value,
                result[i].BaseObject
            );
    }

#endregion

#region RunScriptAsync

    [Fact]
    [Trait("Category", "Integration")]
    public async void RunScriptAsync_ReadsDataFromOutputStream()
    {
        var logger = TestLoggerFactory.Create().CreateLogger("Test");
        var script = @"Write-Output 'one'; Write-Output 2";

        var result = await PwshRunner.RunScriptAsync(script, logger).ToListAsync();

        Assert.Equal(new List<PSObject> { "one", 2 }, result);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void RunScriptAsync_LogsErrorsWarningsAndInformation()
    {
        var lf = TestLoggerFactory.Create();

        var script =
            @"Write-Output 'one'; Write-Error 'error'; Write-Output 'two'; Write-Warning 'warning'; Write-Information 'info'";

        _ = await PwshRunner.RunScriptAsync(script, lf.CreateLogger("Test")).ToListAsync();

        Assert.Equal(3, lf.Sink.LogEntries.Count());
        Assert.Contains(lf.Sink.LogEntries, o => o.Message is "error");
        Assert.Contains(lf.Sink.LogEntries, o => o.Message is "warning");
        Assert.Contains(lf.Sink.LogEntries, o => o.Message is "info");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void RunScriptAsync_ReadsDataFromInputStream()
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

        var result = await PwshRunner.RunScriptAsync(script, lf.CreateLogger("Test"), null, input)
            .ToListAsync();

        var actual = result.Select(o => (int)o.BaseObject).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void RunScriptAsync_CorrectlyPassesVariablesToScript()
    {
        var lf = TestLoggerFactory.Create();

        var script = @"1..6 | % { Write-Output (Get-Variable -Name ""Var$_"").Value }";

        var entity = Entity.Create(
            ("Var1", "value1"),
            ("Var2", 2),
            ("Var3", 3.3),
            ("Var4", true),
            ("Var5", TextCase.Upper),
            ("Var6", new DateTime(2020, 12, 12))
        );

        var result = await PwshRunner.RunScriptAsync(script, lf.CreateLogger("Test"), entity)
            .ToListAsync();

        for (var i = 0; i < entity.Headers.Length; i++)
            Assert.Equal(
                entity.TryGetValue($"Var{i + 1}").Value,
                result[i].BaseObject
            );
    }

#endregion

#region EntityFromPSObject

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

        //Assert.NotNull(entity);

        var val1 = entity.TryGetValue("prop1").Map(x => x.Serialize(SerializeOptions.Primitive));
        var val2 = entity.TryGetValue("prop2").Map(x => x.Serialize(SerializeOptions.Primitive));

        Assert.Equal(2,        entity.Count());
        Assert.Equal("value1", val1.ToString());
        Assert.Equal("value2", val2.ToString());
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

        //Assert.NotNull(entity);

        var val = entity.TryGetValue(EntityKey.PrimitiveKey).Value;

        Assert.Equal(expected, val.ToCSharpObject());
    }

    [Fact]
    public void EntityFromPSObject_WhenBaseObjectIsHashtable_ReturnsEntity()
    {
        var pso = new PSObject(new Hashtable { { "prop1", "value1" }, { "prop2", 2 } });

        var entity = PwshRunner.EntityFromPSObject(pso);

        //Assert.NotNull(entity);

        var val1 = entity.TryGetValue("prop1").Map(x => x.Serialize(SerializeOptions.Primitive));
        var val2 = entity.TryGetValue("prop2").Map(x => x);

        Assert.Equal(2,             entity.Count());
        Assert.Equal("value1",      val1);
        Assert.Equal(new SCLInt(2), val2);
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

        var val1 = result[0].TryGetValue("prop1").Map(x => x.Serialize(SerializeOptions.Primitive));
        var val2 = result[0].TryGetValue("prop2").Map(x => x);

        Assert.Equal("value1",      val1);
        Assert.Equal(new SCLInt(2), val2);
    }

    [Fact]
    public void EntityFromPSObject_WhenBaseObjectIsArray_ReturnsEntity()
    {
        var arr = new object[] { "value1", 2 };
        var pso = new PSObject(arr);

        var entity = PwshRunner.EntityFromPSObject(pso);

        //Assert.NotNull(entity);

        var actual = ConvertToList(entity.TryGetValue(EntityKey.PrimitiveKey).Value);

        Assert.Equal(ISCLObject.CreateFromCSharpObject(arr[0]), actual[0]);
        Assert.Equal(ISCLObject.CreateFromCSharpObject(arr[1]), actual[1]);
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

        var actual = ConvertToList(result[0].TryGetValue(EntityKey.PrimitiveKey).Value);

        Assert.Equal(new StringStream("value1"), actual[0]);
        Assert.Equal(2.ConvertToSCLObject(),     actual[1]);
    }

    private static List<ISCLObject> ConvertToList(ISCLObject arrayObject) =>
        ((IArray)arrayObject).ListIfEvaluated().Value;

#endregion

#region PSObjectFromEntity

    [Theory]
    [InlineData("hello")]
    [InlineData(123)]
    [InlineData(1.5)]
    [InlineData(true)]
    public void PSObjectFromEntity_WhenEntityIsAPrimitive_ReturnsPsObject(object expected)
    {
        var entity = Entity.Create((EntityKey.PrimitiveKey, expected));

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
            ("enum", TextCase.Upper),
            ("date", new DateTime(2020, 12, 12))
        );

        var actual = PwshRunner.PSObjectFromEntity(entity);

        Assert.IsType<PSObject>(actual);

        foreach (var prop in entity)
            Assert.Equal(
                prop.Value.ToCSharpObject(),
                actual.Properties[prop.Key.Inner].Value
            );
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

#endregion
}
